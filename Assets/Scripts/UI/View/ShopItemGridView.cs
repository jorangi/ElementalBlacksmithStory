using System.Collections.Generic;
using UnityEngine;
using R3;

namespace ElementalBlacksmithStory.UI
{
    public class ShopItemGridView : MonoBehaviour
    {
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private GameObject _itemPrefab;

        private readonly Dictionary<uint, ShopItemView> _itemViews = new();
        private readonly Subject<ShopItemView> _onItemClickedSubject = new();
        private CompositeDisposable _itemDisposables = new();

        /// <summary>
        /// 아이템 클릭 이벤트 스트림
        /// </summary>
        public Observable<ShopItemView> OnItemClickedAsObservable() => _onItemClickedSubject;

        /// <summary>
        /// 특정 아이템 ID의 뷰 반환
        /// </summary>
        public ShopItemView GetItemView(uint itemId) => _itemViews.TryGetValue(itemId, out var view) ? view : null;

        /// <summary>
        /// 현재 생성된 모든 아이템 뷰 반환
        /// </summary>
        public IEnumerable<ShopItemView> GetAllItemViews() => _itemViews.Values;

        /// <summary>
        /// 그리드에 새 아이템을 생성하여 배치
        /// </summary>
        public ShopItemView CreateItem(uint itemId, string itemName, ulong price, Sprite icon)
        {
            if (_gridContainer == null || _itemPrefab == null)
            {
                Debug.LogWarning("[ShopItemGridView] _gridContainer 또는 _itemPrefab이 설정되지 않았습니다.");
                return null;
            }

            // 이미 존재하는 경우 기존 항목 제거 후 재생성
            if (_itemViews.ContainsKey(itemId))
            {
                RemoveItem(itemId);
            }

            GameObject instance = Instantiate(_itemPrefab, _gridContainer);
            instance.name = $"{itemName}_{itemId}";

            if (instance.TryGetComponent<ShopItemView>(out var itemView))
            {
                itemView.SetData(itemId, itemName, price, icon);
                itemView.OnClickAsObservable()
                    .Subscribe(_ => _onItemClickedSubject.OnNext(itemView))
                    .AddTo(_itemDisposables);

                _itemViews[itemId] = itemView;
                return itemView;
            }

            Debug.LogError("[ShopItemGridView] _itemPrefab에 ShopItemView 컴포넌트가 존재하지 않습니다.");
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
            _itemDisposables.Dispose();
            _itemDisposables = new CompositeDisposable();
            _itemViews.Clear();

            if (_gridContainer == null) return;

            foreach (Transform child in _gridContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void OnDestroy()
        {
            _itemDisposables.Dispose();
            _onItemClickedSubject.Dispose();
        }
    }
}