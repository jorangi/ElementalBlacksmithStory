using ElementalBlacksmithStory.Data;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace ElementalBlacksmithStory.Editor
{
    public class WeaponSearchDropdown : AdvancedDropdown
    {
        private readonly SerializedProperty _targetProperty;

        public WeaponSearchDropdown(AdvancedDropdownState state, SerializedProperty target) : base(state)
        {
            _targetProperty = target;
            minimumSize = new UnityEngine.Vector2(280, 320);
        }
        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("무기 목록 (검색 가능)");
            root.AddChild(new AdvancedDropdownItem("None (비우기)") { id = -1 });

            string[] guids = AssetDatabase.FindAssets("t:SO_WeaponData");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var wep = AssetDatabase.LoadAssetAtPath<SO_WeaponData>(path);
                if (wep == null) continue;
                string nameText = !string.IsNullOrEmpty(wep.weaponName) ? wep.weaponName : wep.name;
                string label = $"[{wep.name}] {nameText}";

                root.AddChild(new WeaponDropdownItem(label, wep));
            }
            return root;
        }
        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is WeaponDropdownItem wepItem)
            {
                _targetProperty.objectReferenceValue = wepItem.Weapon;

                _targetProperty.serializedObject.ApplyModifiedProperties();
            }
            else if (item.id == -1)
            {
                _targetProperty.objectReferenceValue = null;
                _targetProperty.serializedObject.ApplyModifiedProperties();
            }
        }
        private class WeaponDropdownItem : AdvancedDropdownItem
        {
            public SO_WeaponData Weapon { get; }
            public WeaponDropdownItem(string name, SO_WeaponData weapon) : base(name)
            {
                Weapon = weapon;
            }
        }
    }
}