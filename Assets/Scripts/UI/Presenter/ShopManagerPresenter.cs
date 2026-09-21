using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Text;
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
    public class ShopMasterPresenter : IAsyncStartable, IDisposable
    {
        private readonly NPCStandingSpriteLoader _spriteLoader;
        private readonly ShopMasterView _view;
        private readonly Dictionary<uint, SO_NPCData> _npcCache = new();
        private readonly IDisposable _subscription;
        private CancellationTokenSource _typingCts;
        private bool _isTyping = false;

        [Inject]
        public ShopMasterPresenter(
            NPCStandingSpriteLoader spriteLoader,
            ShopMasterView view,
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

        public async UniTask StartAsync(CancellationToken ct = default)
        {
            // 테스트 스위치: true = 단일 품목 케이스, false = 다수 품목 케이스
            bool isSingleItemTest = true;

            Dictionary<string, object> testParams;

            if (isSingleItemTest)
            {
                // [케이스 1] 단품 구매: itemCount == 1 조건 만족
                testParams = new Dictionary<string, object>
                {
                    { "itemCount", 1 },
                    { "item1", "철검" },
                    { "totalAmount", 3 },
                    { "totalPrice", ZString.Format("{0:N0} 골드", 15000) }
                };
            }
            else
            {
                // [케이스 2] 다수 품목 구매: itemCount == 1 조건 불만족 (거짓 분기)
                testParams = new Dictionary<string, object>
                {
                    { "itemCount", 2 },
                    { "item1", "철검" },
                    { "item2", "청동 도끼" },
                    { "item3", "청동 활" },
                    { "totalAmount", 7 },
                    { "totalPrice", ZString.Format("{0:N0} 골드", 38500) }
                };
            }

            await SetDialogueByIdAsync(600307, testParams, 0, ct);
        }

        /// <summary>
        /// 대화 ID로 SO_DialogueData를 로드하여 대사 출력
        /// </summary>
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
                    Debug.LogWarning($"[ShopMasterPresenter] 대사 데이터({dialogueId})를 찾을 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShopMasterPresenter] 대사 데이터({dialogueId}) 로드 실패: {ex.Message}");
            }
        }

        public async UniTask SetDialogue(SO_DialogueData data, IReadOnlyDictionary<string, object> parameters = null, int lineIndex = 0)
        {
            if (_view == null)
            {
                Debug.LogError($"[ShopMasterPresenter] ShopMasterView가 없습니다.");
                return;
            }

            if (data == null || data.contents == null || data.contents.Count == 0)
            {
                Debug.LogWarning($"[ShopMasterPresenter] SO_DialogueData가 없거나 비어있습니다.");
                return;
            }

            if (lineIndex < 0 || lineIndex >= data.contents.Count)
            {
                lineIndex = 0;
            }

            var line = data.contents[lineIndex];

            // 1. SpeakerId로 SO_NPCData 및 스탠딩 스프라이트 가져오기
            var npcData = await GetNPCDataAsync(line.speakerId);
            string speakerName = npcData != null ? npcData.NpcName : string.Empty;
            Sprite standingSprite = null;

            if (_spriteLoader != null && line.speakerId != 0)
            {
                standingSprite = await _spriteLoader.GetSprite(line.speakerId);
            }

            // 2. View에 스프라이트와 이름 설정
            _view.SetSprite(standingSprite);
            _view.SetName(speakerName);

            // 3. 타이핑 텍스트 출력 (조건식, 플레이스홀더, 한국어 조사 자동 연쇄 처리)
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
                Debug.LogWarning($"[ShopMasterPresenter] NPC 데이터({speakerId}) 로드 실패: {ex.Message}");
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
