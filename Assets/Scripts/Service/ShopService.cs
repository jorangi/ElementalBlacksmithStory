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
        private readonly ForgeManager _forgeManager;
        private readonly IPublisher<ChangeMoneyEvent> _moneyPublisher;
        private readonly IPublisher<AcquireItemEvent> _acquireItemPublisher;
        private readonly IPublisher<CoinParticleEvent> _coinParticlePublisher;
        private readonly IPublisher<PlaySoundEvent> _soundPublisher;
        private readonly IPublisher<SellEvent> _sellEventPublisher;
        private readonly IDisposable _moneySubscription;

        public MaterialInventory MaterialInventory => _materialInventory;
        public EquipmentInventory EquipmentInventory => _equipmentInventory;
        public RuneInventory RuneInventory => _runeInventory;
        public SO_MaterialDatabase MaterialDatabase => _materialDatabase;
        public IPublisher<AcquireItemEvent> AcquireItemPublisher => _acquireItemPublisher;

        private readonly Subject<IReadOnlyDictionary<uint, uint>> _onPurchaseSuccessSubject = new();
        public Observable<IReadOnlyDictionary<uint, uint>> OnPurchaseSuccessAsObservable => _onPurchaseSuccessSubject;

        private ulong _currentMoney;

        [Inject]
        public ShopService(
            MaterialInventory materialInventory,
            EquipmentInventory equipmentInventory,
            RuneInventory runeInventory,
            SO_MaterialDatabase materialDatabase,
            ForgeManager forgeManager,
            ISubscriber<ChangeMoneyEvent> moneySubscriber,
            IPublisher<ChangeMoneyEvent> moneyPublisher,
            IPublisher<AcquireItemEvent> acquireItemPublisher,
            IPublisher<CoinParticleEvent> coinParticlePublisher,
            IPublisher<PlaySoundEvent> soundPublisher,
            IPublisher<SellEvent> sellEventPublisher)
        {
            _materialInventory = materialInventory;
            _equipmentInventory = equipmentInventory;
            _runeInventory = runeInventory;
            _materialDatabase = materialDatabase;
            _forgeManager = forgeManager;
            _moneyPublisher = moneyPublisher;
            _acquireItemPublisher = acquireItemPublisher;
            _coinParticlePublisher = coinParticlePublisher;
            _soundPublisher = soundPublisher;
            _sellEventPublisher = sellEventPublisher;

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

            bool isEquippedWeaponSold = false;
            var resultDict = new Dictionary<uint, uint>();
            foreach (var (shopItem, amount) in cartItems)
            {
                if (shopItem == null) continue;
                resultDict[shopItem.Id] = amount;
                shopItem.OnSold(this, amount);

                if (_forgeManager != null && _forgeManager.CurrentWeapon != null)
                {
                    if (shopItem == _forgeManager.CurrentWeapon ||
                        (shopItem is Weapon w && w.Id == _forgeManager.CurrentWeapon.Id))
                    {
                        isEquippedWeaponSold = true;
                    }
                }
            }

            _currentMoney += totalRevenue;
            _moneyPublisher.Publish(new ChangeMoneyEvent(out _, _currentMoney));

            // 판매 수익에 비례한 코인 파티클 연출
            int coinCount = totalRevenue switch
            {
                >= 100000 => 300,
                >= 80000  => 250,
                >= 50000  => 200,
                >= 20000  => 150,
                >= 10000  => 100,
                >= 8000   => 90,
                >= 5000   => 60,
                >= 3000   => 40,
                >= 1000   => 20,
                > 0       => 10,
                _         => 0
            };

            if (coinCount > 0)
            {
                _coinParticlePublisher.Publish(new CoinParticleEvent(coinCount));
            }

            // 장착된 무기가 판매된 경우 대장간 초기화 및 기본 무기(10001) 재장착
            if (isEquippedWeaponSold)
            {
                _sellEventPublisher.Publish(new SellEvent(0));
            }
            else
            {
                _soundPublisher.Publish(new PlaySoundEvent(40103));
            }

            Debug.Log($"[ShopService] 판매 완료! {totalRevenue}골드 획득, 잔여: {_currentMoney}골드 (장착무기판매: {isEquippedWeaponSold})");
            _onPurchaseSuccessSubject.OnNext(resultDict);
            return true;
        }

        public void Dispose()
        {
            _moneySubscription?.Dispose();
            _onPurchaseSuccessSubject.Dispose();
        }
    }
}
