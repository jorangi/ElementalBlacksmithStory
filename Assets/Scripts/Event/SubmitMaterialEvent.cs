namespace ElementalBlacksmithStory.Events
{
    public struct SubmitMaterialEvent
    {
        private static uint _lastId = 0;
        public uint Id {get; private set;}
        public uint MaterialId {get; private set;}
        public uint Amount {get; private set;}
        public SubmitMaterialEvent(uint id, uint amount)
        {
            Id = _lastId++;
            MaterialId = id;
            Amount = amount;
        }
    }
}