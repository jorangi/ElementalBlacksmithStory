#if UNITY_EDITOR
using ElementalBlacksmithStory.Data;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ElementalBlacksmithStory.Editor
{
    [CustomEditor(typeof(SO_CraftRecipe))]
    [CanEditMultipleObjects]
    public class SO_CraftRecipeEditor : UnityEditor.Editor
    {
        private SerializedProperty _recipeOutcomeProp;
        private SerializedProperty _recipeMaterialsProp;
        private SerializedProperty _resultWeaponProp;
        private SerializedProperty _failWeaponProp;

        private void OnEnable()
        {
            _recipeOutcomeProp = serializedObject.FindProperty("recipeOutcome");
            _recipeMaterialsProp = serializedObject.FindProperty("recipeMaterials");
            _resultWeaponProp = _recipeOutcomeProp.FindPropertyRelative("resultWeapon");
            _failWeaponProp = _recipeOutcomeProp.FindPropertyRelative("defaultFailWeapon");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var weapon = _resultWeaponProp.objectReferenceValue as SO_WeaponData;
            bool hasWeapon = weapon != null;

            string weaponTitle = hasWeapon ?
                (!string.IsNullOrEmpty(weapon.weaponName) ?
                weapon.weaponName : weapon.name) : "미등록";

            string dynamicRecipeTitle = $"{weaponTitle} 제작법";

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    normal = { textColor = hasWeapon ? new Color(0.35f, 0.85f, 0.4f) : Color.gray }
                };
                EditorGUILayout.LabelField("제작법 명칭", dynamicRecipeTitle, titleStyle);
                EditorGUILayout.LabelField("ID", target.name, EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(8);

            DrawWeaponFieldWithSearch("제작 성공", _resultWeaponProp);
            DrawWeaponFieldWithSearch("제작 실패", _failWeaponProp);

            var chanceProp = _recipeOutcomeProp.FindPropertyRelative("chance");
            EditorGUILayout.PropertyField(chanceProp, new GUIContent("성공 확률"));
            EditorGUILayout.Space(8);

            EditorGUILayout.PropertyField(_recipeMaterialsProp, new GUIContent("재료"), true);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawWeaponFieldWithSearch(string label, SerializedProperty weaponProp)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(weaponProp, new GUIContent(label));

            if (GUILayout.Button("🔍 선택", GUILayout.Width(65f)))
            {
                var dropdown = new WeaponSearchDropdown(new AdvancedDropdownState(), weaponProp);
                dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
            }
            EditorGUILayout.EndHorizontal();

            var currentWep = weaponProp.objectReferenceValue as SO_WeaponData;

            if (currentWep != null && !string.IsNullOrEmpty(currentWep.weaponName))
            {
                EditorGUILayout.LabelField($"    └ 현재 무기: {currentWep.weaponName}", EditorStyles.miniLabel);
            }
        }
    }
}











#endif