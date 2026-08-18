using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using Cysharp.Text;
using R3;

namespace ElementalBlacksmithStory.UI
{
    public class QuickSellView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Button _sellButton;
        private ulong _currentPrice;
        public Observable<Unit> OnClickAsObservable()
        {
            if(_sellButton == null)
            {
                Debug.LogError("[QuickSellView] _sellButton 연결되지 않았습니다.");
                return Observable.Empty<Unit>();
            }
            return _sellButton.OnClickAsObservable();
        }
        private CancellationTokenSource _cts;
        public async UniTaskVoid SetPrice(ulong targetPrice, ulong totalCost)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
        
            double startPrice = _currentPrice;
            double endPrice = targetPrice;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (token.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                double t = Mathf.Clamp01(elapsed / duration);

                _currentPrice = (ulong)(startPrice + (endPrice - startPrice) * t);
                _priceText.SetText(ZString.Format("가격: {0:N0} <sprite name=\"CoinSack\">(총합 소모 비용: {1:N0})", _currentPrice, totalCost));

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _currentPrice = targetPrice;
            _priceText.SetText(ZString.Format("가격: {0:N0} <sprite name=\"CoinSack\">(총합 소모 비용: {1:N0})", _currentPrice, totalCost));
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
