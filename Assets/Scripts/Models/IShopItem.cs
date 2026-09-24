namespace ElementalBlacksmithStory.Data
{
    public interface IShopItem : IIdentifiable
    {
        /// <summary>
        /// 화면에 표시될 아이템 이름
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 판매 또는 구매 가격
        /// </summary>
        ulong Price { get; }

        /// <summary>
        /// 스프라이트 아틀라스 로드에 사용되는 원본 데이터 ID
        /// </summary>
        uint SpriteId { get; }

        /// <summary>
        /// 아이템 수량
        /// </summary>
        uint Count { get; }
    }
}
