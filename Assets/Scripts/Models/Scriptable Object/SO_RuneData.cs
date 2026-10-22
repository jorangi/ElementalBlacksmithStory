using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Effect;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    public enum RuneGrade
    {
        Common = 5000,
        Uncommon = 10000,
        Rare = 30000,
        Epic = 80000,
        Legendary = 150000,
        Mythical
    }
    [CreateAssetMenu(fileName = "SO_RuneData", menuName = "Scriptable Objects/SO_RuneData")]
    public class SO_RuneData : ScriptableObject, IIdentifiable, IValuable
    {
        [Header("기본 정보")]
        public uint Id => uint.Parse(this.name);
        [SerializeField] private string runeName = string.Empty;
        public string Name => runeName;
        [TextArea(2, 5)]
        public string description = string.Empty;
        public RuneGrade grade = RuneGrade.Common;
        [Tooltip("다음 등급의 룬 데이터")]
        public SO_RuneData nextRune;
        [SerializeField] private ulong value = 0;
        public ulong Value => value;

        [Header("효과 목록")]
        [SerializeReference, SubclassSelector]
        public List<IEffect> effects = new();

#if UNITY_EDITOR
        [Header("메모용")]
        [TextArea(2, 5)]
        public string memo = string.Empty;
#endif
    }
}