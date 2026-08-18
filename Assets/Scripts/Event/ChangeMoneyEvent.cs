namespace ElementalBlacksmithStory.Events
{
    public readonly struct ChangeMoneyEvent
    {
        private const ulong MAXMONEY = 9999999999999;
        private static uint _lastId = 0;
        public readonly uint Id { get; }
        public readonly ulong MoneyChange { get; }
        public ChangeMoneyEvent(out uint id, ulong moneyChange)
        {
            this = default;
            id = _lastId++;
            MoneyChange = System.Math.Clamp(moneyChange, 0UL, MAXMONEY);
        }
    }
}