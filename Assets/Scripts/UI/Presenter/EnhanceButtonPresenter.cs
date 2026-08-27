using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using MessagePipe;
using R3;
using ElementalBlacksmithStory.Events;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;

namespace ElementalBlacksmithStory.UI
{
    public class EnhanceButtonPresenter : IStartable, IDisposable
    {
        private readonly EnhanceButtonView _view;
        private readonly IAsyncPublisher<EnhanceRequestEvent> _enhanceRequestPublisher;
        private readonly CompositeDisposable _disposables = new();
        private readonly ForgeManager _forgeManager;
        [Inject]
        public EnhanceButtonPresenter(
            EnhanceButtonView view, 
            IAsyncPublisher<EnhanceRequestEvent> enhanceRequestPublisher,
            ForgeManager forgeManager,
            ISubscriber<EnhanceButtonPositionEvent> buttonPositionSubscriber
            )
        {
            _view = view;
            _enhanceRequestPublisher = enhanceRequestPublisher;
            _forgeManager = forgeManager;

            buttonPositionSubscriber.Subscribe(e =>
            {
                if (e.positionY <= -1000f)
                {
                    _view.HidingButtonAnimation().Forget();
                }
                else if (Mathf.Approximately(e.positionY, 0f))
                {
                    _view.ShowingButtonAnimation().Forget();
                }
                else
                {
                    _view.SyncYPosition(295f + e.positionY);
                }
            }).AddTo(_disposables);
        }
        public void Start()
        {
            _view.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(300))
                .Subscribe(_ =>
                {
                    Debug.Log($"[EnhanceButtonPresenter] EnhanceRequestEvent {_forgeManager.CurrentWeapon.Id} 전송");
                    _enhanceRequestPublisher.PublishAsync(new EnhanceRequestEvent(_forgeManager.CurrentWeapon)).Forget();
                })
                .AddTo(_disposables);
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}