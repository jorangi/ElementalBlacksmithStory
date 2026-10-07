using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Inventory;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class Rune : IShopItem
    {
        private readonly SO_RuneData _data;

        public SO_RuneData Data => _data;
        public uint Id => _data != null ? _data.Id : 0;
        public string Name => _data != null ? _data.Name : string.Empty;
        public ulong Value => _data != null ? _data.Value : 0;
        public uint SpriteId => _data != null ? _data.Id : 0;
        public uint Count => 1;

        [Inject]
        public Rune(SO_RuneData data)
        {
            _data = data;
        }

        public void OnPurchased(IInventoryContext context, uint amount)
        {
            if (_data != null)
            {
                context.RuneInventory.Add(_data, amount);
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