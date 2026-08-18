using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using Cysharp.Text;

namespace ElementalBlacksmithStory.UI
{
    public class MoneyView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _moneyText;
        private ulong _currentMoney;
        private CancellationTokenSource _cts;
        

        public async UniTaskVoid SetMoney(ulong targetMoney)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            double startMoney = _currentMoney;
            double endMoney = targetMoney;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (token.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                double t = Mathf.Clamp01(elapsed / duration);

                _currentMoney = (ulong)(startMoney + (endMoney - startMoney) * t);
                _moneyText.SetText(ZString.Format("<sprite name=\"CoinSack\"> {0:N0}", _currentMoney));

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _currentMoney = targetMoney;
            _moneyText.SetText(ZString.Format("<sprite name=\"CoinSack\"> {0:N0}", _currentMoney));
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
