using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    public interface IDatabase
    {
#if UNITY_EDITOR
        [ContextMenu("데이터베이스 자동 동기화")]
        public void AutoRegister();
#endif
    }
}