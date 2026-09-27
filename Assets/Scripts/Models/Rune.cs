using ElementalBlacksmithStory.Data;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class Rune : IShopItem
    {
        private static uint _id = 0;
        public uint Id { get; }
        public string Name => _data.runeName;
        public ulong Price => _data.Value;
        public uint SpriteId => _data.Id;
        public uint Count => 1;
        private SO_RuneData _data;
        [Inject]
        public Rune(SO_RuneData data)
        {
            _data = data;
            Id = _id++;
        }
    }
}