using System.Collections.Generic;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_MercenaryDatabase", menuName = "Scriptable Objects/Database/SO_MercenaryDatabase")]
    public class SO_MercenaryDatabase : SO_BaseDatabase<SO_Mercenary>
    {
        public IReadOnlyList<SO_Mercenary> GetAll() => _itemList;
#if UNITY_EDITOR
        [ContextMenu("데이터 자동 등록")]
        public override void AutoRegister() => base.AutoRegister();
#endif
    }
}