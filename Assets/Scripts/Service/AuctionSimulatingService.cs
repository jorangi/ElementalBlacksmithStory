using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Core
{
    public class AuctionSimulatingService
    {
        /// <summary>
        /// 경매 참여자 목록 (key: 물품 인스턴스 id, value: 참여자 목록)
        /// </summary>
        private readonly Dictionary<uint, List<AuctionBidder>> _bidders;
        public AuctionSimulatingService()
        {
        }
        public void InitBidSetting(IShopItem item)
        {
            var bidders = new Span<AuctionBidder>();

            //RegistBidders(item.Id);

        }
        public void RegistBidders(uint itemID, List<AuctionBidder> bidders)
        {
        }
        public void RemoveBidders(uint itemID, List<AuctionBidder> bidders)
        {
        }
        public void SimulateAuction()
        {
        }
    }
}