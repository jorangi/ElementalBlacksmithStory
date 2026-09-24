using UnityEngine;
using System;

namespace ElementalBlacksmithStory.Data
{
    public enum ElementType
    {
        Flame,
        Cold,
        Lightning,
        Wind,
        Earth,
        Holy,
        Dark
    }
    public enum StatusEffectType
    {
        Poison,
        Burn,
        Freeze,
        Shock,
        Slow,
        Stun,
        Root,
        Silence,
        Blind,
        Charm,
        Fear,
        Curse,
        Bleed,
        Stagger,
        Taunt,
        Lifesteal,
        ReflectDamage,
        Invincible,
        Shield,
        Heal,
        Regen,
        Haste,
        DefenseDown,
        AttackDown,
    }
    [Serializable]
    public class StatusEffectData
    {
        public StatusEffectType type;
        [Tooltip("상태이상 지속 시간(초)")]
        public float duration;
        [Tooltip("상태이상 효과 수치")]
        public float value;
    }
}