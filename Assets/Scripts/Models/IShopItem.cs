using ElementalBlacksmithStory.Inventory;

namespace ElementalBlacksmithStory.Data
{
    /// <summary>
    /// 상점에서 판매/구매 가능한 아이템 인터페이스
    /// </summary>
    public interface IShopItem : IIdentifiable, IValuable
    {
        /// <summary>
        /// 화면에 표시될 아이템 이름
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 스프라이트 아틀라스 로드에 사용되는 원본 데이터 ID
        /// </summary>
        public uint SpriteId { get; }

        /// <summary>
        /// 아이템 수량
        /// </summary>
        public uint Count { get; }

        /// <summary>
        /// 구매 완료 시 플레이어 인벤토리에 지급
        /// </summary>
        void OnPurchased(IInventoryContext context, uint amount);

        /// <summary>
        /// 판매 검증 시 플레이어의 현재 소지량 조회
        /// </summary>
        uint GetOwnedCount(IInventoryContext context);

        /// <summary>
        /// 판매 완료 시 플레이어 인벤토리에서 차감
        /// </summary>
        void OnSold(IInventoryContext context, uint amount);
    }
}
