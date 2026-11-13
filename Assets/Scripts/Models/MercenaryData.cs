using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Inventory;
using UnityEngine;
using VContainer;
namespace ElementalBlacksmithStory.Data
{
    public class MercenaryData : IIdentifiable
    {
        private readonly SO_Mercenary _baseData;
        public uint Id => _baseData.Id;
        public string Name => _baseData.name;

        [Header("스탯 데이터")]
        private static readonly int StatTypeCount = Enum.GetValues(typeof(StatType)).Length;
        private readonly Stat[] _stats = new Stat[StatTypeCount];
        public IReadOnlyList<Stat> Stats => _stats;
        private float _hp; //현재 체력
        private readonly List<ModifierHandle> _weaponHandles = new(); //장착 무기 Modifier 핸들

        [Header("스킬 데이터")]
        private readonly List<SkillData> _skills = new();

        [Header("인벤토리 데이터")]
        private EquipmentInventory _eqiupmentInventory;
        private ConsumableInventory _consumableInventory;
        private MaterialInventory _materialInventory;
        private RuneInventory _runeInventory;

        [Header("장비 데이터")]
        private Weapon _weapon;
        //추후 방어구나 장신구 등이 추가될 수 있음
        public MercenaryData(SO_Mercenary baseData, Weapon weapon = null, IReadOnlyDictionary<StatType, IReadOnlyList<StatModifier>> modifiers = null)
        {
            _baseData = baseData;

            var empty = Array.Empty<StatModifier>();
            for (int i = 0; i < StatTypeCount; i++)
            {
                StatType statType = (StatType)i;
                IReadOnlyList<StatModifier> mods = null;
                modifiers?.TryGetValue(statType, out mods);
                _stats[i] = new Stat(baseData.GetBaseStat(statType), mods ?? empty);
            }

            _hp = GetStatValue(StatType.HP);
            if (weapon != null)
            {
                EquipWeapon(weapon);
            }
        }
        /// <summary>
        /// 스탯 값을 조회하는 함수
        /// </summary>
        public float GetStatValue(StatType statType)
        {
            int idx = (int)statType;
            if (idx < 0 || idx >= StatTypeCount) return 0f;
            return _stats[idx].Value;
        }
        /// <summary>
        /// 스탯을 조회하는 함수
        /// </summary>
        public Stat GetStat(StatType statType)
        {
            int idx = (int)statType;
            if (idx < 0 || idx >= StatTypeCount) return null;
            return _stats[idx];
        }
        /// <summary>
        /// 피해를 입는 함수
        /// </summary>
        /// <param name="damage">피해량</param>
        /// <returns>해당 공격이 체력 이상의 피해인지</returns>
        public bool TakeDamage(float damage)
        {
            //임시 단순 계산 및 로직
            float actualDamage = Mathf.Max(0, damage - GetStatValue(StatType.DEF));
            _hp = Mathf.Clamp(_hp - actualDamage, 0, GetStatValue(StatType.HP));
            return _hp == 0;
        }
        /// <summary>
        /// 장비를 장착하는 함수
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns></returns>
        public bool EquipWeapon(Weapon weapon)
        {
            if (weapon == null) return false;
            if (_weapon != null && !UnequipWeapon())
            {
                Debug.LogWarning("[MercenaryData]장비를 해제할 수 없어 새로운 장비를 착용할 수 없습니다.");
                return false;
            }
            _weapon = weapon;
            for (int i = 0; i < weapon.Stats.Count && i < StatTypeCount; i++)
            {
                float val = weapon.Stats[i].Value;
                if (val != 0f)
                {
                    var handle = _stats[i].AddModifier(new StatModifier((StatType)i, StatModifierType.Flat, val));
                    _weaponHandles.Add(handle);
                }
            }
            return true;
        }
        /// <summary>
        /// 장비 착용 해제하는 함수
        /// </summary>
        /// <returns></returns>
        public bool UnequipWeapon()
        {
            if (_weapon == null)
            {
                Debug.LogWarning("[MercenaryData]장비가 없습니다.");
                return false;
            }
            for (int i = 0; i < _weaponHandles.Count; i++)
            {
                _weaponHandles[i].Dispose();
            }
            _weaponHandles.Clear();
            _weapon = null;
            return true;
        }
    }
}