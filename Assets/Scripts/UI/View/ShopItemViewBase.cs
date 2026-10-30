using Cysharp.Text;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 상점 아이템 카드의 기본 공통 뷰 컴포넌트입니다.
    /// 아이콘, 이름, 가격, 구매 버튼, 인터랙션(구매 가능 여부에 따른 반투명 처리)을 담당합니다.
    /// </summary>
    public abstract class ShopItemViewBase : MonoBehaviour
    {
        [Header("기본 요소")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] protected TextMeshProUGUI _priceText;
        [SerializeField] private Button _button;

        [Tooltip("구매 불가 시 반투명")]
        [SerializeField] private CanvasGroup _canvasGroup;

        public uint ItemId { get; protected set; }
        public ulong Price { get; protected set; }
        public string ItemName { get; protected set; }
        public bool IsUniqueItem { get; protected set; }

        protected virtual void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (_button == null)
            {
                _button = GetComponentInChildren<Button>();
            }

            if (_button != null && !_button.TryGetComponent<UIButtonSound>(out _))
            {
                _button.gameObject.AddComponent<UIButtonSound>();
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = interactable ? 1.0f : 0.55f;
            }
        }

        public virtual void SetData(uint itemId, string itemName, ulong price, Sprite icon, bool isUniqueItem = false)
        {
            ItemId = itemId;
            ItemName = itemName;
            Price = price;
            IsUniqueItem = isUniqueItem;
            SetName(itemName);
            SetPrice(price);
            SetIcon(icon);
        }

        public virtual void SetAmountInPocket(uint amount)
        {
            // 기본 베이스에서는 보유 수량 미표시 (장비 등)
        }

        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }
        }

        public virtual void SetPrice(ulong price)
        {
            Price = price;
            if (_priceText != null)
            {
                _priceText.SetText(ZString.Format("<size=70%><sprite name=\"CoinSack\">{0:N0}</size>", price));
            }
        }

        public void SetName(string itemName)
        {
            ItemName = itemName;
            if (_nameText != null)
            {
                _nameText.SetText(itemName);
            }
        }

        public Observable<Unit> OnClickAsObservable()
        {
            if (_button == null)
            {
                return Observable.Empty<Unit>();
            }
            return _button.OnClickAsObservable();
        }
    }
}
