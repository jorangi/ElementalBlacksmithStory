using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 상점 카테고리 단일 버튼 뷰
    /// </summary>
    public class ShopCategoryButtonView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _button;
        [SerializeField] private Image _backgroundImage;

        private static readonly Color SelectedColor = new Color(1.0f, 0.7451f, 0.5922f, 1f);
        private static readonly Color UnselectedColor = Color.white;

        public string CategoryId { get; private set; }

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_backgroundImage == null) _backgroundImage = GetComponent<Image>();

            if (_iconImage == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.gameObject != gameObject)
                    {
                        _iconImage = img;
                        break;
                    }
                }
            }

            if (_titleText == null)
            {
                _titleText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        public void SetData(string categoryId, string displayName, Sprite icon = null)
        {
            EnsureReferences();

            CategoryId = categoryId;

            if (_titleText != null)
            {
                _titleText.SetText(displayName);
            }

            if (icon != null && _iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = true;
            }
        }

        public void SetSelected(bool isSelected)
        {
            EnsureReferences();

            if (_backgroundImage != null)
            {
                _backgroundImage.color = isSelected ? SelectedColor : UnselectedColor;
            }

            if (_button != null)
            {
                _button.interactable = !isSelected;
            }
        }

        public Observable<Unit> OnClickAsObservable()
        {
            EnsureReferences();
            if (_button == null) return Observable.Empty<Unit>();
            return _button.OnClickAsObservable();
        }
    }
}
