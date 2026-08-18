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
        private Stack<ulong> _stackedCost = new();
        public ulong Cost => _cost;

        // 누적 가격(+최종, 마진 제외)
        private Stack<ulong> _stackedBasePrice = new();
        private ulong _basePrice;
        public ulong BasePrice => _basePrice;
        private ulong _price;
        public ulong Price => _price;
        public Weapon(SO_WeaponData weaponData)
        {
            _id = lastId++;
            _weaponData = weaponData;
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
        public void PushPrice(ulong price)
        {
            _stackedBasePrice.Push(price);
            _basePrice += price;
        }

        public void PopPrice()
        {
            if (_stackedBasePrice.Count > 0)
            {
                _basePrice -= _stackedBasePrice.Pop();
            }
        }
        public void ClearPrice()
        {
            _stackedBasePrice.Clear();
            _basePrice = 0;
        }
        
        public void PushCost(ulong cost)
        {
            _stackedCost.Push(cost);
            _cost += cost;
        }
        public void PopCost()
        {
            if (_stackedCost.Count > 0)
            {
                _cost -= _stackedCost.Pop();
            }
        }
        public void ClearCost()
        {
            _stackedCost.Clear();
            _cost = 0;
        }
        
    }
}