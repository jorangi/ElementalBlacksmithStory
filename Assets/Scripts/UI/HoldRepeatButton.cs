using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 버튼을 누르고(Hold) 있으면 점진적으로 가속하며 이벤트를 연속 발생시키는 컴포넌트
    /// </summary>
    public class HoldRepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Hold Settings")]
        [Tooltip("꾹 누르고 있을 때 첫 반복이 시작되기까지의 대기 시간(초)")]
        [SerializeField] private float initialDelay = 0.35f;

        [Tooltip("연속 입력 시작 시 초기 주기(초)")]
        [SerializeField] private float initialInterval = 0.15f;

        [Tooltip("최대 속도에 도달했을 때 최소 주기(초)")]
        [SerializeField] private float minInterval = 0.025f;

        [Tooltip("매 반복마다 주기가 줄어드는 가속 계수 (0 < value < 1)")]
        [SerializeField] private float accelerationRate = 0.88f;

        private readonly Subject<Unit> _onTickSubject = new();
        private CancellationTokenSource _cts;
        private UIButtonSound _buttonSound;

        private void Awake()
        {
            _buttonSound = GetComponent<UIButtonSound>();
        }

        public Observable<Unit> OnTickAsObservable() => _onTickSubject;

        public void OnPointerDown(PointerEventData eventData)
        {
            StopRepeat();
            _cts = new CancellationTokenSource();
            _buttonSound?.PlaySound();
            _onTickSubject.OnNext(Unit.Default);
            RepeatLoopAsync(_cts.Token).Forget();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StopRepeat();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopRepeat();
        }

        private void OnDisable()
        {
            StopRepeat();
        }

        private void OnDestroy()
        {
            StopRepeat();
            _onTickSubject.Dispose();
        }

        private void StopRepeat()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async UniTaskVoid RepeatLoopAsync(CancellationToken ct)
        {
            bool canceled = await UniTask.Delay(TimeSpan.FromSeconds(initialDelay), cancellationToken: ct).SuppressCancellationThrow();
            if (canceled) return;
            float currentInterval = initialInterval;
            while (!ct.IsCancellationRequested)
            {
                _buttonSound?.PlaySound();
                _onTickSubject.OnNext(Unit.Default);
                canceled = await UniTask.Delay(TimeSpan.FromSeconds(currentInterval), cancellationToken: ct).SuppressCancellationThrow();
                if (canceled) return;
                currentInterval = Mathf.Max(minInterval, currentInterval * accelerationRate);
            }
        }
    }
}
