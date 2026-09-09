using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class NPCDialoguePresenter : IStartable, IDisposable
    {
        private readonly NPCDialogueView _view;
        private readonly NPCStandingSpriteLoader _spriteLoader;
        private readonly CompositeDisposable _disposables = new();
        private readonly Dictionary<uint, SO_NPCData> _npcCache = new();

        private SO_DialogueData _currentDialogue;
        private int _currentLineIndex = -1;
        private bool _isTyping = false;
        private bool _isAdvancingLine = false;
        private CancellationTokenSource _typingCts;
        private Action _onComplete;

        public bool IsPlaying => _currentDialogue != null;
        public bool IsActivated => _view != null && _view.IsActivated;

        [Inject]
        public NPCDialoguePresenter(
            NPCDialogueView view,
            NPCStandingSpriteLoader spriteLoader = null)
        {
            _view = view;
            _spriteLoader = spriteLoader;
        }

        public void Start()
        {
            if (_view == null) return;

            _view.OnClickDialogueBoxAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(150))
                .Subscribe(_ => OnClickDialogueBox())
                .AddTo(_disposables);

            if (!IsPlaying && _view.IsActivated)
            {
                _view.Hide();
            }
        }

        /// <summary>
        /// 대화 ID로 대화 시작
        /// </summary>
        public void StartDialogue(uint dialogueId, Action onComplete = null)
        {
            StartDialogueAsync(dialogueId, onComplete).Forget();
        }

        /// <summary>
        /// 대화 ID로 대화 비동기 시작
        /// </summary>
        public async UniTask StartDialogueAsync(uint dialogueId, Action onComplete = null)
        {
            var handle = Addressables.LoadAssetAsync<SO_DialogueData>(dialogueId.ToString());
            var dialogueData = await handle.ToUniTask();

            if (dialogueData == null || dialogueData.contents == null || dialogueData.contents.Count == 0)
            {
                Debug.LogWarning($"[NPCDialoguePresenter] 대화 데이터({dialogueId})를 찾을 수 없거나 비어있습니다.");
                onComplete?.Invoke();
                return;
            }

            _currentDialogue = dialogueData;
            _onComplete = onComplete;
            _currentLineIndex = -1;

            _view.Show();
            ShowNextLine().Forget();
        }

        /// <summary>
        /// 대화창 클릭 처리 (타이핑 중엔 스킵, 완료 시 다음 라인 진행)
        /// </summary>
        private void OnClickDialogueBox()
        {
            if (!IsPlaying) return;

            if (_isTyping)
            {
                _typingCts?.Cancel();
            }
            else
            {
                if (_isAdvancingLine) return;
                ShowNextLine().Forget();
            }
        }

        /// <summary>
        /// 다음 대사 라인 출력
        /// </summary>
        private async UniTaskVoid ShowNextLine()
        {
            if (_isAdvancingLine) return;
            _isAdvancingLine = true;

            try
            {
                _currentLineIndex++;

                if (_currentDialogue == null || _currentLineIndex >= _currentDialogue.contents.Count)
                {
                    EndDialogue();
                    return;
                }

                var line = _currentDialogue.contents[_currentLineIndex];

                // SpeakerId로 SO_NPCData 및 스탠딩 스프라이트 가져오기
                var npcData = await GetNPCDataAsync(line.speakerId);
                string speakerName = npcData != null ? npcData.NpcName : string.Empty;
                Sprite standingSprite = null;

                if (_spriteLoader != null && line.speakerId != 0)
                {
                    standingSprite = await _spriteLoader.GetSprite(line.speakerId);
                }

                _view.SetNPC(speakerName, standingSprite);

                // 라인 세팅 완료 후 클릭 스킵 허용
                _isAdvancingLine = false;

                _typingCts?.Dispose();
                _typingCts = new CancellationTokenSource();
                _isTyping = true;

                try
                {
                    await _view.TypingText(line.text, _typingCts.Token);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    _isTyping = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NPCDialoguePresenter] 대사 출력 중 오류 발생: {ex}");
                EndDialogue();
            }
            finally
            {
                _isAdvancingLine = false;
            }
        }

        /// <summary>
        /// SpeakerId로 SO_NPCData 조회 (캐시 및 Addressables 로드)
        /// </summary>
        private async UniTask<SO_NPCData> GetNPCDataAsync(uint speakerId)
        {
            if (speakerId == 0) return null;

            if (_npcCache.TryGetValue(speakerId, out var cachedData))
            {
                return cachedData;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<SO_NPCData>(speakerId.ToString());
                var npcData = await handle.ToUniTask();
                if (npcData != null)
                {
                    _npcCache[speakerId] = npcData;
                    return npcData;
                }
            }
            catch
            {
                // 로드 실패 시 null 반환
            }

            return null;
        }

        /// <summary>
        /// 대화 종료 및 UI 닫기
        /// </summary>
        public void EndDialogue()
        {
            _typingCts?.Cancel();
            _typingCts?.Dispose();
            _typingCts = null;

            _isTyping = false;
            _isAdvancingLine = false;
            _currentDialogue = null;
            _currentLineIndex = -1;

            if (_view != null)
            {
                _view.Hide();
            }

            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        public void Hide() => EndDialogue();

        public void Dispose()
        {
            EndDialogue();
            _disposables.Dispose();
        }
    }
}
