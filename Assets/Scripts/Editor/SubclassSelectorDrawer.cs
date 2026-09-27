#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ElementalBlacksmithStory.Core
{
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Type baseType = GetFieldType();
            var types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract && !t.IsInterface && t.GetCustomAttribute<SerializableAttribute>() != null)
                .ToList();
            string typeName = string.IsNullOrEmpty(property.managedReferenceFullTypename)
                ? "null (선택 안 됨)"
                : property.managedReferenceFullTypename.Split(' ').Last().Split('.').Last();

            Rect dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // 타입 선택 드롭다운 버튼
            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent($"타입: {typeName}"), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("null"), property.managedReferenceValue == null, () =>
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                });

                foreach (var type in types)
                {
                    menu.AddItem(new GUIContent(type.Name), typeName == type.Name, () =>
                    {
                        property.managedReferenceValue = Activator.CreateInstance(type);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }
            if (property.managedReferenceValue != null)
            {
                Rect contentRect = new Rect(
                    position.x, 
                    position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, 
                    position.width, 
                    position.height - EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing
                );
                EditorGUI.indentLevel++;
                var endProperty = property.GetEndProperty();
                var childProperty = property.Copy();
                childProperty.NextVisible(true);

                float currentY = contentRect.y;
                while (!SerializedProperty.EqualContents(childProperty, endProperty))
                {
                    float height = EditorGUI.GetPropertyHeight(childProperty, true);
                    Rect propRect = new Rect(contentRect.x, currentY, contentRect.width, height);
                    EditorGUI.PropertyField(propRect, childProperty, true);
                    currentY += height + EditorGUIUtility.standardVerticalSpacing;

                    if (!childProperty.NextVisible(false))
                        break;
                }
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference || property.managedReferenceValue == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            float totalHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var endProperty = property.GetEndProperty();
            var childProperty = property.Copy();
            childProperty.NextVisible(true);

            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                totalHeight += EditorGUI.GetPropertyHeight(childProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                if (!childProperty.NextVisible(false))
                    break;
            }

            return totalHeight;
        }

        private Type GetFieldType()
        {
            Type fieldType = fieldInfo.FieldType;
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                return fieldType.GetGenericArguments()[0];
            return fieldType;
        }
    }
}
#endif
