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
    public class BargainRegisterItemDetailView : MonoBehaviour
    {
        private const float ANIM_DURATION = 0.2f;

        [SerializeField] private RectTransform _rect;
        [SerializeField] private Color _panelColor;
        [SerializeField] private Image _panel;
        [SerializeField] private Button _decreaseButton;
        [SerializeField] private Button _increaseButton;
        [SerializeField] private TextMeshProUGUI _inventoryAmount;
        [SerializeField] private TMP_InputField _amountInput;
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private Button _registerButton;
        [SerializeField] private Button _cancelButton;

        private CancellationTokenSource _cts;
        private MotionHandle motionHandle;

        private void Awake()
        {
            EnsureButtonSounds();
            EnsureHoldButtons();
        }
        private void EnsureButtonSounds()
        {
            EnsureButtonSound(_increaseButton, false);
            EnsureButtonSound(_decreaseButton, false);
            EnsureButtonSound(_registerButton, true);
            EnsureButtonSound(_cancelButton, true);
        }
        private void EnsureButtonSound(Button btn, bool autoBind)
        {
            if (btn != null && !btn.TryGetComponent<UIButtonSound>(out var sound))
            {
                sound = btn.gameObject.AddComponent<UIButtonSound>();
                sound.SetAutoBindOnClick(autoBind);
            }
        }
        public void SetData(string itemName, Sprite sprite, uint amount = 1)
        {
            SetItemName(itemName);
            SetIcon(sprite);
            SetAmount(amount);
        }
        public void SetItemName(string itemName)
        {
            _itemName.SetText(itemName);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_itemName.rectTransform);
        }
        public void SetIcon(Sprite sprite)
        {
            _itemIcon.sprite = sprite;
        }
        public void SetAmount(uint amount = 1)
        {
            _inventoryAmount.SetText(amount > 1 ? ZString.Format("x{0:N0}", amount) : string.Empty);
        }
        public void SetRegisterAmount(uint amount = 1)
        {
            _amountInput.SetTextWithoutNotify(ZString.Format("{0:N0}", amount));
        }
        private HoldRepeatButton _increaseHoldButton;
        private HoldRepeatButton _decreaseHoldButton;
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
        public Observable<uint> OnAmountChangedAsObservable()
        {
            if (_amountInput == null)
            {
                return Observable.Empty<uint>();
            }
            return _amountInput.onValueChanged.AsObservable().Select(text =>
            {
                if (string.IsNullOrEmpty(text)) return 0u;
                string cleanText = text.Replace(",", "");
                return uint.TryParse(cleanText, out uint amount) ? amount : 0u;
            });
        }
        public Observable<Unit> OnRegisterButtonAsObservable()
        {
            if (_registerButton == null)
            {
                Debug.LogWarning("[BargainRegisterItemDetailView] _registerButton이 존재하지 않습니다.");
                return Observable.Empty<Unit>();
            }
            return _registerButton.OnClickAsObservable();
        }
        public Observable<Unit> OnCancelButtonAsObservable()
        {
            if (_cancelButton == null)
            {
                Debug.LogWarning("[BargainRegisterItemDetailView] _cancelButton이 존재하지 않습니다.");
                return Observable.Empty<Unit>();
            }
            return _cancelButton.OnClickAsObservable();
        }
        public Observable<Unit> OnIncreaseButtonAsObservable()
        {
            if (_increaseButton == null)
            {
                Debug.LogError("[BargainRegisterItemDetailView] _increaseButton이 존재하지 않습니다.");
                return Observable.Empty<Unit>();
            }
            EnsureHoldButtons();
            return _increaseHoldButton != null ? _increaseHoldButton.OnTickAsObservable() : _increaseButton.OnClickAsObservable();
        }
        public Observable<Unit> OnDecreaseButtonAsObservable()
        {
            if (_decreaseButton == null)
            {
                Debug.LogError("[BargainRegisterItemDetailView] _decreaseButton이 존재하지 않습니다.");
                return Observable.Empty<Unit>();
            }
            EnsureHoldButtons();
            return _decreaseHoldButton != null ? _decreaseHoldButton.OnTickAsObservable() : _decreaseButton.OnClickAsObservable();
        }
        public async UniTask Show(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (motionHandle.IsActive())
            {
                motionHandle.Cancel();
            }
            _rect.gameObject.SetActive(true);
            motionHandle = LSequence.Create()
                                .Append(LMotion.Create(Color.clear, _panelColor, ANIM_DURATION)
                                        .WithEase(Ease.InOutBack)
                                        .BindToColor(_panel))
                                .Append(LMotion.Create(Vector3.zero, Vector3.one, ANIM_DURATION)
                                        .WithEase(Ease.InOutBack)
                                        .BindToLocalScale(_rect))
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
            motionHandle = LSequence.Create()
                                .Append(LMotion.Create(Vector3.one, Vector3.zero, ANIM_DURATION)
                                        .WithEase(Ease.InOutBack)
                                        .BindToLocalScale(_rect))
                                .Append(LMotion.Create(_panelColor, Color.clear, ANIM_DURATION)
                                        .WithEase(Ease.InOutBack)
                                        .BindToColor(_panel))
                                .Run()
                                .AddTo(gameObject);

            await motionHandle.ToUniTask(_cts.Token)
                            .SuppressCancellationThrow();
            _rect.gameObject.SetActive(false);
        }
    }
}