using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;

namespace ElementalBlacksmithStory.UI
{
    public class ShopMasterView : MonoBehaviour
    {
        [SerializeField] private Image _standingImage;
        [SerializeField] private RectTransform _namePlateRect;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI contextText;

        public void SetSprite(Sprite sprite)
        {
            if (_standingImage == null)
            {
                Debug.LogError($"[ShopMasterView]_standingImage가 없습니다.");
                return;
            }
            _standingImage.sprite = sprite;
        }

        public void SetName(string name)
        {
            if (nameText == null)
            {
                Debug.LogError($"[ShopMasterView]nameText가 없습니다.");
                return;
            }
            nameText.SetText(name);

            // 텍스트 메쉬 및 네임 플레이트 레이아웃 즉시 강제 갱신
            nameText.ForceMeshUpdate();

            var targetRect = _namePlateRect != null ? _namePlateRect : nameText.transform.parent as RectTransform;
            if (targetRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(targetRect);
            }
            else if (nameText.rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(nameText.rectTransform);
            }
        }

        private CancellationTokenSource _cts = new();

        public async UniTask TypingText(string text, CancellationToken ct = default)
        {
            if (contextText == null)
            {
                Debug.LogError($"[ShopMasterView]contextText가 없습니다.");
                return;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            
            contextText.SetText(text);

            // 1. 텍스트 메쉬 즉시 강제 갱신 (서식 태그 해석 및 크기 계산)
            contextText.ForceMeshUpdate();

            // 2. 말풍선 패널 및 상위 레이아웃 강제 갱신
            if (contextText.transform.parent is RectTransform bubbleRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRect);
                if (bubbleRect.parent is RectTransform rootRect)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                }
            }
            else if (contextText.rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contextText.rectTransform);
            }

            // 3. 서식 태그(<b> 등)를 제외한 실제 글자 수 기준으로 타이핑
            contextText.maxVisibleCharacters = 0;
            int totalLength = contextText.textInfo.characterCount;

            for (int i = 0; i <= totalLength; i++)
            {
                contextText.maxVisibleCharacters = i;

                bool isCanceled = 
                    await UniTask.Delay(TimeSpan.FromMilliseconds(10), cancellationToken: _cts.Token)
                    .SuppressCancellationThrow();

                if (isCanceled) return;
            }

            contextText.maxVisibleCharacters = totalLength;
        }
    }
}
