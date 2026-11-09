namespace ElementalBlacksmithStory.Events
{
    public readonly struct QuickSellEvent
    {
        public readonly ulong Price { get; }
        public QuickSellEvent(ulong price)
        {
            Price = price;
        }
    }
}