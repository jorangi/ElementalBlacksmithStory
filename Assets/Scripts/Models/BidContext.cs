namespace ElementalBlacksmithStory.Data
{
    public readonly struct BidContext
    {
        public string Bidder { get; }
        public ulong Price { get; }

        public BidContext(string bidder, ulong price)
        {
            Bidder = bidder;
            Price = price;
        }
    }
}
