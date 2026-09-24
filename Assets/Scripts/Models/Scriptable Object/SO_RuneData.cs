using System;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    public enum RuneGrade
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythical
    }
    [CreateAssetMenu(fileName = "SO_RuneData", menuName = "Scriptable Objects/SO_RuneData")]
    public class SO_RuneData : ScriptableObject, IValuable, IIdentifiable
    {
        [Header("기본 정보")]
        public uint Id => uint.Parse(this.name);
        public string runeName = string.Empty;
        [TextArea(2, 5)]
        public string description = string.Empty;
        public RuneGrade grade = RuneGrade.Common;
        [SerializeField] private ulong value = 0;
        public ulong Value => value;
#if UNITY_EDITOR
        [Header("메모용")]
        [TextArea(2, 5)]
        public string memo = string.Empty;
#endif
    }
}