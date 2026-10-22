#if UNITY_EDITOR
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ElementalBlacksmithStory.Editor
{
    public class RecipeSearchDropdown : AdvancedDropdown
    {
        private readonly SerializedProperty _targetProperty;

        public RecipeSearchDropdown(AdvancedDropdownState state, SerializedProperty target) : base(state)
        {
            _targetProperty = target;
            minimumSize = new Vector2(320, 360);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("제작법 목록 (검색 가능)");
            root.AddChild(new AdvancedDropdownItem("None (비우기)") { id = -1 });

            var swordsGroup = new AdvancedDropdownItem("⚔️ 검 제작법 (Swords)");
            var bowsGroup = new AdvancedDropdownItem("🏹 활 제작법 (Bows)");
            var othersGroup = new AdvancedDropdownItem("📦 기타 제작법");

            int swordsCount = 0;
            int bowsCount = 0;
            int othersCount = 0;

            string[] guids = AssetDatabase.FindAssets("t:SO_CraftRecipe");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var recipe = AssetDatabase.LoadAssetAtPath<SO_CraftRecipe>(path);
                if (recipe == null) continue;

                string resultName = recipe.recipeOutcome.resultWeapon != null
                    ? (!string.IsNullOrEmpty(recipe.recipeOutcome.resultWeapon.weaponName)
                        ? recipe.recipeOutcome.resultWeapon.weaponName
                        : recipe.recipeOutcome.resultWeapon.name)
                    : "결과물 미지정";

                float chancePercent = recipe.recipeOutcome.chance * 100f;
                string label = $"[{recipe.name}] ➔ {resultName} ({chancePercent:0}%)";

                var item = new RecipeDropdownItem(label, recipe);
                if (recipe.name.StartsWith("20"))
                {
                    swordsGroup.AddChild(item);
                    swordsCount++;
                }
                else if (recipe.name.StartsWith("21"))
                {
                    bowsGroup.AddChild(item);
                    bowsCount++;
                }
                else
                {
                    othersGroup.AddChild(item);
                    othersCount++;
                }
            }

            if (swordsCount > 0) root.AddChild(swordsGroup);
            if (bowsCount > 0) root.AddChild(bowsGroup);
            if (othersCount > 0) root.AddChild(othersGroup);

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is RecipeDropdownItem recipeItem)
            {
                _targetProperty.objectReferenceValue = recipeItem.Recipe;
                _targetProperty.serializedObject.ApplyModifiedProperties();
            }
            else if (item.id == -1)
            {
                _targetProperty.objectReferenceValue = null;
                _targetProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        private class RecipeDropdownItem : AdvancedDropdownItem
        {
            public SO_CraftRecipe Recipe { get; }
            public RecipeDropdownItem(string name, SO_CraftRecipe recipe) : base(name)
            {
                Recipe = recipe;
            }
        }
    }
}
#endif
