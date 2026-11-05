using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class AuctionSimulatingService
    {
        private readonly ItemTierService _itemTierService;
        private readonly SO_CustomerGroupDatabase _customerGroupDatabase;

        /// <summary>
        /// 경매 참여자 목록 (key: 물품 인스턴스 id, value: 참여자 목록)
        /// </summary>
        private readonly Dictionary<uint, List<AuctionBidder>> _bidders = new();

        public IReadOnlyDictionary<uint, List<AuctionBidder>> Bidders => _bidders;

        [Inject]
        public AuctionSimulatingService(
            ItemTierService itemTierService,
            SO_CustomerGroupDatabase customerGroupDatabase = null)
        {
            _itemTierService = itemTierService;
            _customerGroupDatabase = customerGroupDatabase;
        }

        /// <summary>
        /// 아이템의 티어에 따른 입찰자 수를 산출하고 입찰 세팅을 초기화합니다.
        /// </summary>
        public void InitBidSetting(IShopItem item)
        {
            if (item == null) return;

            float tier = _itemTierService.GetNormalizedTier(item);
            int bidderCount = DetermineBidderCount(tier);

            // TODO: bidderCount 만큼 customerGroupDatabase에서 가중치 추첨하여 AuctionBidder 생성
        }

        private int DetermineBidderCount(float tier)
        {
            if (tier < 0.35f) return UnityEngine.Random.Range(3, 5); // 3 ~ 4명
            if (tier < 0.70f) return UnityEngine.Random.Range(5, 7); // 5 ~ 6명
            return UnityEngine.Random.Range(7, 10);                   // 7 ~ 9명
        }

        public void RegistBidders(uint itemID, List<AuctionBidder> bidders)
        {
            _bidders[itemID] = bidders;
        }

        public void RemoveBidders(uint itemID)
        {
            _bidders.Remove(itemID);
        }

        public void SimulateAuction()
        {
        }
    }
}
