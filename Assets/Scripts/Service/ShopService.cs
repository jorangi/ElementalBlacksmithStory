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
    public class ShopService : IInventoryContext, IDisposable
    {
        private readonly MaterialInventory _materialInventory;
        private readonly EquipmentInventory _equipmentInventory;
        private readonly RuneInventory _runeInventory;
        private readonly SO_MaterialDatabase _materialDatabase;
        private readonly IPublisher<ChangeMoneyEvent> _moneyPublisher;
        private readonly IDisposable _moneySubscription;

        public MaterialInventory MaterialInventory => _materialInventory;
        public EquipmentInventory EquipmentInventory => _equipmentInventory;
        public RuneInventory RuneInventory => _runeInventory;
        public SO_MaterialDatabase MaterialDatabase => _materialDatabase;

        private readonly Subject<IReadOnlyDictionary<uint, uint>> _onPurchaseSuccessSubject = new();
        public Observable<IReadOnlyDictionary<uint, uint>> OnPurchaseSuccessAsObservable => _onPurchaseSuccessSubject;

        private ulong _currentMoney;

        [Inject]
        public ShopService(
            MaterialInventory materialInventory,
            EquipmentInventory equipmentInventory,
            RuneInventory runeInventory,
            SO_MaterialDatabase materialDatabase,
            ISubscriber<ChangeMoneyEvent> moneySubscriber,
            IPublisher<ChangeMoneyEvent> moneyPublisher)
        {
            _materialInventory = materialInventory;
            _equipmentInventory = equipmentInventory;
            _runeInventory = runeInventory;
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

            _currentMoney -= totalCost;
            _moneyPublisher.Publish(new ChangeMoneyEvent(out _, _currentMoney));

            var resultDict = new Dictionary<uint, uint>();
            foreach (var (shopItem, amount) in cartItems)
            {
                if (shopItem == null) continue;
                resultDict[shopItem.Id] = amount;
                shopItem.OnPurchased(this, amount);
            }

            Debug.Log($"[ShopService] 구매 완료! {totalCost}골드 소모, 잔여: {_currentMoney}골드");
            _onPurchaseSuccessSubject.OnNext(resultDict);
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

            foreach (var (shopItem, amount) in cartItems)
            {
                if (shopItem == null) continue;
                if (shopItem.GetOwnedCount(this) < amount)
                {
                    Debug.LogWarning($"[ShopService] 판매할 아이템({shopItem.Name})의 소지량이 부족합니다.");
                    return false;
                }
            }

            var resultDict = new Dictionary<uint, uint>();
            foreach (var (shopItem, amount) in cartItems)
            {
                if (shopItem == null) continue;
                resultDict[shopItem.Id] = amount;
                shopItem.OnSold(this, amount);
            }

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

            foreach (var kvp in cartItems)
            {
                var materialData = _materialDatabase.Get(kvp.Key);
                if (materialData != null)
                {
                    _materialInventory.Get(materialData, kvp.Value);
                }
            }

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
