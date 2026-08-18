namespace ElementalBlacksmithStory.Events
{
    public readonly struct SellEvent
    {
        public readonly ulong Price { get; }
        public SellEvent(ulong price)
        {
            Price = price;
        }
    }
}