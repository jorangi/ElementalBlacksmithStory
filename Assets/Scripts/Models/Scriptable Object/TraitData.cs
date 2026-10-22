using System;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    /// <summary>
    /// 영웅/장비/룬에 부여되는 특성(Trait) 종류
    /// </summary>
    public enum TraitType
    {
        None,
        //TODO : 처형, 흡혈, 등등 추가할 듯
    }

    /// <summary>
    /// 특성 데이터
    /// </summary>
    [Serializable]
    public class TraitData
    {
        [Tooltip("특성 종류")]
        public TraitType type;

        [Tooltip("특성 수치 (퍼센트 또는 고정값)")]
        public float value;

        [Tooltip("특성 설명 및 메모")]
        public string description;
    }
}
