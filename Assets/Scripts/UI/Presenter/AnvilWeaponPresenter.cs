using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class AnvilWeaponPresenter : IStartable, IDisposable
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
            weaponChangeSubscriber.Subscribe(e => { ChangeSprite(e).Forget(); }).AddTo(_disposables);
            _forgeManager = forgeManager;
        }
        public void Start() { }
        private async UniTask ChangeSprite(ChangeWeaponEvent e, CancellationToken cancellationToken = default)
        {
            try
            {
                if (e.Weapon == null) return;
                SO_WeaponData weaponData = _weaponDatabase.Get(e.Weapon.WeaponId);
                if (weaponData == null)
                {
                    Debug.LogWarning($"[AnvilWeaponPresenter] {e.Weapon.WeaponId} 데이터를 찾을 수 없습니다.");
                    return;
                }

                _anvilWeaponView.ChangeWeaponName(weaponData.weaponName);
                Sprite sprite = await _weaponSpriteLoader.GetSprite(weaponData.Id, cancellationToken);
                if (sprite == null)
                {
                    Debug.LogWarning($"[AnvilWeaponPresenter] {e.Weapon.WeaponId} 스프라이트가 없습니다.");
                    return;
                }

                _anvilWeaponView.ChangeSprite(sprite, weaponData.hideOutline);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnvilWeaponPresenter] 스프라이트 변경 실패: {ex.Message}");
            }
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}