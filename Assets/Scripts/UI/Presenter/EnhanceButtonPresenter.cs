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
        private readonly EnhancementService _enhancementService;
        private readonly CompositeDisposable _disposables = new();
        private readonly ForgeManager _forgeManager;
        [Inject]
        public EnhanceButtonPresenter(
            EnhanceButtonView view, 
            IPublisher<EnhanceRequestEvent> enhanceRequestPublisher,
            ForgeManager forgeManager,
            ISubscriber<EnhanceButtonPositionEvent> buttonPositionSubscriber,
            EnhancementService enhancementService
            )
        {
            _view = view;
            _enhancementService = enhancementService;
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
                    Debug.Log($"[EnhanceButtonPresenter] {_forgeManager.CurrentWeapon.Id}({_forgeManager.CurrentWeapon.WeaponId})을 강화시도");
                    // Debug.Log($"[EnhanceButtonPresenter] EnhanceRequestEvent {_forgeManager.CurrentWeapon.Id} 전송");
                    EnhanceResult result = _enhancementService.TryEnhance(_forgeManager.CurrentWeapon, null);
                    if(result == EnhanceResult.SUCESS)
                    {
                        Debug.Log($"[EnhanceButtonPresenter] {_forgeManager.CurrentWeapon.Id}({_forgeManager.CurrentWeapon.WeaponId})로 강화성공");
                    }
                    else
                    {
                        Debug.LogWarning($"[EnhanceButtonPresenter] {_forgeManager.CurrentWeapon.Id}({_forgeManager.CurrentWeapon.WeaponId})을 강화하지 못함: {result}");
                    }
                    // _enhanceRequestPublisher.Publish(new EnhanceRequestEvent(_forgeManager.CurrentWeapon));
                })
                .AddTo(_disposables);
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}