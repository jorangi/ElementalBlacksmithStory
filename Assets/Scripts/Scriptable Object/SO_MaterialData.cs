using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    public abstract class BaseMaterialData : ScriptableObject
    {
        [Header("기본 정보")]
        public uint Id => uint.Parse(this.name);
        public virtual ulong Value {get;}
    }
    [CreateAssetMenu(fileName ="SO_MaterialData", menuName = "Scriptable Objects/SO_MaterialData")]
    public class SO_MaterialData : BaseMaterialData
    {
        [Header("재료 기본 정보")]
        public string materialName;
        public string description;
        private ulong value = 0;
        public override ulong Value => value;
    }
}