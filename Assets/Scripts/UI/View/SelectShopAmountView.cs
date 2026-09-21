using System;
using Cysharp.Text;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class SelectShopAmountView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI materialNameText;
        [SerializeField] private TextMeshProUGUI finalPrice;
        [SerializeField] private TextMeshProUGUI pocketAmount;
        [SerializeField] private TextMeshProUGUI remainingWallet;
        [SerializeField] private TMP_InputField countText;
        [SerializeField] private Button decreseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;

        public uint MaterialCount { get; private set; } // 최대 수량
        public bool IsActivated => panel != null && panel.activeSelf;

        private HoldRepeatButton _increaseHoldButton;
        private HoldRepeatButton _decreaseHoldButton;

        private void Awake()
        {
            EnsureHoldButtons();
            EnsureButtonSounds();
        }

        /// <summary>
        /// 선택 창 설정
        /// </summary>
        /// <param name="sprite">아이템 이미지</param>
        /// <param name="name">아이템 이름</param>
        /// <param name="unitPrice">개당 단가</param>
        /// <param name="currentAmount">현재 카트 수량</param>
        /// <param name="maxAmount">최대 수량</param>
        public void SetData(Sprite sprite, string name, ulong unitPrice, uint currentAmount, uint maxAmount)
        {
            if (panel != null) panel.SetActive(true);
            gameObject.SetActive(true);
            if (iconImage != null) iconImage.sprite = sprite;
            if (materialNameText != null)
            {
                materialNameText.SetText(ZString.Format("{0}({1:N0})", name, unitPrice));
            }
            MaterialCount = maxAmount;
            SetAmount(currentAmount);
        }

        public void SetAmount(uint amount)
        {
            if (countText != null)
            {
                countText.SetTextWithoutNotify(ZString.Format("{0:N0}", amount));
            }
            UpdateButtonStates(amount);
        }

        private void UpdateButtonStates(uint amount)
        {
            if (MaterialCount == 0)
            {
                if (increaseButton != null) increaseButton.interactable = false;
                if (decreseButton != null) decreseButton.interactable = false;
                if (countText != null) countText.interactable = false;
                return;
            }

            if (increaseButton != null)
            {
                increaseButton.interactable = amount < MaterialCount;
            }

            if (decreseButton != null)
            {
                decreseButton.interactable = amount > 0;
            }

            if (countText != null)
            {
                countText.interactable = true;
            }
        }
        public void SetPocketEA(uint ea)
        {
            if (pocketAmount != null)
            {
                pocketAmount.SetText(ZString.Format("소지: {0:N0}개", ea));
            }
        }
        public void SetFinalPrice(ulong price)
        {
            if (finalPrice != null)
            {
                finalPrice.SetText(ZString.Format("총합: <sprite name=\"CoinSack\">{0:N0}", price));
            }
        }

        public void SetRemainingWallet(ulong money)
        {
            if (remainingWallet != null)
            {
                remainingWallet.SetText(ZString.Format("잔여금: {0:N0}", money));
            }
        }


        public Observable<uint> OnChangedAmountAsObservable()
        {
            if (countText == null) return Observable.Empty<uint>();
            return countText.onValueChanged.AsObservable().Select(text => uint.TryParse(text, out uint amount) ? amount : 0);
        }

        private void EnsureButtonSounds()
        {
            EnsureButtonSound(increaseButton, false);
            EnsureButtonSound(decreseButton, false);
            EnsureButtonSound(submitButton, true);
            EnsureButtonSound(cancelButton, true);
        }

        private void EnsureButtonSound(Button btn, bool autoBind)
        {
            if (btn != null && !btn.TryGetComponent<UIButtonSound>(out var sound))
            {
                sound = btn.gameObject.AddComponent<UIButtonSound>();
                sound.SetAutoBindOnClick(autoBind);
            }
        }

        private void EnsureHoldButtons()
        {
            if (increaseButton != null && _increaseHoldButton == null)
            {
                if (!increaseButton.TryGetComponent<HoldRepeatButton>(out _increaseHoldButton))
                {
                    _increaseHoldButton = increaseButton.gameObject.AddComponent<HoldRepeatButton>();
                }
            }

            if (decreseButton != null && _decreaseHoldButton == null)
            {
                if (!decreseButton.TryGetComponent<HoldRepeatButton>(out _decreaseHoldButton))
                {
                    _decreaseHoldButton = decreseButton.gameObject.AddComponent<HoldRepeatButton>();
                }
            }
        }

        public Observable<Unit> OnIncreaseAsObservable()
        {
            if (increaseButton == null) return Observable.Empty<Unit>();
            EnsureHoldButtons();
            return _increaseHoldButton != null ? _increaseHoldButton.OnTickAsObservable() : increaseButton.OnClickAsObservable();
        }

        public Observable<Unit> OnDecreaseAsObservable()
        {
            if (decreseButton == null) return Observable.Empty<Unit>();
            EnsureHoldButtons();
            return _decreaseHoldButton != null ? _decreaseHoldButton.OnTickAsObservable() : decreseButton.OnClickAsObservable();
        }

        public Observable<Unit> OnPurchaseAsObservable() => purchaseButton != null ? purchaseButton.OnClickAsObservable() : Observable.Empty<Unit>();
        public Observable<Unit> OnSubmitAsObservable() => submitButton != null ? submitButton.OnClickAsObservable() : Observable.Empty<Unit>();
        public Observable<Unit> OnCancelAsObservable() => cancelButton != null ? cancelButton.OnClickAsObservable() : Observable.Empty<Unit>();

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}