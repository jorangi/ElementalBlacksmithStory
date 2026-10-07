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
        private readonly RuneSpriteLoader _runeSpriteLoader;
        private readonly CompositeDisposable _disposables = new();
        private readonly CompositeDisposable _weaponDisposables = new();
        private readonly ForgeManager _forgeManager;
        private Weapon _currentWeapon;

        [Inject]
        public AnvilWeaponPresenter(
            SO_WeaponDatabase weaponDatabase,
            AnvilWeaponView view,
            WeaponSpriteLoader spriteLoader,
            ISubscriber<ChangeWeaponEvent> weaponChangeSubscriber,
            ForgeManager forgeManager,
            RuneSpriteLoader runeSpriteLoader = null)
        {
            _weaponDatabase = weaponDatabase;
            _anvilWeaponView = view;
            _weaponSpriteLoader = spriteLoader;
            _runeSpriteLoader = runeSpriteLoader;
            _forgeManager = forgeManager;

            weaponChangeSubscriber.Subscribe(e => { HandleWeaponChange(e).Forget(); }).AddTo(_disposables);
        }

        public void Start() { }

        private async UniTask HandleWeaponChange(ChangeWeaponEvent e, CancellationToken cancellationToken = default)
        {
            _weaponDisposables.Clear();
            _currentWeapon = e.Weapon;

            if (_currentWeapon != null)
            {
                await RefreshAllRunesAsync(_currentWeapon, cancellationToken);

                _currentWeapon.OnRuneSlotChanged
                    .Subscribe(slotCount =>
                    {
                        _anvilWeaponView.SetRuneSlotCount(slotCount);
                    })
                    .AddTo(_weaponDisposables);

                _currentWeapon.OnRuneChanged
                    .Subscribe(change =>
                    {
                        UpdateRuneSlotAsync(change.socketIndex, change.rune).Forget();
                    })
                    .AddTo(_weaponDisposables);
            }
            else
            {
                _anvilWeaponView.ChangeRuneStatus(0, null, null, null);
            }

            await ChangeSprite(e, cancellationToken);
        }

        private async UniTask RefreshAllRunesAsync(Weapon weapon, CancellationToken ct = default)
        {
            Sprite s0 = null;
            Sprite s1 = null;
            Sprite s2 = null;

            if (_runeSpriteLoader != null)
            {
                var r0 = weapon.GetRune(0);
                if (r0 != null) s0 = await _runeSpriteLoader.GetSprite(r0.SpriteId, ct);

                var r1 = weapon.GetRune(1);
                if (r1 != null) s1 = await _runeSpriteLoader.GetSprite(r1.SpriteId, ct);

                var r2 = weapon.GetRune(2);
                if (r2 != null) s2 = await _runeSpriteLoader.GetSprite(r2.SpriteId, ct);
            }

            _anvilWeaponView.ChangeRuneStatus(weapon.RuneSlot, s0, s1, s2);
        }

        private async UniTask UpdateRuneSlotAsync(int socketIndex, Rune rune, CancellationToken ct = default)
        {
            Sprite runeSprite = null;
            if (rune != null && _runeSpriteLoader != null)
            {
                runeSprite = await _runeSpriteLoader.GetSprite(rune.SpriteId, ct);
            }
            _anvilWeaponView.SetRune(socketIndex, runeSprite);
        }

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
            _weaponDisposables.Dispose();
            _disposables.Dispose();
        }
    }
}