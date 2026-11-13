using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_MercenarySynergy", menuName = "Scriptable Objects/SO_MercenarySynergy")]
    public class SO_MercenarySynergy : ScriptableObject, IIdentifiable
    {
        public uint Id => uint.Parse(name);
        public string Name = string.Empty;
        public SO_Mercenary[] Targets = new SO_Mercenary[3];
        [TextArea(3, 10)]
        public string Description = string.Empty;
    }
}