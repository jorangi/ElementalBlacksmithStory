using UnityEngine;
using System.Collections.Generic;

namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_WeaponDatabase", menuName = "Scriptable Objects/Database/SO_WeaponDatabase")]
    public class SO_WeaponDatabase : SO_BaseDatabase<SO_WeaponData>
    {
#if UNITY_EDITOR
        [ContextMenu("데이터 자동 등록")]
        public override void AutoRegister() => base.AutoRegister();
#endif
    }
}
