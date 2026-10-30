using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Data.DTO;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Inventory
{
    public class EquipmentInventory : IInventory, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly SO_RuneDatabase _runeDatabase;
        private readonly List<Weapon> _inventory = new();
        private readonly List<Weapon> _activeInventory = new();
        private readonly Subject<Weapon> _onAddWeapon = new();
        private readonly Subject<uint> _onRemoveWeapon = new();
        public Observable<Weapon> OnAddWeapon => _onAddWeapon;
        public Observable<uint> OnRemoveWeapon => _onRemoveWeapon;

        [Inject]
        public EquipmentInventory(
            ISubscriber<AcquireItemEvent> acquireSubscriber,
            SO_WeaponDatabase weaponDatabase,
            SO_RuneDatabase runeDatabase)
        {
            _weaponDatabase = weaponDatabase;
            _runeDatabase = runeDatabase;

            acquireSubscriber.Subscribe(e =>
            {
                // 무기 카테고리가 아니면 무시
                if (ItemIdHelper.GetCategory(e.ItemId) != ItemCategory.Weapon)
                {
                    return;
                }

                Weapon weapon = e.Weapon;
                if (weapon == null && _weaponDatabase != null)
                {
                    var data = _weaponDatabase.Get(e.ItemId);
                    if (data != null)
                    {
                        weapon = new Weapon(data);
                    }
                }

                if (weapon != null)
                {
                    Add(weapon);
                    Debug.Log($"[EquipmentInventory] 무기 획득 처리 완료: [{weapon.WeaponId}]{weapon.Data?.weaponName} (Sender: {e.Sender ?? "Unknown"}, Target: {e.Target ?? "Player"}, Reason: {e.Reason})");
                }
            }).AddTo(_disposables);
        }
        /// <summary>
        /// 장비 인벤토리에 아이템 추가(일단 현재는 Weapon만 지원)
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns></returns>
        public uint Add(Weapon weapon)
        {
            _inventory.Add(weapon);
            _activeInventory.Add(weapon);
            _onAddWeapon.OnNext(weapon);
            return (uint)_inventory.Count;
        }
        /// <summary>
        /// 슬롯 스왑 등 아이템을 다른 위치로 이동
        /// </summary>
        /// <param name="start"></param>
        /// <param name="dest"></param>
        /// 
        public void Move(int start, int dest) => (_inventory[start], _inventory[dest]) = (_inventory[dest], _inventory[start]);
        /// <summary>
        /// 인벤토리의 idx슬롯의 아이템을 제거(리스트의 크기는 바뀌지 않음)
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool Remove(uint idx)
        {
            if (_inventory.Count > idx)
            {
                //_inventory[(int)idx].Dispose 함수
                _activeInventory.Remove(_inventory[(int)idx]);
                _inventory[(int)idx] = null;
                _onRemoveWeapon.OnNext(idx);
                return true;
            }
            return false;
        }
        /// <summary>
        /// idx번째 아이템을 Get(현재는 Weapon만 지원)
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public Weapon Get(uint idx)
        {
            if (_inventory.Count > idx && _inventory[(int)idx] != null)
                return _inventory[(int)idx];
            Debug.LogError($"[EquipmentInventory] {idx}번 슬롯에 장비가 없습니다.");
            return null;
        }
        /// <summary>
        /// Read 전용 모든 인벤토리 슬롯 반환
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<Weapon> GetAll() => _inventory;
        public IReadOnlyCollection<Weapon> GetAllActive() => _activeInventory;
        /// <summary>
        /// 인벤토리의 크기(중간의 null인 빈슬롯을 포함함)
        /// </summary>
        /// <returns></returns>
        public int Count() => _inventory.Count;
        /// <summary>
        /// 비어있지 않고 채워져있는 슬롯 개수
        /// </summary>
        /// <returns></returns>
        public int ItemCount()
        {
            int n = 0;
            foreach (var i in _inventory)
            {
                if (i != null) n++;
            }
            return n;
        }
        /// <summary>
        /// 해당 아이템이 인벤토리에 몇 개 있는지
        /// </summary>
        /// <param name="weaponId"></param>
        /// <returns></returns>
        public int GetCountByWeaponId(uint weaponId)
        {
            int count = 0;
            foreach (var i in _inventory)
            {
                if (i != null && i.WeaponId == weaponId) count++;
            }
            return count;
        }
        /// <summary>
        /// 인벤토리에서 해당하는 아이템을 선택한 개수만큼 제거하는 함수
        /// </summary>
        /// <param name="weaponId">제거할 아이템 id</param>
        /// <param name="count">해당 아이템을 몇개 제거할지, 기본 값 -1: 선택된 id를 지닌 모든 슬롯을 비움</param>
        /// <returns></returns>
        public bool RemoveByWeaponId(uint weaponId, int count = -1)
        {
            if (count > 0 && GetCountByWeaponId(weaponId) < count)
                return false;
            for (int i = _inventory.Count - 1; i >= 0; i--)
            {
                var item = _inventory[i];
                if (item != null && item.WeaponId == weaponId)
                {
                    Remove((uint)i);
                    count--;
                }
                if (count == 0) break;
            }
            return true;
        }
        /// <summary>
        /// 인벤토리 클리어
        /// </summary>
        public void Clear()
        {
            _inventory.Clear();
        }
        /// <summary>
        /// 뒤에서부터 빈 슬롯을 제거
        /// </summary>
        public void TrimInventory()
        {
            int n = 0;
            for (int i = _inventory.Count - 1; i >= 0; i--)
            {
                if (_inventory[i] == null)
                {
                    _inventory.RemoveAt(i);
                    n++;
                }
            }
            Debug.Log($"[EquipmentInventory]마지막 인덱스로부터 총 {n}개의 빈 슬롯을 정리했습니다.");
        }
        public List<WeaponSaveData> ToSaveData()
        {
            var list = new List<WeaponSaveData>(_inventory.Count);
            for (int i = 0; i < _inventory.Count; i++)
            {
                var weapon = _inventory[i];
                list.Add(weapon?.ToSaveData());
            }
            return list;
        }

        public void LoadSaveData(List<WeaponSaveData> data)
        {
            _inventory.Clear();
            _activeInventory.Clear();
            if (data == null || _weaponDatabase == null) return;
            for (int i = 0; i < data.Count; i++)
            {
                var saveData = data[i];
                if (saveData == null)
                {
                    _inventory.Add(null);
                    continue;
                }
                var weaponData = _weaponDatabase.Get(saveData.WeaponDataId);
                var baseWeaponData = _weaponDatabase.Get(saveData.BaseWeaponDataId);
                var weapon = Weapon.FromSaveData(saveData, weaponData, baseWeaponData, _runeDatabase);
                _inventory.Add(weapon);
                if (weapon != null)
                {
                    _activeInventory.Add(weapon);
                }
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}