using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Core
{
    /// <summary>
    /// 상점에서 취급하는 아이템(IShopItem)을 생성하는 팩토리 인터페이스
    /// </summary>
    public interface IItemShopFactory
    {
        /// <summary>
        /// ID와 수량, 마진을 바탕으로 적절한 IShopItem 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="id">아이템 고유 ID</param>
        /// <param name="stock">상점 진열 수량</param>
        /// <param name="margin">마진 배율 (무기 등 가격 배율)</param>
        /// <returns>생성된 IShopItem, 미지원 ID이거나 데이터가 없으면 null</returns>
        IShopItem Create(uint id, uint stock = 1, float margin = 1.0f);
    }
}
