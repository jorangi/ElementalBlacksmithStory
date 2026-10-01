using System;

namespace ElementalBlacksmithStory.Data
{
    /// <summary>
    /// 상점 카테고리 내 개별 판매 아이템 설정 (수량 및 고정/유동 입고 여부)
    /// </summary>
    [Serializable]
    public struct ShopCategoryItem
    {
        public uint itemId;
        public uint stock;
        public bool isFixed;

        public ShopCategoryItem(uint itemId, uint stock = 1, bool isFixed = true)
        {
            this.itemId = itemId;
            this.stock = stock;
            this.isFixed = isFixed;
        }
    }
}
