using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Data.DTO;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Inventory;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Data
{
    public class PlayerData : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly ReactiveProperty<ulong> _money = new(1000000000);
        public MaterialInventory MaterialInventory { get; private set; }
        public RuneInventory RuneInventory { get; private set; }
        public EquipmentInventory EquipmentInventory { get; private set; }
        private readonly Dictionary<uint, PriceRecord> _priceRecord = new();
        private readonly Subject<uint> _onTradePriceChangedSubject = new();
        public Observable<uint> OnTradePriceChanged => _onTradePriceChangedSubject;
        private readonly IPublisher<PlaySoundEvent> _soundPublisher;
        private readonly IPublisher<CoinParticleEvent> _coinParticlePublisher;

        /// <summary>
        /// 소지금
        /// </summary>
        public ReadOnlyReactiveProperty<ulong> Money => _money;
        [Inject]
        public PlayerData(
            MaterialInventory materialInventory,
            RuneInventory runeInventory,
            EquipmentInventory equipmentInventory,
            IPublisher<PlaySoundEvent> soundPublisher,
            IPublisher<CoinParticleEvent> coinParticlePublisher
        )
        {
            this.MaterialInventory = materialInventory;
            this.RuneInventory = runeInventory;
            this.EquipmentInventory = equipmentInventory;
            _soundPublisher = soundPublisher;
            _coinParticlePublisher = coinParticlePublisher;
        }
        /// <summary>
        /// 소지금 변경
        /// </summary>
        /// <param name="amount">변경할 금액</param>
        public void SetMoney(ulong amount)
        {
            _money.Value = amount;
        }
        public void AddMoney(ulong amount)
        {
            if (amount == 0) return;
            _money.Value += amount;
            int coinCount = amount switch
            {
                >= 100000 => 300,
                >= 80000 => 250,
                >= 50000 => 200,
                >= 20000 => 150,
                >= 10000 => 100,
                >= 8000 => 90,
                >= 5000 => 60,
                >= 3000 => 40,
                >= 1000 => 20,
                > 0 => 10,
                _ => 0
            };
            if (coinCount > 0)
            {
                _coinParticlePublisher.Publish(new CoinParticleEvent(coinCount));
            }
            _soundPublisher.Publish(new PlaySoundEvent(40103));
        }
        public bool TrySpend(ulong amount)
        {
            if (_money.Value >= amount)
            {
                _money.Value -= amount;
                return true;
            }
            return false;
        }
        public bool CanAfford(ulong amount) => _money.Value >= amount;
        public PlayerSaveData ToSaveData()
        {
            var saveData = new PlayerSaveData
            {
                Money = _money.Value,
                Materials = MaterialInventory.ToSaveData(),
                Runes = RuneInventory.ToSaveData(),
                Equipments = EquipmentInventory.ToSaveData()
            };
            return saveData;
        }
        public void LoadSaveData(PlayerSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogError("[PlayerData] 세이브 데이터가 없습니다.");
                return;
            }
            _money.Value = saveData.Money;
            MaterialInventory.LoadSaveData(saveData.Materials);
            RuneInventory.LoadSaveData(saveData.Runes);
            EquipmentInventory.LoadSaveData(saveData.Equipments);
        }
        public void RecordTradePrice(uint itemId, ulong price, ulong basePrice = 0)
        {
            if (!_priceRecord.TryGetValue(itemId, out var record))
            {
                record = default;
            }
            record.AddRecord(price, basePrice);
            _priceRecord[itemId] = record;
            _onTradePriceChangedSubject.OnNext(itemId);
        }
        public bool TryPeekLatestPrice(uint itemId, out ulong price)
        {
            if (_priceRecord.TryGetValue(itemId, out var record) && record.LatestPriceRemainingUses > 0 && record.LatestPrice > 0)
            {
                price = record.LatestPrice;
                return true;
            }
            price = 0;
            return false;
        }
        public bool TryConsumeLatestPrice(uint itemId, out ulong price)
        {
            if (_priceRecord.TryGetValue(itemId, out var record))
            {
                if (record.TryConsumeLatestPrice(out price))
                {
                    _priceRecord[itemId] = record;
                    return true;
                }
            }
            price = 0;
            return false;
        }
        public (ulong highestPrice, ulong lowestPrice, ulong averagePrice) GetPriceHistory(uint itemId)
        {
            if (_priceRecord.TryGetValue(itemId, out var record))
            {
                return (record.HighestPrice, record.LowestPrice, record.AveragePrice);
            }
            return (0, 0, 0);
        }
        public void Dispose()
        {
            _disposables.Dispose();
            _onTradePriceChangedSubject.Dispose();
        }
    }
}