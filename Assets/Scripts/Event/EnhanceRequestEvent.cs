using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Core;

namespace ElementalBlacksmithStory.Events
{
    public enum EnhanceResult
    {
        SUCESS,
        FAIL, // 운이 안좋았음
        NEEDSMOREMONEY, //돈이 없음
        NOMOREENHANCEMENT, //강화 단계가 더 이상 없음
        INVAILDRECIPE, //재료가 틀림
        LACKOFMATERIALS_WEAPONS, //재료가 틀림
        LACKOFMATERIALS_PUREMATERIALS, //재료가 틀림
    }
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