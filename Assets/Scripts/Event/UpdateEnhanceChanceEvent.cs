namespace ElementalBlacksmithStory.Events
{
    /// <summary>
    /// 강화 성공 확률 및 강화 비용을 UI에 전달하는 이벤트
    /// </summary>
    public readonly struct UpdateEnhanceChanceEvent
    {
        public readonly float Chance;
        public readonly ulong Cost;

        public UpdateEnhanceChanceEvent(float chance, ulong cost)
        {
            Chance = chance;
            Cost = cost;
        }
    }
}
