using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using R3;
using VContainer.Unity;
using UnityEngine;
using System.Linq;

namespace ElementalBlacksmithStory.Inventory
{
    public class EquipmentInventory : IStartable, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private List<Weapon> _inventory = new();
        private List<Weapon> _activeInventory = new();
        private Subject<Weapon> _onAddWeapon = new();
        private Subject<uint> _onRemoveWeapon = new();
        public Observable<Weapon> OnAddWeapon => _onAddWeapon;
        public Observable<uint> OnRemoveWeapon => _onRemoveWeapon;
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
            if(_inventory.Count > idx)
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
            if(_inventory.Count > idx && _inventory[(int)idx] != null)
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
        public int Count()=>_inventory.Count;
        /// <summary>
        /// 비어있지 않고 채워져있는 슬롯 개수
        /// </summary>
        /// <returns></returns>
        public int ItemCount()
        {
            int n = 0;
            foreach(var i in _inventory)
            {
                if(i != null) n++;
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
            foreach(var i in _inventory)
            {
                if(i.WeaponId == weaponId) count++;
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
            for(int i = _inventory.Count - 1; i > 0; i--)
            {
                var item = _inventory[i];
                if(item.WeaponId == weaponId)
                {
                    Remove((uint)i);
                    count--;
                }
                if(count == 0) break;
            }
            return true;
        }
        public void Start()
        {
            
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
            for(int i = _inventory.Count-1; i > 0; i++)
            {
                if(_inventory[i] == null)
                {
                    _inventory.RemoveAt(i);
                    n++;
                }
            }
            Debug.Log($"[EquipmentInventory]마지막 인덱스로부터 총 {n}개의 빈 슬롯을 정리했습니다.");
        }
        public void Dispose()
        {
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}