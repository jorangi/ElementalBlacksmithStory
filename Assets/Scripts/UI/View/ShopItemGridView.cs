using System.Collections.Generic;
using UnityEngine;

namespace ElementalBlacksmithStory.UI
{
    public class ShopItemGridView : MonoBehaviour
    {
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private GameObject _equipmentItemPrefab;
        [SerializeField] private GameObject _bargainItemPrefab;
        [SerializeField] private GameObject _bargainEquipmentItemPrefab;

        private readonly Dictionary<uint, ShopItemViewBase> _itemViews = new();

        private void Awake()
        {
            Debug.Log($"[ShopItemGridView] Awake - _gridContainer: {_gridContainer}, _itemPrefab: {_itemPrefab}, _equipmentItemPrefab: {_equipmentItemPrefab}");
        }

        /// <summary>
        /// 특정 아이템 ID의 뷰 반환
        /// </summary>
        public ShopItemViewBase GetItemView(uint itemId) => _itemViews.TryGetValue(itemId, out var view) ? view : null;

        /// <summary>
        /// 현재 생성된 모든 아이템 뷰 반환
        /// </summary>
        public IEnumerable<ShopItemViewBase> GetAllItemViews() => _itemViews.Values;

        /// <summary>
        /// 그리드에 새 아이템을 생성하여 배치
        /// </summary>
        public ShopItemViewBase CreateItem(uint itemId, string itemName, ulong price, Sprite icon, bool isUniqueItem = false, bool isBargain = false)
        {
            if (_gridContainer == null || _itemPrefab == null)
            {
                Debug.LogWarning($"[ShopItemGridView] _gridContainer is null: {_gridContainer == null}, _itemPrefab is null: {_itemPrefab == null}");
                return null;
            }

            // 이미 존재하는 경우 기존 항목 제거 후 재생성
            if (_itemViews.ContainsKey(itemId))
            {
                RemoveItem(itemId);
            }

            GameObject prefabToUse;
            if (isBargain)
            {
                if (isUniqueItem)
                {
                    prefabToUse = _bargainEquipmentItemPrefab != null ? _bargainEquipmentItemPrefab : _equipmentItemPrefab;
                }
                else
                {
                    prefabToUse = _bargainItemPrefab != null ? _bargainItemPrefab : _itemPrefab;
                }
            }
            else
            {
                prefabToUse = (isUniqueItem && _equipmentItemPrefab != null) ? _equipmentItemPrefab : _itemPrefab;
            }

            GameObject instance = Instantiate(prefabToUse, _gridContainer);
            instance.name = $"{itemName}_{itemId}";

            if (instance.TryGetComponent<ShopItemViewBase>(out var itemView))
            {
                itemView.SetData(itemId, itemName, price, icon, isUniqueItem);
                _itemViews[itemId] = itemView;
                return itemView;
            }

            Debug.LogError($"[ShopItemGridView] {prefabToUse.name}에 ShopItemViewBase 컴포넌트가 존재하지 않습니다.");
            return null;
        }

        /// <summary>
        /// 특정 아이템 항목 제거
        /// </summary>
        public void RemoveItem(uint itemId)
        {
            if (_itemViews.TryGetValue(itemId, out var itemView))
            {
                _itemViews.Remove(itemId);
                if (itemView != null)
                {
                    Destroy(itemView.gameObject);
                }
            }
        }

        /// <summary>
        /// 그리드의 모든 아이템 제거 및 정리
        /// </summary>
        public void Clear()
        {
            _itemViews.Clear();

            if (_gridContainer == null) return;

            foreach (Transform child in _gridContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}

