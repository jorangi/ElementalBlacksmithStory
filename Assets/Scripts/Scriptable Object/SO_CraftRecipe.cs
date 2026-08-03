using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [Serializable]
    public struct RecipeMaterial
    {
        public uint materialId;
        public uint count;
    }

    [Serializable]
    public struct RecipeOutcome
    {
        [Header("레시피 성공확률")]
        [Range(0f, 1f)]
        public float chance;
        [Header("레시피 성공시")]
        public SO_WeaponData resultWeapon;
        [Header("레시피 실패시")]
        public SO_WeaponData defaultFailWeapon;
    }

    [CreateAssetMenu(fileName = "SO_CraftRecipe", menuName = "Scriptable Objects/SO_CraftRecipe")]
    public class SO_CraftRecipe : ScriptableObject
    {
        [Header("레시피 아이디")]
        public uint recipeId;
        [Header("레시피 이름")]
        public string recipeName;
        [Header("레시피 재료")]
        public List<RecipeMaterial> recipeMaterials;
        [Header("레시피 성공")]
        public RecipeOutcome recipeOutcome;
    }
}