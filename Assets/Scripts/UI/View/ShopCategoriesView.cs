using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 상점 카테고리 목록 컨테이너 뷰
    /// </summary>
    public class ShopCategoriesView : MonoBehaviour
    {
        [SerializeField] private Transform _container;
        [SerializeField] private GameObject _categoryButtonPrefab;

        [SerializeField] private Sprite _defaultMaterialIcon;
        public Sprite DefaultMaterialIcon { get => _defaultMaterialIcon; }

        private readonly Dictionary<string, ShopCategoryButtonView> _buttons = new();

        private void Awake()
        {
            EnsureContainer();
        }

        private void EnsureContainer()
        {
            if (_container == null)
            {
                var scrollRect = GetComponentInChildren<ScrollRect>(true);
                if (scrollRect != null && scrollRect.content != null)
                {
                    _container = scrollRect.content;
                }
            }
        }

        /// <summary>
        /// 카테고리 버튼 생성
        /// </summary>
        public ShopCategoryButtonView CreateCategoryButton(string categoryId, string displayName, Sprite icon)
        {
            EnsureContainer();

#if UNITY_EDITOR
            if (_categoryButtonPrefab == null)
            {
                _categoryButtonPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ShopCategoryButton.prefab");
            }
#endif

            if (_buttons.TryGetValue(categoryId, out var existing))
            {
                existing.SetData(categoryId, displayName, icon);
                return existing;
            }

            GameObject instance = null;
            if (_categoryButtonPrefab != null && _container != null)
            {
                instance = Instantiate(_categoryButtonPrefab, _container);
            }
            else if (_container != null)
            {
                // 프리팹이 미지정된 경우 컨테이너 내 기존 배치된 자식 중 이름 매칭 또는 첫번째 프리팹 복제
                foreach (Transform child in _container)
                {
                    if (child.name.Equals(displayName, System.StringComparison.OrdinalIgnoreCase) ||
                        child.name.Equals(categoryId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        instance = child.gameObject;
                        break;
                    }
                }

                if (instance == null && _container.childCount > 0)
                {
                    instance = Instantiate(_container.GetChild(0).gameObject, _container);
                }
            }

            if (instance == null)
            {
                Debug.LogWarning($"[ShopCategoriesView] 카테고리 버튼 생성 실패: {categoryId}");
                return null;
            }

            instance.name = $"Category_{categoryId}";
            var buttonView = instance.GetComponent<ShopCategoryButtonView>();
            if (buttonView == null)
            {
                buttonView = instance.AddComponent<ShopCategoryButtonView>();
            }

            buttonView.SetData(categoryId, displayName, icon);
            _buttons[categoryId] = buttonView;
            return buttonView;
        }

        /// <summary>
        /// 특정 카테고리 버튼 선택 비주얼 갱신
        /// </summary>
        public void SetSelectedVisual(string selectedCategoryId)
        {
            foreach (var pair in _buttons)
            {
                pair.Value.SetSelected(pair.Key == selectedCategoryId);
            }
        }

        /// <summary>
        /// 등록된 모든 카테고리 버튼 반환
        /// </summary>
        public IEnumerable<ShopCategoryButtonView> GetAllButtons() => _buttons.Values;

        /// <summary>
        /// 생성된 모든 카테고리 버튼 정리
        /// </summary>
        public void Clear()
        {
            foreach (var view in _buttons.Values)
            {
                if (view != null && view.gameObject != null)
                {
                    Destroy(view.gameObject);
                }
            }
            _buttons.Clear();
        }
    }
}