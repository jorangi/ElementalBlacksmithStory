namespace ElementalBlacksmithStory.Effect
{
    public class AttackContext
    {
        // 공격측
        // 피해측
        /// <summary>
        /// 피해량
        /// </summary>
        public ulong Damage;
    }

    /// <summary>
    /// 공격 시 발동하는 효과 인터페이스
    /// </summary>
    public interface IAttackEffect : IEffect
    {
        public void OnAttack(ref AttackContext context);
    }
}