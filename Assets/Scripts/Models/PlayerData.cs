using System;
using R3;

namespace ElementalBlacksmithStory.Data
{
    public class PlayerData : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly ReactiveProperty<ulong> _money = new(1000000000);

        /// <summary>
        /// 소지금
        /// </summary>
        public ReadOnlyReactiveProperty<ulong> Money => _money;
        /// <summary>
        /// 소지금 변경
        /// </summary>
        /// <param name="amount">변경할 금액</param>
        public void SetMoney(ulong amount)
        {
            _money.Value = amount;
        }
        public void AddMoney(ulong amount)
        {
            _money.Value += amount;
        }
        public bool TrySpend(ulong amount)
        {
            if (_money.Value >= amount)
            {
                _money.Value -= amount;
                return true;
            }
            return false;
        }
        public bool CanAfford(ulong amount) => _money.Value >= amount;
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}