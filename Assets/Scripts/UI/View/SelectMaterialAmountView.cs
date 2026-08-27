using UnityEngine;
using UnityEngine.UI;
using R3;
using TMPro;
using Cysharp.Text;
using UnityEngine.EventSystems;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class SelectMaterialAmountView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI materialNameText;
        [SerializeField] private TMP_InputField countText;
        [SerializeField] private Button decreseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;
        private MaterialItemView _itemView;
        public uint MaterialCount { get; private set; } // 재료 최대개수

        public void SetData(Sprite sprite, string name, uint maxAmount)
        {
            panel.SetActive(true);
            iconImage.sprite = sprite;
            materialNameText.SetText(name);
            MaterialCount = maxAmount;
            countText.SetTextWithoutNotify("0");
        }
        public Observable<uint> OnChangedAmountAsObservable()
        {
            if(countText == null)
            {
                Debug.LogError("[SelectMaterialAmountView] countText가 없습니다.");
                return Observable.Empty<uint>();
            }
            return countText.onValueChanged.AsObservable().Select(text => uint.TryParse(text, out uint amount) ? amount : 0);
        }
        private HoldRepeatButton _increaseHoldButton;
        private HoldRepeatButton _decreaseHoldButton;

        private void Awake()
        {
            EnsureHoldButtons();
        }

        private void EnsureHoldButtons()
        {
            if (increaseButton != null && _increaseHoldButton == null)
            {
                _increaseHoldButton = increaseButton.GetComponent<HoldRepeatButton>() 
                                      ?? increaseButton.gameObject.AddComponent<HoldRepeatButton>();
            }

            if (decreseButton != null && _decreaseHoldButton == null)
            {
                _decreaseHoldButton = decreseButton.GetComponent<HoldRepeatButton>() 
                                      ?? decreseButton.gameObject.AddComponent<HoldRepeatButton>();
            }
        }

        public Observable<Unit> OnIncreaseAsObservable()
        {
            if (increaseButton == null)
            {
                Debug.LogError("[SelectMaterialAmountView] increaseButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            EnsureHoldButtons();
            return _increaseHoldButton != null ? _increaseHoldButton.OnTickAsObservable() : increaseButton.OnClickAsObservable();
        }
        public Observable<Unit> OnDecreaseAsObservable()
        {
            if (decreseButton == null)
            {
                Debug.LogError("[SelectMaterialAmountView] decreseButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            EnsureHoldButtons();
            return _decreaseHoldButton != null ? _decreaseHoldButton.OnTickAsObservable() : decreseButton.OnClickAsObservable();
        }
        public Observable<Unit> OnSubmitAsObservable()
        {
            if (submitButton == null)
            {
                Debug.LogError("[SelectMaterialAmountView] submitButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return submitButton.OnClickAsObservable();
        }
        public Observable<Unit> OnCancelAsObservable()
        {
            if (cancelButton == null)
            {
                Debug.LogError("[SelectMaterialAmountView] cancelButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return cancelButton.OnClickAsObservable();
        }
        public void Hide()
        {
            panel.SetActive(false);
        }
        public void SetAmount(uint amount)
        {
            countText?.SetTextWithoutNotify(ZString.Format("{0:N0}", amount));
        }
    }
}
