using ElementalBlacksmithStory.Inventory;

namespace ElementalBlacksmithStory.Data
{
    public class MaterialShopItem : IShopItem
    {
        private readonly SO_MaterialData _materialData;
        private readonly uint _count;

        public SO_MaterialData Data => _materialData;
        public uint Id => _materialData != null ? _materialData.Id : 0;
        public string Name => Data != null ? Data.materialName : string.Empty;
        public ulong Value => Data != null ? Data.Value : 0;
        public uint SpriteId => Data != null ? Data.Id : 0;
        public uint Count { get; set; }

        public MaterialShopItem(SO_MaterialData materialData, uint count = 1)
        {
            _materialData = materialData;
            _count = count;
        }

        public void OnPurchased(IInventoryContext context, uint amount)
        {
            if (_materialData != null)
            {
                context.MaterialInventory.Add(_materialData, amount);
            }
        }

        public uint GetOwnedCount(IInventoryContext context)
        {
            if (_materialData == null) return 0;
            return context.MaterialInventory.GetCount(_materialData);
        }

        public void OnSold(IInventoryContext context, uint amount)
        {
            if (_materialData != null)
            {
                context.MaterialInventory.Get(_materialData, amount);
            }
        }
    }
}