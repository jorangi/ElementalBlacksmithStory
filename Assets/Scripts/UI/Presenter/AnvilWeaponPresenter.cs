using ElementalBlacksmithStory.Data;
using Cysharp.Threading.Tasks;
using MessagePipe;
using R3;
using VContainer;
using VContainer.Unity;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using ElementalBlacksmithStory.Events;
using System.Threading;
using ElementalBlacksmithStory.Core;

namespace ElementalBlacksmithStory.UI
{
    public class AnvilWeaponPresenter: IStartable, IDisposable
    {
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly AnvilWeaponView _anvilWeaponView;
        private readonly WeaponSpriteLoader _weaponSpriteLoader;
        private readonly CompositeDisposable _disposables = new();
        private readonly ForgeManager _forgeManager;

        public AnvilWeaponPresenter(
                                    SO_WeaponDatabase weaponDatabase,
                                    AnvilWeaponView view,
                                    WeaponSpriteLoader spriteLoader, 
                                    ISubscriber<ChangeWeaponEvent> weaponChangeSubscriber,
                                    ForgeManager forgeManager)
        {
            _weaponDatabase = weaponDatabase;
            _anvilWeaponView = view;
            _weaponSpriteLoader = spriteLoader;
            weaponChangeSubscriber.Subscribe(e => {ChangeSprite(e).Forget();}).AddTo(_disposables);
            _forgeManager = forgeManager;
        }
        public void Start()
        {
            ChangeSprite(new ChangeWeaponEvent(10001)).Forget();
        }
        private async UniTask ChangeSprite(ChangeWeaponEvent e, CancellationToken cancellationToken = default)
        {
            try
            {
                Sprite sprite = await _weaponSpriteLoader.GetWeaponSprite(e.WeaponId.ToString(), cancellationToken);
                if(sprite == null)
                {
                    Debug.Log($"[AnvilWeaponPresenter] {e.WeaponId}스프라이트가 없습니다.");
                    return;
                }
                Debug.Log($"[AnvilWeaponPresenter] {e.WeaponId}스프라이트를 정상적으로 불러왔습니다.");
                _anvilWeaponView.ChangeSprite(sprite);
                SO_WeaponData weaponData = _weaponDatabase.GetWeapon(e.WeaponId);
                _anvilWeaponView.ChangeWeaponName(weaponData.weaponName);
            }
            catch(Exception exception)
            {
                Debug.LogError($"[AnvilWeaponPresenter] {e.WeaponId}스프라이트를 불러오던중 오류가 발생했습니다. {exception.Message}");
            }
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}