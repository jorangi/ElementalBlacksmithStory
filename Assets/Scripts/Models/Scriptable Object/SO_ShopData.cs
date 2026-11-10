using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    /// <summary>
    /// 상점 정보 정의 에셋 (하위 카테고리들을 소유)
    /// </summary>
    [CreateAssetMenu(fileName = "SO_ShopData_", menuName = "Scriptable Objects/Shop/SO_ShopData")]
    public class SO_ShopData : ScriptableObject
    {
        [Header("상점 기본 정보")]
        [SerializeField] private MarketType _shopType;
        [SerializeField] private string _shopName;
        [SerializeField] private uint _npcId;
        [Header("대사 설정 (입장/구매 탭, 판매 탭, 구매 완료 탭)")]
        [SerializeField] private SO_DialogueData _buyDialogueId;
        [SerializeField] private SO_DialogueData _sellDialogueId;
        [SerializeField] private SO_DialogueData _purchaseDialogueId;

        [Header("소속 카테고리 목록")]
        [SerializeField] private List<SO_ShopCategory> _categories = new();

        public MarketType ShopType => _shopType;
        public string ShopName => _shopName;
        public uint NpcId => _npcId;
        public uint BuyDialogueId => _buyDialogueId != null ? _buyDialogueId.Id : 0;
        public uint SellDialogueId => _sellDialogueId != null ? _sellDialogueId.Id : 0;
        public uint PurchaseDialogueId => _purchaseDialogueId != null ? _purchaseDialogueId.Id : 0;
        public IReadOnlyList<SO_ShopCategory> Categories => _categories;

        [Header("리프레시 설정")]
        [Tooltip("자동 재입고/새로고침 주기 (분 단위)")]
        [Range(0f, 60f)]
        [SerializeField] private float _refreshIntervalMinutes = 15;
        public TimeSpan RefreshInterval => TimeSpan.FromMinutes(_refreshIntervalMinutes);


        private List<ShopCategoryItem> _cachedItems = new();
        /// <summary>
        /// 저장된 판매품 입고 프로퍼티(공개, 읽기용)
        /// </summary>
        public IReadOnlyList<ShopCategoryItem> AllStockedItems => _cachedItems.AsReadOnly();
        /// <summary>
        /// 판매될 모든 카테고리의 입고 아이템 목록을 취합하여 저장하는 재입고 함수
        /// </summary>
        public void RefreshStockedItems()
        {
            _cachedItems.Clear();
            var seenIds = new HashSet<uint>();
            if (_categories != null)
            {
                foreach (var category in _categories)
                {
                    if (category == null) continue;
                    var stockedItems = category.GetStockedItems();
                    foreach (var item in stockedItems)
                    {
                        if (seenIds.Add(item.itemId))
                        {
                            _cachedItems.Add(item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 등록된 모든 카테고리의 아이템 ID를 중복 없이 취합하여 반환
        /// </summary>
        public List<uint> GetAllItemIds()
        {
            var uniqueIds = new HashSet<uint>();
            if (_categories != null)
            {
                foreach (var category in _categories)
                {
                    if (category == null || category.Items == null) continue;
                    foreach (var item in category.Items)
                    {
                        uniqueIds.Add(item.itemId);
                    }
                }
            }
            return new List<uint>(uniqueIds);
        }
    }
}
