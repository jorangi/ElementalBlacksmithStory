using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
namespace ElementalBlacksmithStory.UI
{
    public class RuneEquipPresenter : IDisposable, IRuneUIState
    {
        private readonly CompositeDisposable _weaponDisposable = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly UIBackPanelPresenter _uiBackPanelPresenter;
        private readonly RuneEquipView _runeEquipView;
        private Weapon _currentWeapon;

        private readonly WeaponSpriteLoader _weaponspriteLoader;
        private readonly RuneSpriteLoader _runeSpriteLoader;
        private readonly RuneInventoryPresenter _runeInventoryPresenter;
        private readonly RuneService _runeService;
        private readonly ForgeManager _forgeManager;
        private readonly IPublisher<PlaySoundEvent> _soundPublisher;
        [SerializeField] private readonly uint[] runes = new uint[3];
        private readonly uint[] _originalRunes = new uint[3];

        [Inject]
        public RuneEquipPresenter(
            UIBackPanelPresenter uiBackPanelPresenter,
            RuneEquipView runeEquipView,
            RuneInventoryPresenter runeInventoryPresenter,
            WeaponSpriteLoader weaponspriteLoader,
            RuneSpriteLoader runeSpriteLoader,
            IPublisher<PlaySoundEvent> soundPublisher,
            RuneService runeService = null,
            ForgeManager forgeManager = null
        )
        {
            _uiBackPanelPresenter = uiBackPanelPresenter;
            _runeEquipView = runeEquipView;
            _runeInventoryPresenter = runeInventoryPresenter;
            _weaponspriteLoader = weaponspriteLoader;
            _runeSpriteLoader = runeSpriteLoader;
            _runeService = runeService;
            _forgeManager = forgeManager;
            _soundPublisher = soundPublisher;

            for (int i = 0; i < runes.Length; i++)
            {
                int index = i;
                _runeEquipView.OnRuneSlotButtonAsObservable(index)
                    .Subscribe(_ =>
                    {
                        selectedSocketIndex = index;
                        _runeEquipView.SelectSocket(index);
                    })
                    .AddTo(_disposables);

                _runeEquipView.OnRuneSlotRightClickAsObservable(index)
                    .Subscribe(_ => UnequipRune(index))
                    .AddTo(_disposables);
            }
            _runeEquipView.SelectSocket(0);

            _runeEquipView.OnCancelButtonAsObservable()
                .Subscribe(_ =>
                {
                    CancelDraftRunes();
                    Hide();
                })
                .AddTo(_disposables);

            _runeEquipView.OnApplyButtonAsObservable()
                .Subscribe(_ =>
                {
                    ApplyRunes();
                    soundPublisher.Publish(new PlaySoundEvent(40108));
                })
                .AddTo(_disposables);
        }
        public void SetWeapon(Weapon weapon)
        {
            this._currentWeapon = weapon;

            _weaponDisposable.Clear();
            if (_currentWeapon != null)
            {
                _runeEquipView.SetSocketCount(_currentWeapon.RuneSlot);
                UpdateWeaponViewAsync(_currentWeapon).Forget();

                _currentWeapon.OnRuneSlotChanged.Subscribe(e => _runeEquipView.SetSocketCount(e))
                                            .AddTo(_weaponDisposable);

                _currentWeapon.OnRuneChanged.Subscribe(e => RuneChanged(e.socketIndex, e.rune).Forget())
                                            .AddTo(_weaponDisposable);

                SyncRunesFromWeapon();
            }
        }
        private async UniTaskVoid UpdateWeaponViewAsync(Weapon weapon)
        {
            if (weapon == null || weapon.Data == null) return;
            Sprite sprite = await _weaponspriteLoader.GetSprite(weapon.WeaponId);
            _runeEquipView.SetWeapon(sprite, weapon.Data.weaponName);
        }
        public void Show()
        {
            if (_forgeManager != null && _forgeManager.CurrentWeapon != null)
            {
                SetWeapon(_forgeManager.CurrentWeapon);
            }
            else
            {
                SyncRunesFromWeapon();
            }

            _uiBackPanelPresenter.Show();
            _runeEquipView.Show().Forget();
            _runeInventoryPresenter.SetRuneUIState(this);
            PublishDialogue(DialogueType.Open);
        }
        public void Hide()
        {
            _uiBackPanelPresenter.Hide();
            _runeEquipView.Hide().Forget();
        }
        /// <summary>
        /// 무기로부터 룬 동기화하는 함수
        /// </summary>
        private void SyncRunesFromWeapon()
        {
            for (int i = 0; i < runes.Length; i++)
            {
                uint orig = GetOriginalRuneId(i);
                _originalRunes[i] = orig;
                runes[i] = orig;
            }
            RefreshRuneSpritesAsync().Forget();
            _runeInventoryPresenter.SyncDraftRunes(runes, _originalRunes);
        }

        private uint GetOriginalRuneId(int socketIndex)
        {
            if (_currentWeapon == null || socketIndex < 0 || socketIndex >= _currentWeapon.RuneSlot)
            {
                return 0;
            }

            Rune rune = _currentWeapon.GetRune(socketIndex);
            return (rune != null && rune.Data != null) ? rune.Id : 0;
        }
        /// <summary>
        /// 룬 스프라이트 갱신 함수
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid RefreshRuneSpritesAsync()
        {
            for (int i = 0; i < runes.Length; i++)
            {
                uint id = runes[i];
                if (id != 0)
                {
                    Sprite sprite = await _runeSpriteLoader.GetSprite(id);
                    _runeEquipView.SetRuneSprite(i, sprite);
                }
                else
                {
                    _runeEquipView.SetRuneSprite(i, null);
                }
            }
        }

        /// <summary>
        /// 현재 무기의 룬 상태와 동기화
        /// </summary>
        /// <param name="socketIndex"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async UniTask RuneChanged(int socketIndex, Rune e)
        {
            if (e != null && e.Data != null)
            {
                Sprite sprite = await _runeSpriteLoader.GetSprite(e.Data.Id);
                _runeEquipView.SetRuneSprite(socketIndex, sprite);
            }
            else
            {
                _runeEquipView.SetRuneSprite(socketIndex, null);
            }
        }
        private int selectedSocketIndex;
        /// <summary>
        /// 룬을 장착 UI에서 변경하는 함수(Apply 이전)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public async UniTask ChangeRune(uint runeId)
        {
            if (selectedSocketIndex < 0 || selectedSocketIndex >= runes.Length) return;

            runes[selectedSocketIndex] = runeId;
            _runeInventoryPresenter.SyncDraftRunes(runes, _originalRunes);

            Sprite sprite = await _runeSpriteLoader.GetSprite(runeId);
            _runeEquipView.SetRuneSprite(selectedSocketIndex, sprite);
            PublishDialogue(DialogueType.CostChanged);
        }
        /// <summary>
        /// 슬롯 우클릭 시 해당 슬롯의 룬을 임시 해제
        /// </summary>
        /// <param name="socketIndex"></param>
        public void UnequipRune(int socketIndex)
        {
            if (socketIndex < 0 || socketIndex >= runes.Length) return;

            uint currentRune = runes[socketIndex];
            if (currentRune == 0) return;

            selectedSocketIndex = socketIndex;
            _runeEquipView.SelectSocket(socketIndex);

            runes[socketIndex] = 0;
            _runeInventoryPresenter.SyncDraftRunes(runes, _originalRunes);

            _runeEquipView.SetRuneSprite(socketIndex, null);
            PublishDialogue(DialogueType.CostChanged);
            _soundPublisher.Publish(new PlaySoundEvent(40106));
        }

        /// <summary>
        /// 모든 임시 슬롯에 장착한 룬을 취소하고 원복
        /// </summary>
        public void CancelDraftRunes()
        {
            _runeInventoryPresenter.ClearDraft();
            SyncRunesFromWeapon();
            selectedSocketIndex = 0;
            _runeEquipView.SelectSocket(0);
        }

        /// <summary>
        /// 임시 슬롯에 장착된 룬을 무기에 실제 장착 및 인벤토리 반영
        /// </summary>
        public void ApplyRunes()
        {
            if (_currentWeapon == null) return;

            if (_runeService != null)
            {
                if (!_runeService.TryApplyRunes(_currentWeapon, runes, out var result, out ulong spentCost))
                {
                    if (result == ApplyRuneResult.NotEnoughMoney)
                    {
                        PublishDialogue(DialogueType.NotEnoughMoney);
                    }
                    else if (result == ApplyRuneResult.NoChanges)
                    {
                        Hide();
                    }
                    return;
                }
                PublishDialogue(DialogueType.EquipSuccess);
            }

            _runeInventoryPresenter.ClearDraft();
            SyncRunesFromWeapon();
            selectedSocketIndex = 0;
            _runeEquipView.SelectSocket(0);
            Hide();
        }

        private enum DialogueType
        {
            Open,
            CostChanged,
            NotEnoughMoney,
            EquipSuccess
        }

        private void PublishDialogue(DialogueType type)
        {
            switch (type)
            {
                case DialogueType.Open:
                    _runeEquipView.SetDialogue("룬 장착을 도와드릴게요. 장비의 슬롯에 장착할 룬을 선택해 주세요.");
                    break;
                case DialogueType.CostChanged:
                    if (_runeService != null && _currentWeapon != null)
                    {
                        ulong cost = _runeService.CalculateEquipCost(_currentWeapon, runes);
                        if (cost > 0)
                        {
                            _runeEquipView.SetDialogue(ZString.Format("공임비와 조율 비용을 계산하니 <b>{0:N0}</b> 골드네요. 확인하시고 결정하세요.", cost));
                        }
                        else
                        {
                            _runeEquipView.SetDialogue("장비의 슬롯에 장착할 룬을 선택해 주세요.");
                        }
                    }
                    break;
                case DialogueType.NotEnoughMoney:
                    _runeEquipView.SetDialogue("골드가 부족하시네요. 정확한 비용을 치르셔야 작업을 진행해 드릴 수 있어요.");
                    break;
                case DialogueType.EquipSuccess:
                    _runeEquipView.SetDialogue("룬 장착이 깔끔하게 끝났어요. 소켓에 룬의 힘이 잘 정착되었네요.");
                    break;
            }
        }
        public void Dispose()
        {
            _disposables.Dispose();
            _weaponDisposable.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}