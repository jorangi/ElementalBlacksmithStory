using System.Collections.Generic;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    /// <summary>
    /// 상점 카테고리 정의 (카테고리에서 판매할 아이템 및 재입고 규칙 관리)
    /// </summary>
    [CreateAssetMenu(fileName = "SO_ShopCategory_", menuName = "Scriptable Objects/Shop/SO_ShopCategory")]
    public class SO_ShopCategory : ScriptableObject
    {
        [Header("카테고리 기본 정보")]
        [SerializeField] private string _categoryId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        [Tooltip("고정(OFF)인 유동 아이템 중 재입고 시 무작위 추첨할 최소 품목 수")]
        [SerializeField] private int _minRandomPickCount = 0;
        [Tooltip("고정(OFF)인 유동 아이템 중 재입고 시 무작위 추첨할 최대 품목 수")]
        [SerializeField] private int _maxRandomPickCount = 1;

        [Header("판매 아이템 목록")]
        [SerializeField] private List<ShopCategoryItem> _items = new();

        public string CategoryId => _categoryId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public int MinRandomPickCount => _minRandomPickCount;
        public int MaxRandomPickCount => _maxRandomPickCount;
        public IReadOnlyList<ShopCategoryItem> Items => _items;

        /// <summary>
        /// 재입고 실행: 고정 아이템 전체 + 유동 아이템 중 min~max개 무작위 추첨 목록 반환
        /// </summary>
        public List<ShopCategoryItem> GetStockedItems()
        {
            var result = new List<ShopCategoryItem>();
            var randomPool = new List<ShopCategoryItem>();

            foreach (var item in _items)
            {
                if (item.isFixed)
                {
                    result.Add(item);
                }
                else
                {
                    randomPool.Add(item);
                }
            }

            if (randomPool.Count > 0)
            {
                // Fisher-Yates 셔플
                for (int i = randomPool.Count - 1; i > 0; i--)
                {
                    int r = Random.Range(0, i + 1);
                    (randomPool[i], randomPool[r]) = (randomPool[r], randomPool[i]);
                }

                int min = Mathf.Clamp(_minRandomPickCount, 0, randomPool.Count);
                int max = Mathf.Clamp(_maxRandomPickCount, min, randomPool.Count);
                int pickCount = Random.Range(min, max + 1);

                for (int i = 0; i < pickCount; i++)
                {
                    result.Add(randomPool[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 해당 아이템이 이 카테고리에 속해 있는지 확인
        /// </summary>
        public bool Contains(IShopItem item)
        {
            if (item == null) return false;
            return ContainsId(item.SpriteId) || ContainsId(item.Id);
        }

        /// <summary>
        /// 특정 아이템 ID가 이 카테고리에 등록되어 있는지 확인
        /// </summary>
        public bool ContainsId(uint id)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].itemId == id) return true;
            }
            return false;
        }
    }
}
