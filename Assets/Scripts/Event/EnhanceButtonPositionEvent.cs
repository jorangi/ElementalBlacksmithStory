namespace ElementalBlacksmithStory.Events
{
    public readonly struct EnhanceButtonPositionEvent
    {
        public readonly float positionY;
        public EnhanceButtonPositionEvent(float y) => positionY = y;
    }
}