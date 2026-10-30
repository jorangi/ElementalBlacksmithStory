using System.Collections.Generic;
using ElementalBlacksmithStory.Core;
using MemoryPack;

namespace ElementalBlacksmithStory.Data.DTO
{
    public partial class EnhanceStepSaveData
    {
        public uint WeaponId { get; set; }
        public ulong AddedBasePrice { get; set; }
        public ulong Cost { get; set; }
    }
    [MemoryPackable]
    public partial class WeaponSaveData
    {
        public uint InstanceId { get; set; }
        public uint WeaponDataId { get; set; }
        public uint BaseWeaponDataId { get; set; }
        public ulong Cost { get; set; }
        public ulong BasePrice { get; set; }
        public ulong Price { get; set; }
        public uint MinimunGuarantee { get; set; }
        public int RuneSlot { get; set; }
        public uint[] SocketedRuneIds { get; set; }
        public List<EnhanceStepSaveData> EnhanceHistory { get; set; }
    }
}