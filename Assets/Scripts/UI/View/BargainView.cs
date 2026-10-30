using System.Threading;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class BargainView : MonoBehaviour
    {
        private const float ANIM_DURATION = 0.2f;
        /// <summary>
        /// 말풍선 패널
        /// </summary>
        [SerializeField] RectTransform Bubble;
        /// <summary>
        /// 말풍선 텍스트
        /// </summary>
        [SerializeField] TextMeshProUGUI bubbleText;
        [SerializeField] TextMeshProUGUI npcName;
        private CancellationTokenSource _cts;
        private MotionHandle motionHandle;
        [SerializeField] private Color rootPanelColor;
        /// <summary>
        /// 상대 제시 금액보다 낮은 금액을 제시할 때 경고성 색상
        /// </summary>
        [SerializeField] private Color _badColor;
        [SerializeField] private Button _offerButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _warningAcceptButton;
        [SerializeField] private Button _warningCancelButton;
        [SerializeField] private Image _itemImage;
        [SerializeField] private TMP_InputField _offerPrice;
        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private TextMeshProUGUI _itemAmount;
        [SerializeField] private TextMeshProUGUI _highestPrice;
        [SerializeField] private TextMeshProUGUI _lowestPrice;
        [SerializeField] private TextMeshProUGUI _averagePrice;
        [SerializeField] private Button _increaseButton;
        [SerializeField] private Button _decreaseButton;
        [SerializeField] private RectTransform _bargainUI;
        /// <summary>
        /// 상대 제시보다 낮은 금액을 제시했을 때 경고성 패널
        /// </summary>
        [SerializeField] private RectTransform _warningUI;
        [SerializeField] private Image _warningPanel;

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
            EnsureButtonSound(_increaseButton, false);
            EnsureButtonSound(_decreaseButton, false);
            EnsureButtonSound(_offerButton, true);
            EnsureButtonSound(_cancelButton, true);
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
        public Observable<Unit> OnOfferButtonAsObservable()
        {
            if (_offerButton == null)
            {
                Debug.LogError("_offerButton is null");
                return Observable.Empty<Unit>();
            }
            return _offerButton.OnClickAsObservable();
        }
        public Observable<Unit> OnCancelButtonAsObservable()
        {
            if (_cancelButton == null)
            {
                Debug.LogError("_cancelBargainButton is null");
                return Observable.Empty<Unit>();
            }
            return _cancelButton.OnClickAsObservable();
        }
        public Observable<Unit> OnIncreaseButtonAsObservable()
        {
            if (_increaseButton == null)
            {
                Debug.LogError("_increaseButton is null");
                return Observable.Empty<Unit>();
            }
            EnsureHoldButtons();
            return _increaseHoldButton != null ? _increaseHoldButton.OnTickAsObservable() : _increaseButton.OnClickAsObservable();
        }
        public Observable<Unit> OnDecreaseButtonAsObservable()
        {
            if (_decreaseButton == null)
            {
                Debug.LogError("_decreaseButton is null");
                return Observable.Empty<Unit>();
            }
            EnsureHoldButtons();
            return _decreaseHoldButton != null ? _decreaseHoldButton.OnTickAsObservable() : _decreaseButton.OnClickAsObservable();
        }
        public Observable<Unit> OnWarningAcceptButtonAsObservable()
        {
            if (_warningAcceptButton == null)
            {
                Debug.LogError("_warningAcceptButton is null");
                return Observable.Empty<Unit>();
            }
            return _warningAcceptButton.OnClickAsObservable();
        }
        public Observable<Unit> OnWarningCancelButtonAsObservable()
        {
            if (_warningCancelButton == null)
            {
                Debug.LogError("_warningCancelButton is null");
                return Observable.Empty<Unit>();
            }
            return _warningCancelButton.OnClickAsObservable();
        }
        public void SetItem(string itemName, Sprite itemImage, uint amount = 1)
        {
            _itemName.SetText(itemName);
            _itemImage.sprite = itemImage;
            _itemAmount.SetText(amount > 1 ? ZString.Format("x{0}", amount) : string.Empty);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_itemAmount.rectTransform);
        }
        public void SetPriceHistory(ulong highestPrice, ulong lowestPrice, ulong averagePrice)
        {
            _highestPrice.SetText(ZString.Format("최고 판매가: {0:N0}", highestPrice));
            _lowestPrice.SetText(ZString.Format("최저 판매가: {0:N0}", lowestPrice));
            _averagePrice.SetText(ZString.Format("평균 판매가: {0:N0}", averagePrice));
        }
        public void SetOfferPrice(ulong price, bool isLower)
        {
            _offerPrice.SetTextWithoutNotify(ZString.Format("{0:N0}", price));
            _offerPrice.textComponent.color = isLower ? _badColor : Color.white;
        }
        public void SetInteract(bool canBargain)
        {
            _offerButton.interactable = canBargain;
        }
        public Observable<ulong> OnChangedOfferPriceAsObservable()
        {
            if (_offerPrice == null)
            {
                Debug.LogError("_offerPrice is null");
                return Observable.Empty<ulong>();
            }
            return _offerPrice.onValueChanged.AsObservable().Select(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) return 0UL;
                string cleanText = text.Replace(",", "");
                return ulong.TryParse(cleanText, out ulong amount) ? amount : 0UL;
            });
        }

        public void SetDialogue(string text)
        {
            if (bubbleText != null)
            {
                bubbleText.SetText(text);
            }
        }
        public void SetNPCName(string name)
        {
            if (npcName != null)
            {
                npcName.SetText(name);
            }
        }

        public async UniTask Show(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (motionHandle.IsActive())
            {
                motionHandle.Cancel();
            }
            _bargainUI.gameObject.SetActive(true);
            motionHandle = LMotion.Create(Vector3.zero, Vector3.one, ANIM_DURATION)
                            .WithEase(Ease.InOutBack)
                            .BindToLocalScale(_bargainUI)
                            .AddTo(gameObject);

            await motionHandle.ToUniTask(_cts.Token)
                            .SuppressCancellationThrow();
        }
        public async UniTask ShowWarning(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (motionHandle.IsActive())
            {
                motionHandle.Cancel();
            }
            motionHandle = LSequence.Create()
                                .Join(LMotion.Create(Vector3.zero, Vector3.one, ANIM_DURATION)
                                        .WithEase(Ease.InOutBack)
                                        .BindToLocalScale(_warningUI))
                                .Join(LMotion.Create(Color.clear, rootPanelColor, ANIM_DURATION)
                                        .WithEase(Ease.InOutBack)
                                        .BindToColor(_warningPanel))
                                .Run()
                                .AddTo(gameObject);

            await motionHandle.ToUniTask(_cts.Token)
                            .SuppressCancellationThrow();
        }
        public async UniTask HideWarning(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (motionHandle.IsActive())
            {
                motionHandle.Cancel();
            }
            motionHandle = LSequence.Create()
                                .Join(LMotion.Create(Vector3.one, Vector3.zero, ANIM_DURATION)
                                        .WithEase(Ease.InOutBack)
                                        .BindToLocalScale(_warningUI))
                                .Join(LMotion.Create(rootPanelColor, Color.clear, ANIM_DURATION)
                                        .WithEase(Ease.InOutBack)
                                        .BindToColor(_warningPanel))
                                .Run()
                                .AddTo(gameObject);

            await motionHandle.ToUniTask(_cts.Token)
                            .SuppressCancellationThrow();
        }
        public async UniTask Hide(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (motionHandle.IsActive())

            {
                motionHandle.Cancel();
            }
            motionHandle = LMotion.Create(Vector3.one, Vector3.zero, ANIM_DURATION)
                .WithEase(Ease.InOutBack)
                .BindToLocalScale(_bargainUI)
                .AddTo(gameObject);

            await motionHandle.ToUniTask(_cts.Token)
                            .SuppressCancellationThrow();
            _bargainUI.gameObject.SetActive(false);
        }


        private HoldRepeatButton _increaseHoldButton;
        private HoldRepeatButton _decreaseHoldButton;
        /// <summary>
        /// 버튼에 홀드 반복 버튼 추가
        /// </summary>
        private void EnsureHoldButtons()
        {
            if (_increaseButton != null && _increaseHoldButton == null)
            {
                if (!_increaseButton.TryGetComponent<HoldRepeatButton>(out _increaseHoldButton))
                {
                    _increaseHoldButton = _increaseButton.gameObject.AddComponent<HoldRepeatButton>();
                }
            }

            if (_decreaseButton != null && _decreaseHoldButton == null)
            {
                if (!_decreaseButton.TryGetComponent<HoldRepeatButton>(out _decreaseHoldButton))
                {
                    _decreaseHoldButton = _decreaseButton.gameObject.AddComponent<HoldRepeatButton>();
                }
            }
        }
    }
}