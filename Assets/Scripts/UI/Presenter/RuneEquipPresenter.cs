using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        private readonly Dictionary<uint, SO_DialogueData> _dialogueCache = new();
        private SO_NPCData _npcData;
        private readonly CancellationTokenSource _cts = new();

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

        private enum DialogueType : uint
        {
            Open = 600816,
            CostChanged = 600817,
            NotEnoughMoney = 600818,
            EquipSuccess = 600819
        }

        private void PublishDialogue(DialogueType type)
        {
            Dictionary<string, object> parameters = null;

            switch (type)
            {
                case DialogueType.CostChanged:
                    if (_runeService != null && _currentWeapon != null)
                    {
                        ulong cost = _runeService.CalculateEquipCost(_currentWeapon, runes);
                        if (cost == 0)
                        {
                            SetDialogueByIdAsync((uint)DialogueType.Open, ct: _cts.Token).Forget();
                            return;
                        }
                        parameters = new Dictionary<string, object>
                        {
                            { "cost", cost },
                            { "costText", cost.ToString("N0") }
                        };
                    }
                    else
                    {
                        SetDialogueByIdAsync((uint)DialogueType.Open, ct: _cts.Token).Forget();
                        return;
                    }
                    break;
            }

            SetDialogueByIdAsync((uint)type, parameters, ct: _cts.Token).Forget();
        }

        private async UniTask<SO_NPCData> GetNPCDataAsync(uint speakerId, CancellationToken ct = default)
        {
            if (speakerId == 0) return null;
            if (_npcData != null) return _npcData;

            try
            {
                var handle = Addressables.LoadAssetAsync<SO_NPCData>(speakerId.ToString());
                _npcData = await handle.ToUniTask(cancellationToken: ct);
                return _npcData;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RuneEquipPresenter] NPC 데이터({speakerId}) 로드 실패: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 대사 ID로 SO_DialogueData를 로드하고 조건문, 플레이스홀더, 한국어 조사를 파싱하여 출력합니다.
        /// </summary>
        public async UniTask SetDialogueByIdAsync(uint dialogueId, IReadOnlyDictionary<string, object> parameters = null, int lineIndex = 0, CancellationToken ct = default)
        {
            try
            {
                if (!_dialogueCache.TryGetValue(dialogueId, out var dialogueData) || dialogueData == null)
                {
                    var handle = Addressables.LoadAssetAsync<SO_DialogueData>(dialogueId.ToString());
                    dialogueData = await handle.ToUniTask(cancellationToken: ct);
                    if (dialogueData != null)
                    {
                        _dialogueCache[dialogueId] = dialogueData;
                    }
                }

                if (dialogueData != null && dialogueData.contents != null && dialogueData.contents.Count > 0)
                {
                    if (lineIndex < 0 || lineIndex >= dialogueData.contents.Count)
                    {
                        lineIndex = 0;
                    }

                    var line = dialogueData.contents[lineIndex];
                    if (line.speakerId != 0)
                    {
                        var npcData = await GetNPCDataAsync(line.speakerId, ct);
                        if (npcData != null)
                        {
                            _runeEquipView.SetNPCName(npcData.NpcName);
                        }
                    }

                    string formattedText = line.GetFormattedText(parameters);
                    _runeEquipView.SetDialogue(formattedText);
                }
                else
                {
                    Debug.LogWarning($"[RuneEquipPresenter] 대사 데이터({dialogueId})를 찾을 수 없습니다.");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RuneEquipPresenter] 대사 데이터({dialogueId}) 로드 실패: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
            _weaponDisposable.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}