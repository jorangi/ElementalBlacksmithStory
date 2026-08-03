using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
namespace ElementalBlacksmithStory.Events
{
    public readonly struct EnhanceRequestEvent
    {
        public readonly uint id;
        public readonly List<RecipeMaterial> materials;

        public EnhanceRequestEvent(uint id, List<RecipeMaterial> materials = null)
        {
            this.id = id;
            this.materials = materials;
        }
    }

    public readonly struct EnhanceResultEvent
    {
        public readonly bool IsSuccess {get;}
        public readonly uint WeaponId {get;}

        public EnhanceResultEvent(bool isSuccess, uint weaponId)
        {
            IsSuccess = isSuccess;
            WeaponId = weaponId;
        }
    }
}