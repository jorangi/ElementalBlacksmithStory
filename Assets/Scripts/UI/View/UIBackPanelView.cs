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
    public class UIBackPanelView : MonoBehaviour
    {
        [SerializeField] private Image _rootPanel;
        private const float ANIM_DURATION = 0.1f;
        private CancellationTokenSource _cts = new();
        [SerializeField] private Color rootPanelColor;
        private MotionHandle _motionHandle;

        public async UniTask Show(CancellationToken ct = default)
        {
            if (_rootPanel == null) return;

            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            _rootPanel.gameObject.SetActive(true);

            if (_motionHandle.IsActive()) _motionHandle.Cancel();

            _motionHandle = LMotion.Create(Color.clear, rootPanelColor, ANIM_DURATION)
                    .WithEase(Ease.Linear)
                    .BindToColor(_rootPanel)
                    .AddTo(_rootPanel);

            await _motionHandle.ToUniTask(_cts.Token)
                            .SuppressCancellationThrow();
        }

        public async UniTask Hide(CancellationToken ct = default)
        {
            if (_rootPanel == null) return;

            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (_motionHandle.IsActive()) _motionHandle.Cancel();


            _motionHandle = LMotion.Create(rootPanelColor, Color.clear, ANIM_DURATION)
                            .WithEase(Ease.Linear)
                            .BindToColor(_rootPanel)
                            .AddTo(_rootPanel);

            await _motionHandle.ToUniTask(_cts.Token)
                            .SuppressCancellationThrow();

            _rootPanel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_motionHandle.IsActive()) _motionHandle.Cancel();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}