using UnityEngine;
using System.Collections.Generic;

namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_MaterialDatabase", menuName = "Scriptable Objects/Database/SO_MaterialDatabase")]
    public class SO_MaterialDatabase : SO_BaseDatabase<SO_MaterialData>
    {
#if UNITY_EDITOR
        [ContextMenu("데이터 자동 등록")]
        public override void AutoRegister() => base.AutoRegister();
#endif
    }
}
