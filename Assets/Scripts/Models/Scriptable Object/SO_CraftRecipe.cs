using System;
using System.Collections.Generic;
using UnityEditor;
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
    public class SO_CraftRecipe : ScriptableObject, IIdentifiable
    {
        [Header("제작법 아이디")]
        public uint Id => uint.Parse(this.name);
        [Header("제작법 이름")]
        public string RecipeName => $"{recipeOutcome.resultWeapon.name ?? ""} 제작법";
        [Header("제작법 재료")]
        public List<RecipeMaterial> recipeMaterials;
        [Header("제작법 성공")]
        public RecipeOutcome recipeOutcome;

#if UNITY_EDITOR
        [ContextMenu("대응 되는 ID로 결과물 등록")]
        public void MatchWeaponToResult()
        {
            if (name.StartsWith('2') && name.Length == 5)
            {
                string extractedId = name.Substring(1, 4);
                string[] guids = AssetDatabase.FindAssets("t:SO_WeaponData " + "1" + extractedId);
                if (guids.Length == 0)
                {
                    Debug.LogError("해당 ID에 대응되는 무기가 없습니다.");
                    return;
                }
                SO_WeaponData weapon = AssetDatabase.LoadAssetAtPath<SO_WeaponData>(AssetDatabase.GUIDToAssetPath(guids[0]));
                recipeOutcome.resultWeapon = weapon;

                string failId;
                if (extractedId[^1] == '1' || extractedId[1] == '0')
                {
                    failId = $"{extractedId[0]}001";
                }
                else
                {
                    failId = $"{extractedId[0]}{extractedId[1]}01";
                }
                guids = AssetDatabase.FindAssets("t:SO_WeaponData " + "1" + failId);
                if (guids.Length == 0)
                {
                    Debug.LogError("해당 ID에 대응되는 실패 무기가 없습니다.");
                    return;
                }
                SO_WeaponData failWeapon = AssetDatabase.LoadAssetAtPath<SO_WeaponData>(AssetDatabase.GUIDToAssetPath(guids[0]));
                recipeOutcome.defaultFailWeapon = failWeapon;
                EditorUtility.SetDirty(this);
                Debug.Log($"해당 ID에 대응되는 무기를 등록했습니다: {weapon.name}");
            }
            else
            {
                Debug.LogError("해당 에셋의 ID가 유효하지 않습니다.");
            }
        }
#endif
    }
}