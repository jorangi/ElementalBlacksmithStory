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
    public class BidView : MonoBehaviour
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
        /// 상위 입찰자가 타인일 때의 텍스트 색상
        /// </summary>
        [SerializeField] private Color _badColor;
        /// <summary>
        /// 상위 입찰자가 본인일 때의 텍스트 색상
        /// </summary>
        [SerializeField] private Color _goodColor;
        [SerializeField] private Button _bidButton;
        [SerializeField] private Button _cancelBidButton;
        [SerializeField] private Image _itemImage;
        [SerializeField] private TMP_InputField _bidPrice;
        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private TextMeshProUGUI _itemAmount;
        [SerializeField] private TextMeshProUGUI _highestBidPrice;
        [SerializeField] private TextMeshProUGUI _highestBidder;
        [SerializeField] private TextMeshProUGUI _remainingWallet;
        [SerializeField] private Button _increaseButton;
        [SerializeField] private Button _decreaseButton;
        [SerializeField] private RectTransform _bidUI;

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
            EnsureButtonSound(_bidButton, true);
            EnsureButtonSound(_cancelBidButton, true);
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
        public Observable<Unit> OnBidButtonAsObservable()
        {
            if (_bidButton == null)
            {
                Debug.LogError("_bidButton is null");
                return Observable.Empty<Unit>();
            }
            return _bidButton.OnClickAsObservable();
        }
        public Observable<Unit> OnCancelButtonAsObservable()
        {
            if (_cancelBidButton == null)
            {
                Debug.LogError("_cancelBidButton is null");
                return Observable.Empty<Unit>();
            }
            return _cancelBidButton.OnClickAsObservable();
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
        public void SetItem(string itemName, Sprite itemImage, uint amount = 1)
        {
            _itemName.SetText(itemName);
            _itemImage.sprite = itemImage;
            _itemAmount.SetText(amount > 1 ? ZString.Format("x{0}", amount) : string.Empty);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_itemAmount.rectTransform);
        }
        public void SetHighestBid(string bidder = "", ulong price = 0)
        {
            _highestBidder.SetText(ZString.Format("상위 입찰자: {0}", string.IsNullOrEmpty(bidder) ? "없음" : bidder == "player" ? "나" : bidder));
            _highestBidder.color = bidder == "player" ? _goodColor : bidder == "" ? Color.white : _badColor;
            _highestBidPrice.SetText(ZString.Format("현재 입찰액: {0:N0}", price));
            _highestBidPrice.color = bidder == "player" ? _goodColor : bidder == "" ? Color.white : _badColor;
        }
        public void SetBidPrice(ulong price)
        {
            _bidPrice.SetTextWithoutNotify(ZString.Format("{0:N0}", price));
        }
        public void SetInteract(bool canBid)
        {
            _bidButton.interactable = canBid;
        }
        public Observable<ulong> OnChangedBidPriceAsObservable()
        {
            if (_bidPrice == null)
            {
                Debug.LogError("_bidPrice is null");
                return Observable.Empty<ulong>();
            }
            return _bidPrice.onValueChanged.AsObservable().Select(text =>
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
            _bidUI.gameObject.SetActive(true);
            motionHandle = LMotion.Create(Vector3.zero, Vector3.one, ANIM_DURATION)
                            .WithEase(Ease.InOutBack)
                            .BindToLocalScale(_bidUI)
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
                .BindToLocalScale(_bidUI)
                .AddTo(gameObject);

            await motionHandle.ToUniTask(_cts.Token)
                            .SuppressCancellationThrow();
            _bidUI.gameObject.SetActive(false);
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