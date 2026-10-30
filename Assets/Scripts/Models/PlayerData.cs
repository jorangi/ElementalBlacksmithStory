using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data.DTO;
using ElementalBlacksmithStory.Inventory;
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

        /// <summary>
        /// 소지금
        /// </summary>
        public ReadOnlyReactiveProperty<ulong> Money => _money;
        [Inject]
        public PlayerData(
            MaterialInventory materialInventory,
            RuneInventory runeInventory,
            EquipmentInventory equipmentInventory)
        {
            this.MaterialInventory = materialInventory;
            this.RuneInventory = runeInventory;
            this.EquipmentInventory = equipmentInventory;
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
            _money.Value += amount;
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
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}