using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 상점 주인-텍스트 버블 Presenter
    /// </summary>
    public class ShopKeeperPresenter : IStartable, IDisposable
    {
        private readonly NPCStandingSpriteLoader _spriteLoader;
        private readonly ShopKeeperView _view;
        private readonly Dictionary<uint, SO_NPCData> _npcCache = new();
        private readonly IDisposable _subscription;
        private uint _currentSpeakerId = 0;
        private CancellationTokenSource _typingCts;
        private bool _isTyping = false;
        public bool IsTyping => _isTyping;

        [Inject]
        public ShopKeeperPresenter(
            NPCStandingSpriteLoader spriteLoader,
            ShopKeeperView view,
            ISubscriber<StartShopDialogueEvent> shopDialogueSubscriber
        )
        {
            _spriteLoader = spriteLoader;
            _view = view;

            _subscription = shopDialogueSubscriber.Subscribe(e =>
            {
                SetDialogueByIdAsync(e.dialogueId, e.parameters).Forget();
            });
        }

        public void Start() { }

        /// <summary>
        /// 상점 닫힘/오픈 시 이전 NPC 잔상 및 대사 텍스트를 즉시 정리
        /// </summary>
        public void ClearView()
        {
            _typingCts?.Cancel();
            _typingCts?.Dispose();
            _typingCts = null;
            _isTyping = false;
            _currentSpeakerId = 0;
            _view?.SetSprite(null);
            _view?.SetName(string.Empty);
        }

        /// <summary>
        /// 대사 출력
        /// </summary>
        /// <param name="dialogueId">대화 id</param>
        /// <param name="parameters">string키와 object값</param>
        /// <param name="lineIndex">대사 인덱스</param>
        /// <param name="ct">캔슬 토큰</param>
        /// <returns></returns>
        public async UniTask SetDialogueByIdAsync(uint dialogueId, IReadOnlyDictionary<string, object> parameters = null, int lineIndex = 0, CancellationToken ct = default)
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<SO_DialogueData>(dialogueId.ToString());
                var dialogueData = await handle.ToUniTask(cancellationToken: ct);
                if (dialogueData != null)
                {
                    await SetDialogue(dialogueData, parameters, lineIndex);
                }
                else
                {
                    Debug.LogWarning($"[ShopKeeperPresenter] 대사 데이터({dialogueId})를 찾을 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShopKeeperPresenter] 대사 데이터({dialogueId}) 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 상점 오픈 시 대사 파일 로딩을 기다리지 않고 NPC 스탠딩 스프라이트와 이름을 즉시 세팅
        /// </summary>
        public async UniTask SetNPCAsync(uint npcId, CancellationToken ct = default)
        {
            if (npcId == 0) return;
            if (_currentSpeakerId == npcId) return;

            _currentSpeakerId = npcId;

            var npcDataTask = GetNPCDataAsync(npcId);
            var spriteTask = _spriteLoader != null ? _spriteLoader.GetSprite(npcId, ct) : UniTask.FromResult<Sprite>(null);

            var (npcData, standingSprite) = await UniTask.WhenAll(npcDataTask, spriteTask);

            if (_currentSpeakerId != npcId) return;

            _view.SetSprite(standingSprite);
            if (npcData != null)
            {
                _view.SetName(npcData.NpcName);
            }
        }

        public async UniTask SetDialogue(SO_DialogueData data, IReadOnlyDictionary<string, object> parameters = null, int lineIndex = 0)
        {
            if (_view == null)
            {
                Debug.LogError($"[ShopKeeperPresenter] ShopKeeperView가 없습니다.");
                return;
            }

            if (data == null || data.contents == null || data.contents.Count == 0)
            {
                Debug.LogWarning($"[ShopKeeperPresenter] SO_DialogueData가 없거나 비어있습니다.");
                return;
            }

            if (lineIndex < 0 || lineIndex >= data.contents.Count)
            {
                lineIndex = 0;
            }

            var line = data.contents[lineIndex];

            // 비동기 로딩 대기 중에 이전 NPC 보이지 않도록 즉시 정리
            if (_currentSpeakerId != line.speakerId)
            {
                _view.SetSprite(null);
                _view.SetName(string.Empty);
            }

            // 스탠딩 스프라이트 및 NPC 데이터 병렬 로드
            var npcDataTask = GetNPCDataAsync(line.speakerId);
            var spriteTask = (_spriteLoader != null && line.speakerId != 0)
                ? _spriteLoader.GetSprite(line.speakerId)
                : UniTask.FromResult<Sprite>(null);

            var (npcData, standingSprite) = await UniTask.WhenAll(npcDataTask, spriteTask);
            string speakerName = npcData != null ? npcData.NpcName : string.Empty;

            // 스프라이트와 이름 설정
            _currentSpeakerId = line.speakerId;
            _view.SetSprite(standingSprite);
            _view.SetName(speakerName);

            // 타이핑 텍스트 출력 (조건식, 플레이스홀더, 한국어 조사 자동 연쇄 처리)
            _typingCts?.Cancel();
            _typingCts?.Dispose();
            _typingCts = new CancellationTokenSource();
            _isTyping = true;

            try
            {
                string formattedText = line.GetFormattedText(parameters);
                await _view.TypingText(formattedText, _typingCts.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isTyping = false;
            }
        }

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
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShopKeeperPresenter] NPC 데이터({speakerId}) 로드 실패: {ex.Message}");
            }

            return null;
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _typingCts?.Cancel();
            _typingCts?.Dispose();
            _typingCts = null;
        }
    }
}
