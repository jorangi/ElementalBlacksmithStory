using ElementalBlacksmithStory.Data;
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
    }
}