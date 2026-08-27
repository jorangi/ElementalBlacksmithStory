using System.Threading;
using UnityEngine;
using TMPro;
using Cysharp.Text;
using Cysharp.Threading.Tasks;

namespace ElementalBlacksmithStory.UI
{
    public class EnhanceChanceView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI chanceText;
        private CancellationTokenSource _cts;
        private float _defaultY = 0f;
        private const float TARGET_SHOW_Y = 429.1f;

        private void Awake()
        {
            if (chanceText != null)
            {
                _defaultY = chanceText.rectTransform.anchoredPosition.y;
            }
        }

        public void SetChance(float chance, ulong cost)
        {
            chanceText.SetText(
                ZString.Format("{0:F2}% <size=\"40%\">(-{1:N0} <sprite name=\"CoinSack\">)</size>",
                chance * 100.0f,
                cost));
        }

        /// <summary>
        /// 확률 텍스트 보이게/올라가게 하는 애니메이션 (목표 Y: 429.1)
        /// </summary>
        public async UniTaskVoid ShowingChanceAnimation()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new();
            var token = _cts.Token;
            var rect = chanceText.rectTransform;

            while (rect.anchoredPosition.y < TARGET_SHOW_Y - 1f)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, new Vector2(rect.anchoredPosition.x, TARGET_SHOW_Y), 0.1f);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, TARGET_SHOW_Y);
        }

        /// <summary>
        /// 확률 텍스트 숨기게/원위치로 내리는 애니메이션
        /// </summary>
        public async UniTaskVoid HidingChanceAnimation()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new();
            var token = _cts.Token;
            var rect = chanceText.rectTransform;

            while (rect.anchoredPosition.y > _defaultY + 1f)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, new Vector2(rect.anchoredPosition.x, _defaultY), 0.1f);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, _defaultY);
        }

        /// <summary>
        /// 드래그 등으로 Y 위치를 실시간 동기화
        /// </summary>
        public void SyncYPosition(float y)
        {
            if (chanceText != null)
            {
                chanceText.rectTransform.anchoredPosition = new Vector2(chanceText.rectTransform.anchoredPosition.x, y);
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}