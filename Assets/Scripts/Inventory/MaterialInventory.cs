using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Inventory
{
    public class MaterialInventory : IStartable, IDisposable
    {
        private readonly Dictionary<SO_MaterialData, uint> _inventory = new();
        private readonly Subject<(SO_MaterialData material, uint count)> _onItemCountChangedSubject = new();
        private readonly CompositeDisposable _disposables = new();

        public Observable<(SO_MaterialData material, uint count)> OnItemCountChangedAsObservable => _onItemCountChangedSubject;

        public void Start()
        {

        }
        public void Add(SO_MaterialData materialData, uint amount)
        {
            if (materialData == null)
            {
                Debug.LogError($"[MaterialInventory] 추가할 아이템이 없습니다.");
                return;
            }
            if (_inventory.ContainsKey(materialData))
            {
                _inventory[materialData] += amount;
            }
            else
            {
                _inventory.Add(materialData, amount);
            }
            _onItemCountChangedSubject.OnNext((materialData, _inventory[materialData]));
        }
        public void Remove(SO_MaterialData materialData)
        {
            if (!_inventory.ContainsKey(materialData))
            {
                Debug.LogError($"[MaterialInventory] {materialData.Id}를 찾을 수 없습니다.");
                return;
            }
            _inventory.Remove(materialData);
            _onItemCountChangedSubject.OnNext((materialData, 0));
        }
        /// <summary>
        /// 가방에서 재료를 선택함
        /// </summary>
        /// <param name="materialData">선택한 재료 데이터</param>
        /// <param name="amount">선택한 재료 개수</param>
        /// <returns>선택된 재료와 개수</returns>
        public (SO_MaterialData, uint) Get(SO_MaterialData materialData, uint amount)
        {
            if (_inventory.TryGetValue(materialData, out uint count))
            {
                if (amount > count)
                {
                    Debug.LogError($"[MaterialInventory] {materialData.Id}가 {amount - count}개 부족합니다.");
                    return (null, 0);
                }
                _inventory[materialData] -= amount;
                _onItemCountChangedSubject.OnNext((materialData, _inventory[materialData]));
                return (materialData, amount);
            }
            Debug.LogError($"[MaterialInventory] {materialData.Id}를 찾을 수 없습니다.");
            return (null, 0);
        }
        public IReadOnlyDictionary<SO_MaterialData, uint> GetAll() => _inventory;
        public uint GetCount(SO_MaterialData materialData) => _inventory.TryGetValue(materialData, out uint count) ? count : 0;
        public void Clear()
        {
            _inventory.Clear();
        }
        public void Dispose()
        {
            _onItemCountChangedSubject.Dispose();
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}