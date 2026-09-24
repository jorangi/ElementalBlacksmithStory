using UnityEngine;
using System;

namespace ElementalBlacksmithStory.Data
{
    public enum ElementType
    {
        Flame, // 불
        Cold, // 냉기
        Lightning, // 번개
        Wind, // 바람
        Earth, // 땅
        Holy, // 신성
        Dark // 어둠
    }
    public enum StatusEffectType
    {
        Poison, //독
        Burn, // 화상
        Freeze, //얼어붙음
        Shock, //감전
        Slow, //감속
        Stun, //기절
        Root, //구속
        Silence, // 침묵
        Blind, // 실명
        Charm, // 매혹
        Fear, // 공포
        Curse, // 저주
        Bleed, // 출혈
        Stagger, // 경직
        Taunt, // 도발
        Lifesteal, // 흡혈
        ReflectDamage, // 반사 피해
        Invincible, // 무적
        Shield, // 보호막
        Heal, // 치유
        Regen, // 재생
        Haste, // 가속
        DefenseDown, // 방어력 감소
        AttackDown, // 공격력 감소
    }
    [Serializable]
    public class StatusEffectData
    {
        /// <summary>
        /// 상태이상 종류
        /// </summary>
        public StatusEffectType type;
        /// <summary>
        /// 상태이상 지속 시간(초)
        /// </summary>
        public float duration;
        /// <summary>
        /// 상태이상 효과 수치
        /// </summary>
        public float value;
    }
}