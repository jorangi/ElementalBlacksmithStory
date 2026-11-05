using System;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    public class AuctionBidder
    {
        /// <summary>
        /// 손님 이름
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// 포함된 그룹
        /// </summary>
        public readonly SO_CustomerGroup Group;
        /// <summary>
        /// 상한가 비율
        /// </summary>
        public readonly float MaxPriceRating;
        /// <summary>
        /// 호가 증액 비율
        /// </summary>
        public readonly float BidIncrementRating;
        /// <summary>
        /// 상품에 대한 욕구 (0.1 ~ 1.0)
        /// </summary>
        public readonly float Desire;
        /// <summary>
        /// 입찰 공격성(0.5~1.5, 표준 1.0)
        /// </summary>
        public readonly float Aggressiveness;
        /// <summary>
        /// 입찰 가능성
        /// </summary>
        public float BidPossibility;
        public Tuple<int, int> Timer;
        private const float IN_DELTA_X0_M = 0.35f; // (호전적 상한가:0.95 - 소극적 상한가:0.60) / (공격성 최대:1.5 - 공격성 최소:0.5) = 0.35
        private const float IN_DELTA_X0_C = 0.425f; // 소극적 상한가:0.60 - (IN_DELTA_X0_M * 공격성 최소: 0.5) = 0.6 - 0.175 = 0.425
        public AuctionBidder(
            string name,
            SO_CustomerGroup group,
            float maxPriceRating,
            float bidIncrementRating,
            float desire,
            float aggressiveness
        )
        {
            Name = name;
            Group = group;
            MaxPriceRating = maxPriceRating;
            BidIncrementRating = bidIncrementRating;
            Desire = Mathf.Clamp01(desire);
            Aggressiveness = Mathf.Clamp(aggressiveness, 0.5f, 1.5f);
            BidPossibility = 0f;
            Timer = new(group._minTimer, group._maxTimer);
        }
        /// <summary>
        /// 입찰을 결정하는 함수
        /// </summary>
        /// <param name="currentPrice">현재 가격</param>
        /// <param name="basePrice">물품 가격</param>
        /// <param name="nextBidPrice">제시할 다음 입찰 가격</param>
        /// <returns>입찰 결정 여부</returns>
        public bool TryEvaluateBid(ulong currentPrice, ulong basePrice, out ulong nextBidPrice)
        {
            nextBidPrice = currentPrice;
            ulong maxPrice = (ulong)(basePrice * MaxPriceRating);

            if (currentPrice >= maxPrice)
            {
                BidPossibility = 0f;
                return false;
            }

            //가격 도달률
            float x = Mathf.Clamp01((float)currentPrice / maxPrice);
            float x0 = IN_DELTA_X0_M * Aggressiveness + IN_DELTA_X0_C;
            float sigmoid = 1.0f / (1.0f + Mathf.Exp(10.0f * (x - x0))); //k=10.0f
            BidPossibility = Mathf.Clamp01(Desire * sigmoid);

            if (UnityEngine.Random.value <= BidPossibility)
            {
                ulong increment = (ulong)Mathf.Max(1, currentPrice * BidIncrementRating);
                ulong calculatedPrice = currentPrice + increment;

                nextBidPrice = calculatedPrice > maxPrice ? maxPrice : calculatedPrice;
                return true;
            }
            return false;
        }
    }
}