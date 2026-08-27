using System;
using R3;
using VContainer.Unity;
using VContainer;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using UnityEngine;

namespace ElementalBlacksmithStory.Inventory
{
    public class MaterialInventory : IStartable, IDisposable
    {
        private readonly Dictionary<SO_MaterialData, uint> _inventory = new();
        private readonly CompositeDisposable _disposables = new();
        public void Start()
        {
            
        }
        public void Add(SO_MaterialData materialData, uint amount)
        {
            if(materialData == null)
            {
                Debug.LogError($"[MaterialInventory] 추가할 아이템이 없습니다.");
                return;
            }
            if(_inventory.ContainsKey(materialData))
            {
                _inventory[materialData] += amount;
                return;
            }
            _inventory.Add(materialData, amount);
        }
        public void Remove(SO_MaterialData materialData)
        {
            if(!_inventory.ContainsKey(materialData))
            {
                Debug.LogError($"[MaterialInventory] {materialData.Id}를 찾을 수 없습니다.");
                return;
            }
            _inventory.Remove(materialData);
        }
        public (SO_MaterialData, uint) GetMaterial(SO_MaterialData materialData, uint amount)
        {
            if(_inventory.TryGetValue(materialData, out uint count))
            {
                if(amount > count)
                {
                    Debug.LogError($"[MaterialInventory] {materialData.Id}가 {amount - count}개 부족합니다.");
                    return (null, 0);
                }
                _inventory[materialData] -= amount;
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
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}