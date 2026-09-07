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
        public bool IsActivated => panel != null && panel.activeSelf;

        /// <summary>
        /// 선택 창 설정
        /// </summary>
        /// <param name="sprite">재료 이미지</param>
        /// <param name="name">재료 이름</param>
        /// <param name="maxAmount">최대 수량</param>
        public void SetData(Sprite sprite, string name, uint maxAmount)
        {
            panel.SetActive(true);
            iconImage.sprite = sprite;
            materialNameText.SetText(name);
            MaterialCount = maxAmount;
            countText.SetTextWithoutNotify("0");
        }
        /// <summary>
        /// 입력창에 입력한 값의 Observable
        /// </summary>
        /// <returns></returns>
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
            EnsureButtonSounds();
        }

        /// <summary>
        /// 버튼에 UI버튼 사운드 추가
        /// </summary>
        private void EnsureButtonSounds()
        {
            EnsureButtonSound(increaseButton, false);
            EnsureButtonSound(decreseButton, false);
            EnsureButtonSound(submitButton, true);
            EnsureButtonSound(cancelButton, true);
        }

        /// <summary>
        /// 버튼에 사운드 추가
        /// </summary>
        /// <param name="btn">버튼</param>
        /// <param name="autoBind">클릭 시 자동 바인딩</param>
        private void EnsureButtonSound(Button btn, bool autoBind)
        {
            if (btn != null && !btn.TryGetComponent<UIButtonSound>(out var sound))
            {
                sound = btn.gameObject.AddComponent<UIButtonSound>();
                sound.SetAutoBindOnClick(autoBind);
            }
        }
        /// <summary>
        /// 버튼에 홀드 반복 버튼 추가
        /// </summary>
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
        /// <summary>
        /// 증가 버튼 클릭
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// 감소 버튼 클릭
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// 확인 버튼 클릭
        /// </summary>
        /// <returns></returns>
        public Observable<Unit> OnSubmitAsObservable()
        {
            if (submitButton == null)
            {
                Debug.LogError("[SelectMaterialAmountView] submitButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return submitButton.OnClickAsObservable();
        }
        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        /// <returns></returns>
        public Observable<Unit> OnCancelAsObservable()
        {
            if (cancelButton == null)
            {
                Debug.LogError("[SelectMaterialAmountView] cancelButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return cancelButton.OnClickAsObservable();
        }
        /// <summary>
        /// 창 숨김
        /// </summary>
        public void Hide()
        {
            panel.SetActive(false);
        }
        /// <summary>
        /// 입력한 값 설정
        /// </summary>
        /// <param name="amount">설정할 값</param>
        public void SetAmount(uint amount)
        {
            if(countText == null)
            {
                Debug.LogError("[SelectMaterialAmountView] countText가 없습니다.");
                return;
            }
            countText.SetTextWithoutNotify(ZString.Format("{0:N0}", amount));
        }
    }
}
