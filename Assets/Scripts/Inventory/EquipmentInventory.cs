using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using R3;
using VContainer.Unity;
using UnityEngine;

namespace ElementalBlacksmithStory.Inventory
{
    public class EquipmentInventory : IStartable, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        public Dictionary<uint, Weapon> _inventory = new();
        private Subject<Weapon> _onAddWeapon = new();
        private Subject<uint> _onRemoveWeapon = new();
        public Observable<Weapon> OnAddWeapon => _onAddWeapon;
        public Observable<uint> OnRemoveWeapon => _onRemoveWeapon;
        public void Add(Weapon weapon)
        {
            _inventory[weapon.Id] = weapon;
            _onAddWeapon.OnNext(weapon);
        }
        public bool Remove(uint id)
        {
            if(_inventory.Remove(id))
            {
                _onRemoveWeapon.OnNext(id);
                return true;
            }
            return false;
        }
        public Weapon Get(uint id)
        {
            if(_inventory.TryGetValue(id, out var weapon))
            {
                return weapon;
            }
            Debug.LogError($"[EquipmentInventory] {id}를 찾을 수 없습니다.");
            return null;
        }
        public IReadOnlyCollection<Weapon> GetAll() => _inventory.Values;
        public void Start()
        {
            
        }
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