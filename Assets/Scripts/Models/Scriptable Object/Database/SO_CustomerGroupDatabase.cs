using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_CustomerGroupDatabase", menuName = "Scriptable Objects/Database/SO_CustomerGroupDatabase")]
    public class SO_CustomerGroupDatabase : SO_BaseDatabase<SO_CustomerGroup>
    {
#if UNITY_EDITOR
        [ContextMenu("데이터 자동 등록")]
        public override void AutoRegister() => base.AutoRegister();
#endif
    }
}