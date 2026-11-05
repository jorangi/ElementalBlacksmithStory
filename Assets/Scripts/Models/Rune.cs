using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Inventory;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class Rune : IShopItem
    {
        private static uint _lastInstanceId = 2000000;
        public static uint GenerateInstanceId() => ++_lastInstanceId;

        private readonly SO_RuneData _data;

        public SO_RuneData Data => _data;
        public readonly uint _count;
        private readonly uint _id;

        public uint Id => _id;
        public string Name => _data != null ? _data.Name : string.Empty;
        public ulong Value => _data != null ? _data.Value : 0;
        public uint ItemId => _data != null ? _data.Id : 0;
        public uint Count => _count;
        public ShopItemCategory Category => ShopItemCategory.Rune;

        [Inject]
        public Rune(SO_RuneData data) : this(data, 1)
        {
        }
        public Rune(SO_RuneData data, uint count)
        {
            _data = data;
            _count = count > 0 ? count : 1;
            _id = data != null ? data.Id : 0;
        }

        public Rune(SO_RuneData data, uint count, uint customId)
        {
            _data = data;
            _count = count > 0 ? count : 1;
            _id = customId;
        }

        public void OnPurchased(IInventoryContext context, uint amount)
        {
            if (_data != null)
            {
                context.AcquireItemPublisher.Publish(new AcquireItemEvent(_data.Id, amount, sender: "Shop", reason: "Shop_Purchase"));
            }
        }

        public uint GetOwnedCount(IInventoryContext context)
        {
            if (_data == null) return 0;
            return context.RuneInventory.GetCount(_data);
        }

        public void OnSold(IInventoryContext context, uint amount)
        {
            if (_data != null)
            {
                context.RuneInventory.Get(_data, amount);
            }
        }
    }
}
