using System.Collections.Generic;
using MemoryPack;

namespace ElementalBlacksmithStory.Data.DTO
{
    [MemoryPackable]
    public partial class PlayerSaveData
    {
        public ulong Money { get; set; }
        public Dictionary<uint, uint> Materials { get; set; }
        public Dictionary<uint, uint> Runes { get; set; }
        public List<WeaponSaveData> Equipments { get; set; }
    }
}