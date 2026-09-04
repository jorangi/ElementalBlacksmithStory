namespace ElementalBlacksmithStory.Events
{
    public readonly struct PlaySoundEvent
    {
        public readonly uint Id { get; }
        public readonly bool _isLoop;
        public PlaySoundEvent(uint id, bool isLoop = false)
        {
            Id = id;
            _isLoop = isLoop;
        }
    }
}