using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.UI.Strategy
{
    public interface IItemInteractionStrategy
    {
        /// <summary>
        /// 해당 아이템의 그리드 표시 가격(상점 원가, 경매 시작가/최고가 등)을 반환
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public ulong GetPrice(IShopItem item);

        /// <summary>
        /// 해당 아이템을 상호작용(클릭) 가능한 상태인지
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CanInteract(IShopItem item);
        /// <summary>
        /// 해당 아이템을 클릭 했을 때
        /// </summary>
        /// <param name="item"></param>
        public void OnItemClicked(IShopItem item);
    }
}