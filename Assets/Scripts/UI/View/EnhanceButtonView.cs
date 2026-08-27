using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class EnhanceButtonView : MonoBehaviour
    {
        [SerializeField] private Button enhanceButton;
        private RectTransform _rectTransform;
        private CancellationTokenSource _cts;
        private float _defaultY = 0f;
        [SerializeField] private float targetShowY = 295f;

        private void Awake()
        {
            if (enhanceButton != null)
            {
                _rectTransform = enhanceButton.GetComponent<RectTransform>();
                if (_rectTransform != null)
                {
                    _defaultY = _rectTransform.anchoredPosition.y;
                }
            }
        }

        public Observable<Unit> OnClickAsObservable()
        {
            if (enhanceButton == null)
            {
                Debug.LogError("[EnhanceButtonView] enhanceButton이 연결되지 않았습니다.");
                return Observable.Empty<Unit>();
            }
            return enhanceButton.OnClickAsObservable();
        }

        /// <summary>
        /// 강화 버튼 보이게/올라가게 하는 애니메이션 (목표 Y: targetShowY)
        /// </summary>
        public async UniTaskVoid ShowingButtonAnimation()
        {
            if (_rectTransform == null) return;

            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new();
            var token = _cts.Token;

            while (_rectTransform.anchoredPosition.y < targetShowY - 1f)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
                _rectTransform.anchoredPosition = Vector2.Lerp(_rectTransform.anchoredPosition, new Vector2(_rectTransform.anchoredPosition.x, targetShowY), 0.1f);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, targetShowY);
        }

        /// <summary>
        /// 강화 버튼 숨기게/원위치로 내리는 애니메이션
        /// </summary>
        public async UniTaskVoid HidingButtonAnimation()
        {
            if (_rectTransform == null) return;

            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new();
            var token = _cts.Token;

            while (_rectTransform.anchoredPosition.y > _defaultY + 1f)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
                _rectTransform.anchoredPosition = Vector2.Lerp(_rectTransform.anchoredPosition, new Vector2(_rectTransform.anchoredPosition.x, _defaultY), 0.1f);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, _defaultY);
        }

        /// <summary>
        /// 드래그 등으로 Y 위치를 실시간 동기화
        /// </summary>
        public void SyncYPosition(float y)
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, y);
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}