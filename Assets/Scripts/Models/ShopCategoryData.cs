using System;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    /// <summary>
    /// 상점 내 카테고리 데이터 정의 모델
    /// </summary>
    public class ShopCategoryData
    {
        public string Id { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public Func<IShopItem, bool> FilterPredicate { get; }

        public ShopCategoryData(
            string id,
            string displayName,
            Sprite icon = null,
            Func<IShopItem, bool> filterPredicate = null)
        {
            Id = id;
            DisplayName = displayName;
            Icon = icon;
            FilterPredicate = filterPredicate ?? (_ => true);
        }
    }
}
