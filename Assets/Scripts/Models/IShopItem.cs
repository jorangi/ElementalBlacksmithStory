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
    }
}
