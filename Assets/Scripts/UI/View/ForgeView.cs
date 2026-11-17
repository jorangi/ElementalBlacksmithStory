using System.Threading;
using Cysharp.Text;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class ForgeView : MonoBehaviour
    {
        [SerializeField] private Button _enhanceButton;
        [SerializeField] private Button _quickSellButton;
        [SerializeField] private Button _inventoryButton;
        [SerializeField] private Button _recipeTreeButton;

        [SerializeField] private TextMeshProUGUI _weaponNameText;
        [SerializeField] private TextMeshProUGUI _enhanceCostText;
        [SerializeField] private TextMeshProUGUI _enhanceChanceText;
        [SerializeField] private TextMeshProUGUI _quickSellPriceText;

        public Observable<Unit> OnEnhanceButtonAsObservable()
        {
            if (_enhanceButton == null)
            {
                Debug.LogWarning("[ForgeView]_enhanceButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _enhanceButton.OnClickAsObservable();
        }
        public Observable<Unit> OnQuickSellButtonAsObservable()
        {
            if (_quickSellButton == null)
            {
                Debug.LogWarning("[ForgeView]_quickSellButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _quickSellButton.OnClickAsObservable();
        }
        public Observable<Unit> OnInventoryButtonAsObservable()
        {
            if (_inventoryButton == null)
            {
                Debug.LogWarning("[ForgeView]_inventoryButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _inventoryButton.OnClickAsObservable();
        }
        public Observable<Unit> OnRecipeTreeButtonAsObservable()
        {
            if (_recipeTreeButton == null)
            {
                Debug.LogWarning("[ForgeView]_recipeTreeButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _recipeTreeButton.OnClickAsObservable();
        }
        public void SetWeaponNameText(string text)
        {
            _weaponNameText.SetText(text);
        }
        public void SetEnhanceCostText(string text)
        {
            _enhanceCostText.SetText(text);
        }
        public void SetEnhanceChanceText(string text)
        {
            _enhanceChanceText.SetText(text);
        }
        private ulong _currentQuickSellPrice;
        private CancellationTokenSource _priceCts;

        private void OnDestroy()
        {
            _priceCts?.Cancel();
            _priceCts?.Dispose();
        }

        public async UniTaskVoid SetQuickSellPriceAsync(ulong targetPrice)
        {
            _priceCts?.Cancel();
            _priceCts?.Dispose();
            _priceCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var token = _priceCts.Token;

            double startPrice = _currentQuickSellPrice;
            double endPrice = targetPrice;
            const float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (token.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                double t = Mathf.Clamp01(elapsed / duration);

                _currentQuickSellPrice = (ulong)(startPrice + (endPrice - startPrice) * t);
                _quickSellPriceText.SetText(ZString.Format("<size=60%><sprite name=\"CoinSack\"></size>{0:N0} G", _currentQuickSellPrice));

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _currentQuickSellPrice = targetPrice;
            _quickSellPriceText.SetText(ZString.Format("<size=60%><sprite name=\"CoinSack\"></size>{0:N0} G", _currentQuickSellPrice));
        }
    }
}