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
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
namespace ElementalBlacksmithStory.UI
{
    public class RuneSynthesisPresenter : IDisposable, IRuneUIState
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly UIBackPanelPresenter _uiBackPanelPresenter;
        private readonly RuneSynthesisView _runeSynthesisView;

        private readonly RuneSpriteLoader _runeSpriteLoader;
        private readonly RuneInventoryPresenter _runeInventoryPresenter;
        private readonly RuneService _runeService;
        private readonly IPublisher<PlaySoundEvent> _soundPublisher;
        [SerializeField] private readonly uint[] runes = new uint[3];
        private readonly uint[] _originalRunes = new uint[3];
        private readonly Dictionary<uint, SO_DialogueData> _dialogueCache = new();
        private SO_NPCData _npcData;
        private readonly CancellationTokenSource _cts = new();

        [Inject]
        public RuneSynthesisPresenter(
            UIBackPanelPresenter uiBackPanelPresenter,
            RuneSynthesisView runeSynthesis,
            RuneInventoryPresenter runeInventoryPresenter,
            RuneSpriteLoader runeSpriteLoader,
            IPublisher<PlaySoundEvent> soundPublisher,
            RuneService runeService = null,
            ForgeManager forgeManager = null
        )
        {
            _uiBackPanelPresenter = uiBackPanelPresenter;
            _runeSynthesisView = runeSynthesis;
            _runeInventoryPresenter = runeInventoryPresenter;
            _runeSpriteLoader = runeSpriteLoader;
            _runeService = runeService;
            _soundPublisher = soundPublisher;

            for (int i = 0; i < runes.Length; i++)
            {
                int index = i;
                _runeSynthesisView.OnRuneSlotButtonAsObservable(index)
                    .Subscribe(_ =>
                    {
                        selectedSocketIndex = index;
                        _runeSynthesisView.SelectSocket(index);
                    })
                    .AddTo(_disposables);

                _runeSynthesisView.OnRuneSlotRightClickAsObservable(index)
                    .Subscribe(_ => UnequipRune(index))
                    .AddTo(_disposables);
            }
            _runeSynthesisView.SelectSocket(0);

            _runeSynthesisView.OnCancelButtonAsObservable()
                .Subscribe(_ =>
                {
                    CancelDraftRunes();
                    Hide();
                })
                .AddTo(_disposables);

            _runeSynthesisView.OnSynthesizeButtonAsObservable()
                .Subscribe(_ =>
                {
                    SynthesizeRunes().Forget();
                    soundPublisher.Publish(new PlaySoundEvent(40108));
                })
                .AddTo(_disposables);
        }
        public void Show()
        {
            _uiBackPanelPresenter.Show();
            _runeSynthesisView.Show().Forget();
            _runeInventoryPresenter.SetRuneUIState(this);
            PublishDialogue(DialogueType.Open);
        }
        public void Hide()
        {
            _uiBackPanelPresenter.Hide();
            _runeSynthesisView.Hide().Forget();
        }
        private int selectedSocketIndex;
        /// <summary>
        /// 룬을 장착 UI에서 변경하는 함수(Synthesize 이전)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public async UniTask ChangeRune(uint runeId)
        {
            if (selectedSocketIndex < 0 || selectedSocketIndex >= runes.Length) return;

            runes[selectedSocketIndex] = runeId;
            _runeInventoryPresenter.SyncDraftRunes(runes, _originalRunes);

            Sprite sprite = await _runeSpriteLoader.GetSprite(runeId);
            _runeSynthesisView.SetRuneSprite(selectedSocketIndex, sprite);

            if (GetEquippedRuneCount() == 3)
            {
                PublishDialogue(DialogueType.CostChanged);
            }
            else
            {
                PublishDialogue(DialogueType.Open);
            }

            if (selectedSocketIndex < runes.Length - 1)
            {
                selectedSocketIndex++;
                _runeSynthesisView.SelectSocket(selectedSocketIndex);
            }
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
            _runeSynthesisView.SelectSocket(socketIndex);

            runes[socketIndex] = 0;
            _runeInventoryPresenter.SyncDraftRunes(runes, _originalRunes);

            _runeSynthesisView.SetRuneSprite(socketIndex, null);
            PublishDialogue(DialogueType.Open);
            _soundPublisher.Publish(new PlaySoundEvent(40106));
        }

        private int GetEquippedRuneCount()
        {
            int count = 0;
            for (int i = 0; i < runes.Length; i++)
            {
                if (runes[i] != 0)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 모든 임시 슬롯에 장착한 룬을 취소하고 원복
        /// </summary>
        public void CancelDraftRunes()
        {
            UnequipRune(0);
            UnequipRune(1);
            UnequipRune(2);
            _runeInventoryPresenter.ClearDraft();
            selectedSocketIndex = 0;
            _runeSynthesisView.SelectSocket(0);
        }

        /// <summary>
        /// 룬 합성
        /// </summary>
        public async UniTask SynthesizeRunes()
        {
            if (_runeService != null)
            {
                var result = _runeService.TrySynthesizeRunes(runes, out var synthesizedRune, out ulong spentCost);
                if (result != SynthesizeRuneResult.Success)
                {
                    switch (result)
                    {
                        case SynthesizeRuneResult.NotEnoughMaterials:
                            PublishDialogue(DialogueType.NotEnoughMaterials);
                            break;
                        case SynthesizeRuneResult.MythicalNotAllowed:
                            PublishDialogue(DialogueType.MythicalNotAllowed);
                            break;
                        case SynthesizeRuneResult.NotEnoughMoney:
                            PublishDialogue(DialogueType.NotEnoughMoney);
                            break;
                    }
                    return;
                }
                PublishDialogue(DialogueType.SynthesizeSuccess);
                Sprite sprite = await _runeSpriteLoader.GetSprite(synthesizedRune.Id);
                _runeSynthesisView.SetResultRune(sprite, synthesizedRune.Name);
            }
            ClearSynthesisDraftSlots();
        }

        /// <summary>
        /// 합성 성공 후 임시 슬롯 룬을 대사 변경 없이 초기화
        /// </summary>
        private void ClearSynthesisDraftSlots()
        {
            for (int i = 0; i < runes.Length; i++)
            {
                runes[i] = 0;
                _runeSynthesisView.SetRuneSprite(i, null);
            }
            _runeInventoryPresenter.ClearDraft();
            selectedSocketIndex = 0;
            _runeSynthesisView.SelectSocket(0);
        }

        private enum DialogueType : uint
        {
            Open = 600810,
            CostChanged = 600811,
            NotEnoughMoney = 600812,
            NotEnoughMaterials = 600813,
            MythicalNotAllowed = 600814,
            SynthesizeSuccess = 600815
        }

        private void PublishDialogue(DialogueType type)
        {
            Dictionary<string, object> parameters = null;

            switch (type)
            {
                case DialogueType.CostChanged:
                    ulong cost = _runeService != null ? _runeService.CalculateSynthesizeCost(runes) : 0UL;
                    if (cost == 0)
                    {
                        PublishDialogue(DialogueType.Open);
                        return;
                    }
                    parameters = new Dictionary<string, object>
                    {
                        { "cost", cost },
                        { "costText", cost.ToString("N0") }
                    };
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
                Debug.LogWarning($"[RuneSynthesisPresenter] NPC 데이터({speakerId}) 로드 실패: {ex.Message}");
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
                            _runeSynthesisView.SetNPCName(npcData.NpcName);
                        }
                    }

                    string formattedText = line.GetFormattedText(parameters);
                    _runeSynthesisView.SetDialogue(formattedText);
                }
                else
                {
                    Debug.LogWarning($"[RuneSynthesisPresenter] 대사 데이터({dialogueId})를 찾을 수 없습니다.");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RuneSynthesisPresenter] 대사 데이터({dialogueId}) 로드 실패: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}