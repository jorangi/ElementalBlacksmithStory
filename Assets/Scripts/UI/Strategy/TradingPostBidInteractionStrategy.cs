using System;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using VContainer;

namespace ElementalBlacksmithStory.UI.Strategy
{
    public class TradingPostBidInteractionStrategy : IItemInteractionStrategy
    {
        public const float STARTING_BID_RATIO = 0.1f;

        private readonly BidPresenter _presenter;
        private readonly TradingPostService _tradingPostService;

        [Inject]
        public TradingPostBidInteractionStrategy(
            BidPresenter presenter,
            TradingPostService tradingPostService = null
        )
        {
            _presenter = presenter;
            _tradingPostService = tradingPostService;
        }

        public ulong GetPrice(IShopItem item)
        {
            if (item == null) return 0;

            if (_tradingPostService != null && _tradingPostService.HighestBids.TryGetValue(item.Id, out var bid))
            {
                return bid.Price;
            }

            ulong startPrice = (ulong)Math.Max(1, Math.Round(item.Value * STARTING_BID_RATIO));
            _tradingPostService?.AddItem(item.Id, new BidContext(string.Empty, startPrice));
            return startPrice;
        }
        /// <summary>
        /// 경매에 입찰하는 것 자체에는 Interact가 가능함
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CanInteract(IShopItem item) => true;
        /// <summary>
        /// 아이템 클릭 시 경매 입찰 모달을 오픈
        /// </summary>
        /// <param name="item"></param>
        public void OnItemClicked(IShopItem item)
        {
            if (item == null) return;
            _presenter.SetItem(item).Forget();
            _presenter.Show();
        }
    }
}