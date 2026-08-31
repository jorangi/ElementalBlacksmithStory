using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Core
{
    public class Weapon
    {
        private SO_WeaponData _weaponData;
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
        // 강화 히스토리 스택 (무기 ID, 해당 단계에서 추가된 기본 가격, 해당 단계 비용)
        private Stack<EnhanceStep> _enhanceHistory = new();

        public Weapon(SO_WeaponData weaponData)
        {
            _id = lastId++;
            _weaponData = weaponData;
            _cost = weaponData != null ? weaponData.cost : 0;
            SetMargin(1f);
        }
        public void SetMargin(float margin)
        {
            _price = (ulong)(BasePrice * margin);
        }
        public void SetData(SO_WeaponData weaponData)
        {
            _weaponData = weaponData;
        }
        public void PushEnhanceStep(uint weaponId, ulong addedBasePrice, ulong nextCost)
        {
            _enhanceHistory.Push(new EnhanceStep(weaponId, addedBasePrice, nextCost));
            _basePrice += addedBasePrice;
            _cost = nextCost;
        }

        public void RollbackTo(uint targetWeaponId, ulong targetCost)
        {
            while (_enhanceHistory.Count > 0 && _enhanceHistory.Peek().WeaponId != targetWeaponId)
            {
                var popped = _enhanceHistory.Pop();
                if (_basePrice >= popped.AddedBasePrice)
                {
                    _basePrice -= popped.AddedBasePrice;
                }
                else
                {
                    _basePrice = 0;
                }
            }
            _cost = targetCost;
        }

        public void PushPrice(ulong price)
        {
            _basePrice += price;
        }

        public void ClearPrice()
        {
            _enhanceHistory.Clear();
            _basePrice = 0;
        }
        
        public void PushCost(ulong cost)
        {
            _cost = cost;
        }

        public void ClearCost()
        {
            _cost = 0;
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