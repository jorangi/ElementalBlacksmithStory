using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class SessionMenusView : MonoBehaviour
    {
        [SerializeField] private Color highlightedColor;
        [SerializeField] private Button[] buttons;
        [SerializeField] private RectTransform indicator;
        private readonly Subject<int> _onChangedButtonIndexSubject = new();
        public Observable<int> OnChangeButtonIndexAsObservable() => _onChangedButtonIndexSubject.AsObservable();
        private int _highlights = 0;
        public void Start()
        {
            ChangeHighLight(0);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        _onChangedButtonIndexSubject.OnNext(i);
                    }).AddTo(this);
            }
        }
        public void ChangeHighLight(int menu)
        {
            if (buttons[_highlights].targetGraphic is Image previousImage)
            {
                previousImage.color = Color.clear;
            }
            _highlights = menu;
            if (buttons[menu].targetGraphic is Image targetImage)
            {
                targetImage.color = highlightedColor;
            }
            MoveIndicator(menu).Forget();
        }
        private const float MOVE_DELTA = 206.8f;
        private const float ANIM_DURATION = 0.1f;
        private MotionHandle _motionHandle;
        private CancellationTokenSource _cts = new();
        public async UniTask MoveIndicator(int idx, CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (_motionHandle.IsPlaying())
            {
                _motionHandle.Cancel();
            }
            Vector2 to = new((idx - 2) * MOVE_DELTA, indicator.anchoredPosition.y);
            _motionHandle = LMotion.Create(indicator.anchoredPosition, to, ANIM_DURATION)
                                    .WithEase(Ease.OutQuint)
                                    .BindToAnchoredPosition(indicator)
                                    .AddTo(gameObject);

            await _motionHandle.ToUniTask(_cts.Token)
                                .SuppressCancellationThrow();
        }
    }
}
