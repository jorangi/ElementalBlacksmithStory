using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Inventory;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class ShopService : IDisposable
    {
        private readonly MaterialInventory _materialInventory;
        private readonly EquipmentInventory _equipmentInventory;
        private readonly SO_MaterialDatabase _materialDatabase;
        private readonly IPublisher<ChangeMoneyEvent> _moneyPublisher;
        private readonly IDisposable _moneySubscription;

        private readonly Subject<IReadOnlyDictionary<uint, uint>> _onPurchaseSuccessSubject = new();
        public Observable<IReadOnlyDictionary<uint, uint>> OnPurchaseSuccessAsObservable => _onPurchaseSuccessSubject;

        private ulong _currentMoney;

        [Inject]
        public ShopService(
            MaterialInventory materialInventory,
            EquipmentInventory equipmentInventory,
            SO_MaterialDatabase materialDatabase,
            ISubscriber<ChangeMoneyEvent> moneySubscriber,
            IPublisher<ChangeMoneyEvent> moneyPublisher)
        {
            _materialInventory = materialInventory;
            _equipmentInventory = equipmentInventory;
            _materialDatabase = materialDatabase;
            _moneyPublisher = moneyPublisher;

            _moneySubscription = moneySubscriber.Subscribe(e =>
            {
                _currentMoney = e.MoneyChange;
            });
        }

        public ulong CurrentMoney => _currentMoney;

        public bool CanAfford(ulong cost) => _currentMoney >= cost;

        /// <summary>
        /// 장바구니 IShopItem 목록 일괄 구매 시도
        /// </summary>
        public bool TryPurchase(IEnumerable<(IShopItem item, uint amount)> cartItems, ulong totalCost)
        {
            if (cartItems == null)
            {
                Debug.LogWarning("[ShopService] 구매할 아이템이 장바구니에 없습니다.");
                return false;
            }

            if (!CanAfford(totalCost))
            {
                Debug.LogWarning($"[ShopService] 골드가 부족합니다. 필요: {totalCost}, 보유: {_currentMoney}");
                return false;
            }

            // 1. 재화 차감 및 이벤트 발행
            _currentMoney -= totalCost;
            _moneyPublisher.Publish(new ChangeMoneyEvent(out _, _currentMoney));

            // 2. 인벤토리에 아이템 지급
            var resultDict = new Dictionary<uint, uint>();
            foreach (var (shopItem, amount) in cartItems)
            {
                resultDict[shopItem.Id] = amount;

                if (shopItem is MaterialShopItem materialItem && materialItem.Data != null)
                {
                    _materialInventory.Add(materialItem.Data, amount);
                }
                else if (shopItem is Weapon weapon)
                {
                    _equipmentInventory.Add(weapon);
                }
                else
                {
                    // 기본 폴백: 재료 DB에서 조회 시도
                    var mat = _materialDatabase.Get(shopItem.Id);
                    if (mat != null)
                    {
                        _materialInventory.Add(mat, amount);
                    }
                    else
                    {
                        Debug.LogWarning($"[ShopService] {shopItem.Name}(ID: {shopItem.Id})를 추가할 적절한 인벤토리를 찾지 못했습니다.");
                    }
                }
            }

            Debug.Log($"[ShopService] 구매 완료! {totalCost}골드 소모, 잔여: {_currentMoney}골드");
            _onPurchaseSuccessSubject.OnNext(resultDict);
            return true;
        }

        /// <summary>
        /// 장바구니 아이템 일괄 구매 시도 (기존 ID 기반 호환)
        /// </summary>
        /// <param name="cartItems">구매할 아이템 목록 (ItemId, 수량)</param>
        /// <param name="totalCost">총 구매 비용</param>
        /// <returns>구매 성공 여부</returns>
        public bool TryPurchase(IReadOnlyDictionary<uint, uint> cartItems, ulong totalCost)
        {
            if (cartItems == null || cartItems.Count == 0)
            {
                Debug.LogWarning("[ShopService] 구매할 아이템이 장바구니에 없습니다.");
                return false;
            }

            if (!CanAfford(totalCost))
            {
                Debug.LogWarning($"[ShopService] 골드가 부족합니다. 필요: {totalCost}, 보유: {_currentMoney}");
                return false;
            }

            // 1. 재화 차감 및 이벤트 발행
            _currentMoney -= totalCost;
            _moneyPublisher.Publish(new ChangeMoneyEvent(out _, _currentMoney));

            // 2. 인벤토리에 아이템 지급
            foreach (var kvp in cartItems)
            {
                uint itemId = kvp.Key;
                uint amount = kvp.Value;

                var materialData = _materialDatabase.Get(itemId);
                if (materialData != null)
                {
                    _materialInventory.Add(materialData, amount);
                }
                else
                {
                    Debug.LogWarning($"[ShopService] ID {itemId}에 해당하는 재료 데이터를 찾을 수 없어 인벤토리에 추가하지 못했습니다.");
                }
            }

            Debug.Log($"[ShopService] 구매 완료! {totalCost}골드 소모, 잔여: {_currentMoney}골드");
            _onPurchaseSuccessSubject.OnNext(cartItems);
            return true;
        }

        /// <summary>
        /// 장바구니 IShopItem 목록 일괄 판매 시도
        /// </summary>
        public bool TrySell(IEnumerable<(IShopItem item, uint amount)> cartItems, ulong totalRevenue)
        {
            if (cartItems == null)
            {
                Debug.LogWarning("[ShopService] 판매할 아이템이 장바구니에 없습니다.");
                return false;
            }

            // 1. 소지 수량 검증
            foreach (var (shopItem, amount) in cartItems)
            {
                if (shopItem is MaterialShopItem materialItem && materialItem.Data != null)
                {
                    if (_materialInventory.GetCount(materialItem.Data) < amount)
                    {
                        Debug.LogWarning($"[ShopService] 판매할 재료({shopItem.Name})의 소지량이 부족합니다.");
                        return false;
                    }
                }
                else if (shopItem is Weapon weapon)
                {
                    if (_equipmentInventory.GetCountByWeaponId(weapon.WeaponId) < (int)amount)
                    {
                        Debug.LogWarning($"[ShopService] 판매할 무기({shopItem.Name})의 소지량이 부족합니다.");
                        return false;
                    }
                }
                else
                {
                    var mat = _materialDatabase.Get(shopItem.Id);
                    if (mat == null || _materialInventory.GetCount(mat) < amount)
                    {
                        Debug.LogWarning($"[ShopService] 판매할 아이템({shopItem.Name})의 소지량이 부족합니다.");
                        return false;
                    }
                }
            }

            // 2. 인벤토리에서 아이템 차감
            var resultDict = new Dictionary<uint, uint>();
            foreach (var (shopItem, amount) in cartItems)
            {
                resultDict[shopItem.Id] = amount;

                if (shopItem is MaterialShopItem materialItem && materialItem.Data != null)
                {
                    _materialInventory.Get(materialItem.Data, amount);
                }
                else if (shopItem is Weapon weapon)
                {
                    _equipmentInventory.RemoveByWeaponId(weapon.WeaponId, (int)amount);
                }
                else
                {
                    var mat = _materialDatabase.Get(shopItem.Id);
                    if (mat != null)
                    {
                        _materialInventory.Get(mat, amount);
                    }
                }
            }

            // 3. 재화 지급 및 이벤트 발행
            _currentMoney += totalRevenue;
            _moneyPublisher.Publish(new ChangeMoneyEvent(out _, _currentMoney));

            Debug.Log($"[ShopService] 판매 완료! {totalRevenue}골드 획득, 잔여: {_currentMoney}골드");
            _onPurchaseSuccessSubject.OnNext(resultDict);
            return true;
        }

        /// <summary>
        /// 장바구니 아이템 일괄 판매 시도 (기존 ID 기반 호환)
        /// </summary>
        public bool TrySell(IReadOnlyDictionary<uint, uint> cartItems, ulong totalRevenue)
        {
            if (cartItems == null || cartItems.Count == 0)
            {
                Debug.LogWarning("[ShopService] 판매할 아이템이 장바구니에 없습니다.");
                return false;
            }

            // 1. 소지 수량 검증
            foreach (var kvp in cartItems)
            {
                uint itemId = kvp.Key;
                uint amount = kvp.Value;

                var materialData = _materialDatabase.Get(itemId);
                if (materialData == null || _materialInventory.GetCount(materialData) < amount)
                {
                    Debug.LogWarning($"[ShopService] ID {itemId} 재료의 소지량이 부족합니다.");
                    return false;
                }
            }

            // 2. 인벤토리에서 차감
            foreach (var kvp in cartItems)
            {
                var materialData = _materialDatabase.Get(kvp.Key);
                if (materialData != null)
                {
                    _materialInventory.Get(materialData, kvp.Value);
                }
            }

            // 3. 재화 지급
            _currentMoney += totalRevenue;
            _moneyPublisher.Publish(new ChangeMoneyEvent(out _, _currentMoney));

            Debug.Log($"[ShopService] 판매 완료! {totalRevenue}골드 획득, 잔여: {_currentMoney}골드");
            _onPurchaseSuccessSubject.OnNext(cartItems);
            return true;
        }

        public void Dispose()
        {
            _moneySubscription?.Dispose();
            _onPurchaseSuccessSubject.Dispose();
        }
    }
}
