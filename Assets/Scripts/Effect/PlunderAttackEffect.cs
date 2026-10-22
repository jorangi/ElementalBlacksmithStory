using System;
using UnityEngine;

namespace ElementalBlacksmithStory.Effect
{
    /// <summary>
    /// 적을 공격할 때 일정 확률로 골드를 강탈
    /// </summary>
    [Serializable]
    public class PlunderAttackEffect : IAttackEffect
    {
        [Header("약탈 설정")]
        [Tooltip("약탈 발동 확률 (%)")]
        [SerializeField, Range(0f, 100f)] private float chance = 15f;

        [Tooltip("피해량 비례 골드 획득률 (1%)")]
        [SerializeField] private float goldPerDamage = 0.01f;

        public string Description => $"공격 적중 시 {chance}% 확률로 피해량의 {goldPerDamage * 100}% 만큼 골드를 약탈합니다.";

        public void OnEquip()
        {
        }

        public void OnUnequip()
        {
        }

        public void OnAttack(ref AttackContext context)
        {
            if (UnityEngine.Random.value * 100f <= chance)
            {
                ulong goldAmount = (ulong)(context.Damage * goldPerDamage);
                Debug.Log($"[약탈 성공] {goldAmount:N0} 골드를 획득했습니다! (발동 확률: {chance}%)");
            }
        }
    }
}
