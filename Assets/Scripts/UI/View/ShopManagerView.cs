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
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            
            contextText.SetText(text);
            contextText.maxVisibleCharacters = 0;
            int totalLength = text.Length;

            for (int i = 0; i <= totalLength; i++)
            {
                contextText.maxVisibleCharacters = i;

                bool isCanceled = 
                    await UniTask.Delay(TimeSpan.FromMilliseconds(10), cancellationToken: _cts.Token)
                    .SuppressCancellationThrow();

                if (isCanceled) break;
            }
            contextText.maxVisibleCharacters = totalLength;
        }
    }
}
