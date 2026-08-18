using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [Serializable]
    public struct RecipeMaterial
    {
        public SO_MaterialData material;
        public uint count;
    }

    [Serializable]
    public struct RecipeOutcome
    {
        [Header("제작법 성공확률")]
        [Range(0f, 1f)]
        public float chance;
        [Header("제작법 성공시")]
        public SO_WeaponData resultWeapon;
        [Header("제작법 실패시")]
        public SO_WeaponData defaultFailWeapon;
    }

    [CreateAssetMenu(fileName = "SO_CraftRecipe", menuName = "Scriptable Objects/SO_CraftRecipe")]
    public class SO_CraftRecipe : ScriptableObject
    {
        [Header("제작법 아이디")]
        public uint Id => uint.Parse(this.name);
        [Header("제작법 이름")]
        public string recipeName;
        [Header("제작법 재료")]
        public List<RecipeMaterial> recipeMaterials;
        [Header("제작법 성공")]
        public RecipeOutcome recipeOutcome;
    }
}