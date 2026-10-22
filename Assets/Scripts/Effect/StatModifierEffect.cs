using System;
using ElementalBlacksmithStory.Data;
using UnityEngine;

namespace ElementalBlacksmithStory.Effect
{
    /// <summary>
    /// 스탯을 보정해주는 상시 효과
    /// </summary>
    [Serializable]
    public class StatModifierEffect : IEffect
    {
        [SerializeField] private StatType statType;
        [SerializeField] private float value;
        [SerializeField] private StatModifierType statModifierType;

        public StatType StatType => statType;
        public float Value => value;
        public StatModifierType StatModifierType => statModifierType;

        public string Description => $"{statType} +{value}";
        public void OnEquip() { }
        public void OnUnequip() { }
    }
}