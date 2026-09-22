namespace ElementalBlacksmithStory.Data
{
    public class MaterialShopItem : IShopItem
    {
        private readonly SO_MaterialData _materialData;
        private readonly uint _count;

        public SO_MaterialData Data => _materialData;
        public uint Id => _materialData != null ? _materialData.Id : 0;
        public string Name => Data != null ? Data.materialName : string.Empty;
        public ulong Price => Data != null ? Data.Value : 0;
        public uint SpriteId => Data != null ? Data.Id : 0;
        public uint Count { get; set; }

        public MaterialShopItem(SO_MaterialData materialData, uint count = 1)
        {
            _materialData = materialData;
            _count = count;
        }
    }
}