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
            PublishDialogue(DialogueType.CostChanged);
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
            PublishDialogue(DialogueType.CostChanged);
            _soundPublisher.Publish(new PlaySoundEvent(40106));
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
            UnequipRune(0);
            UnequipRune(1);
            UnequipRune(2);
            _runeInventoryPresenter.ClearDraft();
            selectedSocketIndex = 0;
            _runeSynthesisView.SelectSocket(0);
        }

        private enum DialogueType
        {
            Open,
            CostChanged,
            NotEnoughMoney,
            NotEnoughMaterials,
            MythicalNotAllowed,
            SynthesizeSuccess
        }

        private void PublishDialogue(DialogueType type)
        {
            switch (type)
            {
                case DialogueType.Open:
                    _runeSynthesisView.SetDialogue("룬 합성을 도와드릴게요. 슬롯에 합성할 룬 3개를 올려주세요.");
                    break;
                case DialogueType.CostChanged:
                    if (_runeService != null)
                    {
                        ulong cost = _runeService.CalculateSynthesizeCost(runes);
                        if (cost > 0)
                        {
                            _runeSynthesisView.SetDialogue(ZString.Format("합성 비용을 계산하니 <b>{0:N0}</b> 골드네요. 확인하시고 결정하세요.", cost));
                        }
                        else
                        {
                            _runeSynthesisView.SetDialogue("슬롯에 합성할 룬 3개를 올려주세요.");
                        }
                    }
                    break;
                case DialogueType.NotEnoughMoney:
                    _runeSynthesisView.SetDialogue("골드가 부족하시네요. 정확한 비용을 치르셔야 작업을 진행해 드릴 수 있어요.");
                    break;
                case DialogueType.NotEnoughMaterials:
                    _runeSynthesisView.SetDialogue("합성을 진행하려면 룬 세 개를 모두 올려주셔야 해요.");
                    break;
                case DialogueType.MythicalNotAllowed:
                    _runeSynthesisView.SetDialogue("신화(Mythical) 등급의 룬은 합성 재료로 사용할 수 없어요.");
                    break;
                case DialogueType.SynthesizeSuccess:
                    _runeSynthesisView.SetDialogue("룬 합성이 깔끔하게 끝났어요. 새로운 룬의 힘이 잘 깃들었네요.");
                    break;
            }
        }
        public void Dispose()
        {
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}