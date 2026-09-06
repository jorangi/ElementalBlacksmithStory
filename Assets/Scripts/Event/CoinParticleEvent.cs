namespace ElementalBlacksmithStory.Event
{
    public readonly struct CoinParticleEvent
    {
        public readonly int Amount;
        public CoinParticleEvent(int amount)
        {
            Amount = amount;
        }
    }
}