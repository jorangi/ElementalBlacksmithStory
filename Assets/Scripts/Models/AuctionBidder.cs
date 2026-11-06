using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    public class AuctionBidder
    {
        private const float IN_DELTA_X0_M = 0.35f; // (호전적 상한가:0.95 - 소극적 상한가:0.60) / (공격성 최대:1.5 - 공격성 최소:0.5) = 0.35
        private const float IN_DELTA_X0_C = 0.425f; // 소극적 상한가:0.60 - (IN_DELTA_X0_M * 공격성 최소: 0.5) = 0.6 - 0.175 = 0.425

        /// <summary>
        /// 손님 이름
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 소속된 손님 그룹
        /// </summary>
        public SO_CustomerGroup Group { get; }

        /// <summary>
        /// 상한가 비율 (원가 대비)
        /// </summary>
        public float MaxPriceRating { get; }

        /// <summary>
        /// 호가 증액 비율
        /// </summary>
        public float BidIncrementRating { get; }

        /// <summary>
        /// 상품에 대한 욕구 (0.0 ~ 1.0)
        /// </summary>
        public float Desire { get; }

        /// <summary>
        /// 입찰 공격성 (0.5 ~ 1.5, 표준 1.0)
        /// </summary>
        public float Aggressiveness { get; }

        /// <summary>
        /// 최근 계산된 입찰 확률 (디버깅/관찰용)
        /// </summary>
        public float BidPossibility { get; private set; }

        /// <summary>
        /// 다음 입찰 검토까지 남은 시간 (초)
        /// </summary>
        public float RemainingSeconds { get; private set; }

        /// <summary>
        /// 상한가 초과 등으로 이번 경매 참여를 포기했는지 여부
        /// </summary>
        public bool IsFolded { get; private set; }

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
            IsFolded = false;

            ResetTimer();
        }

        /// <summary>
        /// 타이머를 갱신하고 입찰 검토 시점 도래 여부를 반환합니다.
        /// </summary>
        public bool Tick(float deltaSeconds)
        {
            if (IsFolded) return false;

            RemainingSeconds -= deltaSeconds;
            return RemainingSeconds <= 0f;
        }

        /// <summary>
        /// 손님 그룹의 _minTimer ~ _maxTimer 범위 내에서 대기 타이머를 재설정합니다.
        /// </summary>
        public void ResetTimer()
        {
            if (Group != null)
            {
                RemainingSeconds = UnityEngine.Random.Range(Group._minTimer, Group._maxTimer + 1);
            }
            else
            {
                RemainingSeconds = 60f;
            }
        }

        /// <summary>
        /// 입찰 참여를 포기 처리합니다.
        /// </summary>
        public void ForceFold()
        {
            IsFolded = true;
            BidPossibility = 0f;
        }

        /// <summary>
        /// 현재 가격과 원가를 기준으로 입찰 여부 및 다음 호가를 산출합니다.
        /// </summary>
        /// <param name="currentPrice">현재 최고 입찰가</param>
        /// <param name="basePrice">물품 기본 원가</param>
        /// <param name="nextBidPrice">제시할 다음 입찰가</param>
        /// <returns>입찰 결정 여부</returns>
        public bool TryEvaluateBid(ulong currentPrice, ulong basePrice, out ulong nextBidPrice)
        {
            nextBidPrice = currentPrice;

            if (IsFolded)
            {
                return false;
            }

            ulong maxPrice = (ulong)(basePrice * MaxPriceRating);

            // 현재가가 이미 자신이 낼 수 있는 상한가를 넘었으면 이번 경매는 포기
            if (currentPrice >= maxPrice)
            {
                ForceFold();
                return false;
            }

            // 가격 도달률 (현재가 / 개인 상한가)
            float x = Mathf.Clamp01((float)currentPrice / maxPrice);
            float x0 = IN_DELTA_X0_M * Aggressiveness + IN_DELTA_X0_C;
            float sigmoid = 1.0f / (1.0f + Mathf.Exp(10.0f * (x - x0))); // k = 10.0f
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