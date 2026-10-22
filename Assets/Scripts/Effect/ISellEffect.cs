namespace ElementalBlacksmithStory.Effect
{
    public class SellContext
    {

    }

    /// <summary>
    /// 아이템 판매 시 발동하는 효과 인터페이스
    /// </summary>
    public interface ISellEffect : IEffect
    {
        public void OnSell(ref SellContext context);
    }
}