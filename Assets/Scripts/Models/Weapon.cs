using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Inventory;
using R3;
using UnityEngine;

namespace ElementalBlacksmithStory.Core
{
    public class Weapon : IShopItem, IDisposable
    {
        private SO_WeaponData _weaponData;
        public SO_WeaponData Data => _weaponData;
        //Weapon 인스턴스 Id 누적용
        private static uint lastId;

        //Weapon 인스턴스의 id
        private readonly uint _id;
        public uint Id => _id;

        //Weapon의 데이터 id
        public uint WeaponId => _weaponData.Id;

        // 최종 비용
        private ulong _cost;
        public ulong Cost => _cost;

        // 누적 가격(+최종, 마진 제외)
        private ulong _basePrice;
        public ulong BasePrice => _basePrice;
        private ulong _price;
        public ulong Price => _price;
        private readonly SO_WeaponData _baseWeaponData;
        public SO_WeaponData BaseWeaponData => _baseWeaponData;
        // 강화 히스토리 스택 (무기 ID, 해당 단계에서 추가된 기본 가격, 해당 단계 비용)
        private readonly Stack<EnhanceStep> _enhanceHistory = new();
        public Stack<EnhanceStep> EnhanceHistory => _enhanceHistory;
        string IShopItem.Name => Data != null ? Data.weaponName : string.Empty;
        public ulong Value => _price;

        uint IShopItem.SpriteId => Data != null ? WeaponId : 0;

        uint IShopItem.Count => 1;

        public void OnPurchased(IInventoryContext context, uint amount)
        {
            context.EquipmentInventory.Add(this);
            for (int i = 1; i < amount; i++)
            {
                context.EquipmentInventory.Add(new Weapon(_weaponData));
            }
        }

        public uint GetOwnedCount(IInventoryContext context)
        {
            int count = context.EquipmentInventory.GetCountByWeaponId(WeaponId);
            return count > 0 ? (uint)count : 0;
        }

        public void OnSold(IInventoryContext context, uint amount)
        {
            context.EquipmentInventory.RemoveByWeaponId(WeaponId, (int)amount);
        }
        private int _runeSlot = 2;
        private readonly Rune[] _runes = new Rune[3];
        private readonly Subject<int> _onRuneSlotChanged = new();
        private readonly Subject<(int socketIndex, Rune rune)> _onRuneChanged = new();

        /// <summary>
        /// 현재 무기에 해금된 룬 소켓 슬롯 수
        /// </summary>
        public int RuneSlot => _runeSlot;

        /// <summary>
        /// 룬 슬롯 목록 읽기 전용 컬렉션
        /// </summary>
        public IReadOnlyList<Rune> Runes => _runes;

        /// <summary>
        /// 룬 슬롯 수 변경 시 발행되는 Observable
        /// </summary>
        public Observable<int> OnRuneSlotChanged => _onRuneSlotChanged;

        /// <summary>
        /// 룬 장착/해제 시 발행되는 Observable (socketIndex, rune)
        /// </summary>
        public Observable<(int socketIndex, Rune rune)> OnRuneChanged => _onRuneChanged;

        /// <summary>
        /// 소켓 인덱스로 룬 조회 (인덱서)
        /// </summary>
        public Rune this[int socketIndex] => GetRune(socketIndex);
        public Weapon(SO_WeaponData weaponData)
        {
            _id = lastId++;
            _weaponData = weaponData;
            _baseWeaponData = weaponData;
            _cost = weaponData != null ? weaponData.cost : 0;
            SetMargin(1f);
        }
        /// <summary>
        /// 마진값 설정
        /// </summary>
        /// <param name="margin"></param>
        public void SetMargin(float margin)
        {
            _price = (ulong)(BasePrice * margin);
        }
        /// <summary>
        /// SO 데이터 설정
        /// </summary>
        /// <param name="weaponData">새로운 SO 데이터</param>
        public void SetData(SO_WeaponData weaponData)
        {
            _weaponData = weaponData;
        }
        /// <summary>
        /// 기본 가격 설정
        /// </summary>
        /// <param name="basePrice">새로운 기본 가격</param>
        public void SetBasePrice(ulong basePrice)
        {
            _basePrice = basePrice;
        }
        /// <summary>
        /// 비용 설정
        /// </summary>
        /// <param name="cost">새로운 비용</param>
        public void SetCost(ulong cost)
        {
            _cost = cost;
        }
        /// <summary>
        /// 강화 히스토리 설정
        /// </summary>
        /// <param name="weaponId">무기 ID</param>
        /// <param name="addedBasePrice">추가된 기본 가격</param>
        /// <param name="nextCost">다음 비용</param>
        public void PushEnhanceStep(uint weaponId, ulong addedBasePrice, ulong nextCost)
        {
            _enhanceHistory.Push(new EnhanceStep(weaponId, addedBasePrice, nextCost));
            _basePrice += addedBasePrice;
            _cost = nextCost;
        }
        /// <summary>
        /// 기본 가격 추가
        /// </summary>
        /// <param name="price">추가할 기본 가격</param>
        public void PushPrice(ulong price)
        {
            _basePrice += price;
        }
        /// <summary>
        /// 기본 가격 초기화
        /// </summary>
        public void ClearPrice()
        {
            _enhanceHistory.Clear();
            _basePrice = 0;
        }
        /// <summary>
        /// 비용 추가
        /// </summary>
        /// <param name="cost">추가할 비용</param>
        public void PushCost(ulong cost)
        {
            _cost = cost;
        }
        /// <summary>
        /// 비용 초기화
        /// </summary>
        public void ClearCost()
        {
            _cost = 0;
        }

        /// <summary>
        /// 소켓에 룬 장착
        /// </summary>
        /// <param name="rune">장착할 룬</param>
        /// <param name="socketIndex">장착할 소켓 인덱스</param>
        /// <returns>장착 해제된 룬, 없으면 null</returns>
        public void EquipRune(Rune rune, int socketIndex, out Rune unequippedRune)
        {
            if (_runeSlot == 0 || socketIndex < 0 || socketIndex >= _runeSlot)
            {
                Debug.LogWarning("소켓 슬롯이 존재하지 않습니다.");
                unequippedRune = null;
                return;
            }
            unequippedRune = _runes[socketIndex];
            _runes[socketIndex] = rune;
            _onRuneChanged.OnNext((socketIndex, rune));
        }

        public bool EquipRune(Rune rune, int socketIndex)
        {
            EquipRune(rune, socketIndex, out _);
            return true;
        }

        /// <summary>
        /// 소켓에서 룬 장착 해제
        /// </summary>
        /// <param name="socketIndex">장착 해제할 소켓 인덱스</param>
        /// <returns>장착 해제된 룬, 없으면 null</returns>
        public void UnequipRune(int socketIndex, out Rune unequippedRune)
        {
            if (_runeSlot == 0 || socketIndex < 0 || socketIndex >= _runeSlot)
            {
                Debug.LogWarning("소켓 슬롯이 존재하지 않습니다.");
                unequippedRune = null;
                return;
            }
            unequippedRune = _runes[socketIndex];
            _runes[socketIndex] = null;
            _onRuneChanged.OnNext((socketIndex, null));
        }

        public bool UnequipRune(int socketIndex)
        {
            UnequipRune(socketIndex, out _);
            return true;
        }

        /// <summary>
        /// 해금된 룬 소켓 슬롯 수 설정 (0 ~ 3)
        /// </summary>
        public void SetRuneSlot(int slotCount)
        {
            _runeSlot = Mathf.Clamp(slotCount, 0, _runes.Length);
            _onRuneSlotChanged.OnNext(_runeSlot);
        }

        /// <summary>
        /// 소켓에 룬 확인
        /// </summary>
        /// <param name="socketIndex">확인할 소켓 인덱스</param>
        /// <returns>장착된 룬</returns>
        public Rune GetRune(int socketIndex)
        {
            if (_runeSlot == 0 || socketIndex < 0 || socketIndex >= _runeSlot)
            {
                Debug.LogWarning("Invalid socket index");
                return null;
            }
            return _runes[socketIndex];
        }

        public void Dispose()
        {
            _onRuneSlotChanged.Dispose();
            _onRuneChanged.Dispose();
        }
    }

    public readonly struct EnhanceStep
    {
        public uint WeaponId { get; }
        public ulong AddedBasePrice { get; }
        public ulong Cost { get; }

        public EnhanceStep(uint weaponId, ulong addedBasePrice, ulong cost)
        {
            WeaponId = weaponId;
            AddedBasePrice = addedBasePrice;
            Cost = cost;
        }
    }
}