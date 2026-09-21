using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class ShopCartItemView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _ea;
        [SerializeField] private Button _btn;
        [SerializeField] private Image _icon;
        private readonly Subject<Unit> _onPointerUpSubject = new();
        private readonly Subject<Unit> _onLongPressSubject = new();
        private readonly Subject<Unit> _onClickSubject = new();
        private bool _isLongPressed = false;

        /// <summary>
        /// 길게 눌렀을 때
        /// </summary>
        /// <returns></returns>
        public Observable<Unit> OnLongPressAsObservable() => _onLongPressSubject;

        /// <summary>
        /// 카트의 아이템의 아이콘을 설정
        /// </summary>
        /// <param name="sprite"></param>
        public void SetIcon(Sprite sprite)
        {
            _icon.sprite = sprite;
        }
        /// <summary>
        /// 카트의 아이템 개수를 설정
        /// </summary>
        /// <param name="ea"></param>
        public void SetEA(uint ea)
        {
            _ea.SetText($"x{ea}");
        }
        /// <summary>
        /// 카트의 아이템 클릭
        /// </summary>
        /// <returns></returns>
        public Observable<Unit> OnClickAsObservable() => _onClickSubject;
        private float _pointerDownTime;
        private const float CLICK_THRESHOLD = 0.3f;

        public void OnPointerClick(PointerEventData e)
        {
            if (_isLongPressed || (Time.unscaledTime - _pointerDownTime) >= CLICK_THRESHOLD) return;
            _onClickSubject.OnNext(Unit.Default);
            Debug.LogWarning("클릭함");
        }
        /// <summary>
        /// 누름 이벤트
        /// </summary>
        /// <param name="e"></param>
        public void OnPointerDown(PointerEventData e)
        {
            _pointerDownTime = Time.unscaledTime;
            _isLongPressed = false;
            StartHoldAnimationAsync().Forget();
        }
        /// <summary>
        /// 뗌 이벤트
        /// </summary>
        /// <param name="e"></param>
        public void OnPointerUp(PointerEventData e)
        {
            CancelHoldAnimation();
        }
        private CancellationTokenSource _cts;
        [SerializeField] private Image _trashCan;
        [SerializeField] private Image _gaugeDonut;
        /// <summary>
        /// 홀드 프레스 애니메이션 실행
        /// </summary>
        /// <returns></returns>
        public async UniTask StartHoldAnimationAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(CLICK_THRESHOLD), cancellationToken: ct);
                _isLongPressed = true;
                _trashCan.gameObject.SetActive(true);
                _gaugeDonut.gameObject.SetActive(true);
                _gaugeDonut.color = Color.white;

                float dur = 0.6f;
                float elapsed = 0f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    _gaugeDonut.fillAmount = Mathf.Clamp01(elapsed / dur);
                    _gaugeDonut.color = new Color(1f, 1 - _gaugeDonut.fillAmount, 1f - _gaugeDonut.fillAmount);
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
                _onLongPressSubject.OnNext(Unit.Default);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("손을 뗌");
            }
            finally
            {
                CancelHoldAnimation();
            }
        }
        /// <summary>
        /// 홀드 프레스 애니메이션 취소
        /// </summary>
        private void CancelHoldAnimation()
        {
            _cts?.Cancel();
            _cts = null;
            if (_trashCan != null) _trashCan.gameObject.SetActive(false);
            if (_gaugeDonut != null)
            {
                _gaugeDonut.fillAmount = 0f;
                _gaugeDonut.gameObject.SetActive(false);
            }
        }
    }
}