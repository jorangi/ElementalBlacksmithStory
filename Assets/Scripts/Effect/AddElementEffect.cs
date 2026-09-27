using System;
using ElementalBlacksmithStory.Data;
using UnityEngine;

namespace ElementalBlacksmithStory.Effect
{
    /// <summary>
    /// 속성을 부여하는 효과
    /// </summary>
    [Serializable]
    public class AddElementEffect : IEffect
    {
        [SerializeField] private ElementType elementType;

        public ElementType ElementType => elementType;
        public string Description => $"무기에 {elementType} 속성을 추가합니다.";

        public void OnEquip() { }
        public void OnUnequip() { }
    }
}
