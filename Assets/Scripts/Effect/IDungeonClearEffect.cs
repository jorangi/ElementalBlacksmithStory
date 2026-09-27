namespace ElementalBlacksmithStory.Effect
{
    public class DungeonClearContext
    {

    }

    /// <summary>
    /// 던전 클리어 시 발동하는 효과 인터페이스
    /// </summary>
    public interface IDungeonClearEffect : IEffect
    {
        public void OnDungeonClear(ref DungeonClearContext context);
    }
}