#if UNITY_EDITOR
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace ElementalBlacksmithStory.Editor
{
    [CustomEditor(typeof(SO_WeaponData))]
    [CanEditMultipleObjects]
    public class SO_WeaponDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _weaponNameProp;
        private SerializedProperty _atkProp;
        private SerializedProperty _atsProp;
        private SerializedProperty _costProp;
        private SerializedProperty _recipesProp;
        private SerializedProperty _hideOutlineProp;
        private SerializedProperty _elementTypesProp;
        private SerializedProperty _traitsProp;
        private SerializedProperty _marginProp;
        private SerializedProperty _basePriceProp;
        private SerializedProperty _priceProp;
        private SerializedProperty _cumulativeBasePriceProp;

        private ReorderableList _recipeList;

        private void OnEnable()
        {
            _weaponNameProp = serializedObject.FindProperty("weaponName");
            _atkProp = serializedObject.FindProperty("atk");
            _atsProp = serializedObject.FindProperty("ats");
            _costProp = serializedObject.FindProperty("cost");
            _recipesProp = serializedObject.FindProperty("recipes");
            _hideOutlineProp = serializedObject.FindProperty("hideOutline");
            _elementTypesProp = serializedObject.FindProperty("elementTypes");
            _traitsProp = serializedObject.FindProperty("traits");
            _marginProp = serializedObject.FindProperty("margin");
            _basePriceProp = serializedObject.FindProperty("basePrice");
            _priceProp = serializedObject.FindProperty("price");
            _cumulativeBasePriceProp = serializedObject.FindProperty("cumulativeBasePrice");

            InitRecipeList();
        }

        private void InitRecipeList()
        {
            _recipeList = new ReorderableList(serializedObject, _recipesProp, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, $"파생 강화 레시피 목록 ({_recipesProp.arraySize}개)", EditorStyles.boldLabel);
                },
                elementHeightCallback = index =>
                {
                    if (index >= _recipesProp.arraySize) return 24f;
                    var element = _recipesProp.GetArrayElementAtIndex(index);
                    var recipe = element.objectReferenceValue as SO_CraftRecipe;
                    return recipe != null ? 48f : 26f;
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    if (index >= _recipesProp.arraySize) return;
                    var element = _recipesProp.GetArrayElementAtIndex(index);
                    float rowHeight = EditorGUIUtility.singleLineHeight;

                    Rect fieldRect = new Rect(rect.x, rect.y + 2, rect.width - 70, rowHeight);
                    Rect buttonRect = new Rect(rect.x + rect.width - 65, rect.y + 2, 65, rowHeight);

                    EditorGUI.PropertyField(fieldRect, element, GUIContent.none);

                    if (GUI.Button(buttonRect, "🔍 선택"))
                    {
                        var dropdown = new RecipeSearchDropdown(new AdvancedDropdownState(), element);
                        dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
                    }

                    var recipe = element.objectReferenceValue as SO_CraftRecipe;
                    if (recipe != null)
                    {
                        Rect infoRect = new Rect(rect.x + 10, rect.y + rowHeight + 4, rect.width - 10, rowHeight);
                        string resultInfo = recipe.recipeOutcome.resultWeapon != null
                            ? $"{recipe.recipeOutcome.resultWeapon.weaponName ?? recipe.recipeOutcome.resultWeapon.name} (확률: {recipe.recipeOutcome.chance * 100:0}%)"
                            : "결과물 미지정";
                        string failInfo = recipe.recipeOutcome.defaultFailWeapon != null
                            ? $" / 실패시: {recipe.recipeOutcome.defaultFailWeapon.name}"
                            : "";

                        var subStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = new Color(0.35f, 0.85f, 0.4f) }
                        };
                        EditorGUI.LabelField(infoRect, $"└ 결과: {resultInfo}{failInfo}", subStyle);
                    }
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var weapon = target as SO_WeaponData;
            string weaponTitle = !string.IsNullOrEmpty(_weaponNameProp.stringValue) ? _weaponNameProp.stringValue : "이름 미지정";

            // 상단 타이틀 배너
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = new Color(0.35f, 0.75f, 1f) }
                };
                EditorGUILayout.LabelField($"[{target.name}] {weaponTitle}", titleStyle);

                if (weapon != null)
                {
                    EditorGUILayout.LabelField($"기본 제작비: {weapon.cost:N0} Gold | 기본 판매가: {weapon.basePrice:N0} Gold | ATK: {weapon.atk} | ATS: {weapon.ats}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(8);

            // 1. 기본 정보 및 전투 스펙
            EditorGUILayout.LabelField("무기 기본 정보 & 전투 스펙", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_weaponNameProp, new GUIContent("무기 이름"));
                EditorGUILayout.PropertyField(_atkProp, new GUIContent("공격력 (ATK)"));
                EditorGUILayout.PropertyField(_atsProp, new GUIContent("공격속도 (ATS)"));
                EditorGUILayout.PropertyField(_costProp, new GUIContent("제작비 (Cost)"));
                EditorGUILayout.PropertyField(_hideOutlineProp, new GUIContent("외곽선 숨김"));
                EditorGUILayout.PropertyField(_elementTypesProp, new GUIContent("속성"), true);
                EditorGUILayout.PropertyField(_traitsProp, new GUIContent("특성"), true);
            }

            EditorGUILayout.Space(8);

            // 2. 가격 및 경제 정보
            EditorGUILayout.LabelField("가격 및 경제 정보", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_marginProp, new GUIContent("기본 마진 범위"));
                EditorGUILayout.PropertyField(_basePriceProp, new GUIContent("기준 가격 (BasePrice)"));
                EditorGUILayout.PropertyField(_priceProp, new GUIContent("판매 가격 (Price)"));
                EditorGUILayout.PropertyField(_cumulativeBasePriceProp, new GUIContent("누적 기준가"));
            }

            EditorGUILayout.Space(8);

            // 3. 강화 레시피 목록 (핵심 기능)
            _recipeList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
