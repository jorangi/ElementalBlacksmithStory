using UnityEngine;
using System.Collections.Generic;

namespace ElementalBlacksmithStory.Data
{
    
    [CreateAssetMenu(fileName = "SO_WeaponData", menuName = "Scriptable Objects/SO_WeaponData")]
    public class SO_WeaponData : ScriptableObject
    {
        [Header("무기 기본 정보")]
        public uint Id => uint.Parse(this.name);
        public string weaponName;
        public float atk;
        public float ats;
        public ulong cost;
        [Header("레시피 목록")]
        public List<SO_CraftRecipe> recipes;
        [Header("외곽선 숨김 여부")]
        public bool hideOutline;
        [Header("속성")]
        public List<ElementType> elementTypes;
        [Header("상태이상")]
        public List<StatusEffectData> statusEffects;
        [Header("기본 마진 범위")]
        [Range(0f, 99f)]
        public float margin;
        [Header("판매 가격")] 
        public ulong basePrice; // 외부에서 책정됨
        public ulong price; // 외부에서 책정됨
        public ulong cumulativeBasePrice;
    }
}