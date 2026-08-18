using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName ="SO_MaterialData", menuName = "Scriptable Objects/SO_MaterialData")]
    public class SO_MaterialData : ScriptableObject
    {
        [Header("재료 기본 정보")]
        public uint Id => uint.Parse(this.name);
        public string materialName;
        public string description;
        public float value;
    }
}