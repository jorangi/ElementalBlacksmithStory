using System;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

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
                .Subscribe(this, static (_, self) =>
                {
                    Debug.Log($"[EnhanceButtonPresenter] {self._forgeManager.CurrentWeapon.Id}({self._forgeManager.CurrentWeapon.WeaponId})을 강화시도");
                    EnhanceResult result = self._enhancementService.TryEnhance(self._forgeManager.CurrentWeapon, null);
                    if (result == EnhanceResult.SUCESS)
                    {
                        Debug.Log($"[EnhanceButtonPresenter] {self._forgeManager.CurrentWeapon.Id}({self._forgeManager.CurrentWeapon.WeaponId})로 강화성공");
                    }
                    else
                    {
                        Debug.LogWarning($"[EnhanceButtonPresenter] {self._forgeManager.CurrentWeapon.Id}({self._forgeManager.CurrentWeapon.WeaponId})을 강화하지 못함: {result}");
                    }
                })
                .AddTo(_disposables);
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}