namespace ElementalBlacksmithStory.Events
{
    public readonly struct PlaySoundEvent
    {
        public readonly uint Id { get; }
        public PlaySoundEvent(uint id)
        {
            Id = id;
        }
    }
}