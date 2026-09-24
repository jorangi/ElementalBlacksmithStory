using System.Collections.Generic;
using UnityEngine;
namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_RuneDatabase", menuName = "Scriptable Objects/Database/SO_RuneDatabase")]
    public class SO_RuneDatabase : SO_BaseDatabase<SO_RuneData>
    {
#if UNITY_EDITOR
        [ContextMenu("데이터 자동 등록")]
        public override void AutoRegister() => base.AutoRegister();
#endif
    }
}