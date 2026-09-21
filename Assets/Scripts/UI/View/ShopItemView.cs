using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;
using Cysharp.Text;

namespace ElementalBlacksmithStory.UI
{
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Button _button;

        public uint ItemId { get; private set; }
        public ulong Price { get; private set; }
        public string ItemName { get; private set; }

        private void Awake()
        {
            if (_button != null && !_button.TryGetComponent<UIButtonSound>(out _))
            {
                _button.gameObject.AddComponent<UIButtonSound>();
            }
        }

        public void SetData(uint itemId, string itemName, ulong price, Sprite icon)
        {
            ItemId = itemId;
            ItemName = itemName;
            Price = price;

            if (_nameText != null)
            {
                _nameText.SetText(itemName);
            }

            if (_priceText != null)
            {
                _priceText.SetText(ZString.Format("{0:N0}", price));
            }

            SetIcon(icon);
        }

        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }
        }

        public void SetPrice(ulong price)
        {
            Price = price;
            if (_priceText != null)
            {
                _priceText.SetText(ZString.Format("{0:N0}", price));
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
