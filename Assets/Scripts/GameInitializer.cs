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
    public class GameInitializer : IStartable
    {
        private MainAction mainAction;
        private readonly MaterialInventory _materialInventory;
        private readonly SO_MaterialDatabase _materialDatabase;
        private readonly IPublisher<PlaySoundEvent> _sfxPublisher;
        private readonly GameExitPresenter _gameExitPresenter;

        [Inject]
        public GameInitializer(
            SO_WeaponDatabase weaponDatabase,
            SO_MaterialDatabase materialDatabase,
            MaterialInventory materialInventory,
            GameExitPresenter gameExitPresenter,
            IPublisher<PlaySoundEvent> sfxPublisher,
            ISubscriber<GameExitEvent> exitSubscriber
        )
        {
            weaponDatabase.Init();
            materialDatabase.Init();
            _materialInventory = materialInventory;
            _materialDatabase = materialDatabase;
            _gameExitPresenter = gameExitPresenter;
            _sfxPublisher = sfxPublisher;
            exitSubscriber.Subscribe(_ => { Debug.Log("게임을 정상적으로 종료했습니다."); Application.Quit(); });
        }
        public void Start()
        {
            QualitySettings.vSyncCount = 0;
            int refreshRate = (int)System.Math.Round(Screen.currentResolution.refreshRateRatio.value);
            Application.targetFrameRate = refreshRate >= 60 ? refreshRate : 60;
            UnityEngine.Debug.Log($"[GameInitializer] Target Frame Rate 설정 완료: {Application.targetFrameRate} FPS (Display: {refreshRate} Hz)");
            mainAction = new();
            _materialInventory.Add(_materialDatabase.Get(30001), 500);
            _materialInventory.Add(_materialDatabase.Get(30002), 500);
            _materialInventory.Add(_materialDatabase.Get(30003), 500);
            _materialInventory.Add(_materialDatabase.Get(30004), 500);
            _materialInventory.Add(_materialDatabase.Get(30005), 500);
            _materialInventory.Add(_materialDatabase.Get(30006), 500);
            _materialInventory.Add(_materialDatabase.Get(30007), 500);
            _materialInventory.Add(_materialDatabase.Get(30008), 500);
            _materialInventory.Add(_materialDatabase.Get(30009), 500);
            _materialInventory.Add(_materialDatabase.Get(30010), 500);
            _sfxPublisher.Publish(new PlaySoundEvent(40201, true));

            mainAction.Enable();
            mainAction.MainActions.Back.performed += OnBack;
        }
        public void OnApplicationQuit()
        {
            mainAction.MainActions.Back.performed -= OnBack;
            mainAction.Disable();
        }
        private void OnBack(InputAction.CallbackContext context)
        {
            _gameExitPresenter.ShowPanel();
        }
    }
}