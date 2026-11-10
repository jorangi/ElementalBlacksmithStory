using MemoryPack;

namespace ElementalBlacksmithStory.Data
{
    [MemoryPackable]
    public partial struct PriceRecord
    {
        public ulong HighestPrice;
        public ulong LowestPrice;
        public ulong TotalPrice;        // 평균가 계산용 누적합
        public uint TransactionCount;   // 총 거래 횟수
        public ulong LatestPrice; // 최근 판매가
        public int LatestPriceRemainingUses;

        public readonly ulong AveragePrice => TransactionCount > 0 ? TotalPrice / TransactionCount : 0;

        public void AddRecord(ulong price, ulong basePrice = 0)
        {
            if (TransactionCount == 0 || price > HighestPrice) HighestPrice = price;
            if (TransactionCount == 0 || price < LowestPrice) LowestPrice = price;

            bool shouldUpdateLatest = (LatestPriceRemainingUses == 0 && price > basePrice)
                                   || (LatestPriceRemainingUses > 0 && price > LatestPrice);

            if (shouldUpdateLatest)
            {
                LatestPrice = price;
                LatestPriceRemainingUses = 3;
            }

            TotalPrice += price;
            TransactionCount++;
        }
        public bool TryConsumeLatestPrice(out ulong price)
        {
            if (LatestPriceRemainingUses > 0 && LatestPrice > 0)
            {
                price = LatestPrice;
                LatestPriceRemainingUses--;
                return true;
            }
            price = 0;
            return false;
        }
    }
}