using System;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Inventory;
using ElementalBlacksmithStory.UI;
using MessagePipe;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Core
{
    public class GameInitializer : IStartable, IDisposable
    {
        private MainAction mainAction;
        private readonly IPublisher<AcquireItemEvent> _acquireItemPublisher;
        private readonly IPublisher<PlaySoundEvent> _sfxPublisher;
        private readonly GameExitPresenter _gameExitPresenter;
        private readonly IDisposable _exitSubscription;

        [Inject]
        public GameInitializer(
            SO_WeaponDatabase weaponDatabase,
            SO_MaterialDatabase materialDatabase,
            IPublisher<AcquireItemEvent> acquireItemPublisher,
            GameExitPresenter gameExitPresenter,
            IPublisher<PlaySoundEvent> sfxPublisher,
            ISubscriber<GameExitEvent> exitSubscriber,
            SO_RuneDatabase runeDatabase = null
        )
        {
            weaponDatabase.Init();
            materialDatabase.Init();
            runeDatabase.Init();
            _acquireItemPublisher = acquireItemPublisher;
            _gameExitPresenter = gameExitPresenter;
            _sfxPublisher = sfxPublisher;
            _exitSubscription = exitSubscriber.Subscribe(_ => { Debug.Log("게임을 정상적으로 종료했습니다."); Application.Quit(); });
        }
        public void Start()
        {
            QualitySettings.vSyncCount = 0;
            int refreshRate = (int)System.Math.Round(Screen.currentResolution.refreshRateRatio.value);
            Application.targetFrameRate = refreshRate >= 60 ? refreshRate : 60;
            UnityEngine.Debug.Log($"[GameInitializer] Target Frame Rate 설정 완료: {Application.targetFrameRate} FPS (Display: {refreshRate} Hz)");
            mainAction = new();
            for (uint id = 30001; id <= 30030; id++)
            {
                _acquireItemPublisher.Publish(new AcquireItemEvent(id, 500, sender: "System", reason: "Initial_Default"));
            }
            _sfxPublisher.Publish(new PlaySoundEvent(40201, true));

            mainAction.Enable();
            mainAction.MainActions.Back.performed += OnBack;
        }
        public void Dispose()
        {
            if (mainAction != null)
            {
                mainAction.MainActions.Back.performed -= OnBack;
                mainAction.Disable();
                mainAction.Dispose();
                mainAction = null;
            }

            _exitSubscription?.Dispose();
        }
        private void OnBack(InputAction.CallbackContext context)
        {
            _gameExitPresenter.ShowPanel();
        }
    }
}