using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Core;

namespace ElementalBlacksmithStory.Events
{
    public readonly struct EnhanceRequestEvent
    {
        private static uint _lastId;
        public readonly uint _id;
        public readonly List<RecipeMaterial> materials;

        // public EnhanceRequestEvent(uint id, List<RecipeMaterial> materials = null)
        // {
        //     _id = id;
        //     this.materials = materials;
        // }
        public readonly Weapon Weapon => _weapon;
        private readonly Weapon _weapon;
        public EnhanceRequestEvent(Weapon weapon, List<RecipeMaterial> materials = null)
        {
            _id = _lastId++;
            _weapon = weapon; //추후 위의 id로 통합하고 db화
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