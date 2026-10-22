namespace ElementalBlacksmithStory.Effect
{
    public class HitContext
    {

    }

    /// <summary>
    /// 피격 시 발동하는 효과 인터페이스
    /// </summary>
    public interface IHitEffect : IEffect
    {
        public void OnHit(ref HitContext context);
    }
}