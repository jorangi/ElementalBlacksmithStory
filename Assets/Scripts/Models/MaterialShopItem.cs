using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Inventory;

namespace ElementalBlacksmithStory.Data
{
    public class MaterialShopItem : IShopItem
    {
        private static uint _lastInstanceId = 1000000;
        public static uint GenerateInstanceId() => ++_lastInstanceId;

        private readonly SO_MaterialData _materialData;
        private readonly uint _count;
        private readonly uint _id;

        public SO_MaterialData Data => _materialData;
        public uint Id => _id;
        public string Name => Data != null ? Data.materialName : string.Empty;
        public ulong Value => Data != null ? Data.Value : 0;
        public uint ItemId => Data != null ? Data.Id : 0;
        public uint Count => _count;
        public ShopItemCategory Category => ShopItemCategory.Material;

        public MaterialShopItem(SO_MaterialData materialData, uint count = 1)
        {
            _materialData = materialData;
            _count = count > 0 ? count : 1;
            _id = materialData != null ? materialData.Id : 0;
        }

        public MaterialShopItem(SO_MaterialData materialData, uint count, uint customId)
        {
            _materialData = materialData;
            _count = count > 0 ? count : 1;
            _id = customId;
        }

        public void OnPurchased(IInventoryContext context, uint amount)
        {
            if (_materialData != null)
            {
                context.AcquireItemPublisher.Publish(new AcquireItemEvent(_materialData.Id, amount, sender: "Shop", reason: "Shop_Purchase"));
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
