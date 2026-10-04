#if UNITY_EDITOR
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [CustomEditor(typeof(SO_ShopCategory))]
    [CanEditMultipleObjects]
    public class SO_ShopCategoryEditor : UnityEditor.Editor
    {
        private SerializedProperty _categoryIdProp;
        private SerializedProperty _displayNameProp;
        private SerializedProperty _iconProp;
        private SerializedProperty _minRandomPickCountProp;
        private SerializedProperty _maxRandomPickCountProp;
        private SerializedProperty _itemsProp;
        private ReorderableList _reorderableList;

        // DB 캐싱
        private SO_MaterialDatabase _materialDatabase;
        private SO_WeaponDatabase _weaponDatabase;
        private SO_RuneDatabase _runeDatabase;
        private readonly Dictionary<uint, string> _nameCache = new();

        private void OnEnable()
        {
            _categoryIdProp = serializedObject.FindProperty("_categoryId");
            _displayNameProp = serializedObject.FindProperty("_displayName");
            _iconProp = serializedObject.FindProperty("_icon");
            _minRandomPickCountProp = serializedObject.FindProperty("_minRandomPickCount");
            _maxRandomPickCountProp = serializedObject.FindProperty("_maxRandomPickCount");
            _itemsProp = serializedObject.FindProperty("_items");

            LoadDatabases();

            _reorderableList = new ReorderableList(serializedObject, _itemsProp, true, true, true, true)
            {
                elementHeightCallback = index => EditorGUIUtility.singleLineHeight * 2 + 12,
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "판매 아이템 목록 (ID / 고정 여부 / 수량)", EditorStyles.boldLabel);
                },
                drawNoneElementCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "목록이 비어있습니다. 아래 '+' 버튼을 눌러 아이템을 추가하세요.", EditorStyles.miniLabel);
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    if (index >= _itemsProp.arraySize) return;

                    var element = _itemsProp.GetArrayElementAtIndex(index);
                    var itemIdProp = element.FindPropertyRelative("itemId");
                    var stockProp = element.FindPropertyRelative("stock");
                    var isFixedProp = element.FindPropertyRelative("isFixed");

                    float lineHeight = EditorGUIUtility.singleLineHeight;
                    float y1 = rect.y + 3;
                    float y2 = y1 + lineHeight + 4;

                    // --- [첫 번째 줄] ID 입력 + 아이템 이름 ---
                    float idWidth = 85f;
                    var idRect = new Rect(rect.x, y1, idWidth, lineHeight);
                    EditorGUI.PropertyField(idRect, itemIdProp, GUIContent.none);

                    uint id = (uint)itemIdProp.longValue;
                    string itemName = ResolveItemName(id);

                    var nameRect = new Rect(rect.x + idWidth + 8, y1, rect.width - idWidth - 8, lineHeight);
                    bool hasItem = !string.IsNullOrEmpty(itemName) && itemName != "미등록 ID";
                    var nameStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = id == 0 ? Color.gray : (hasItem ? new Color(0.35f, 0.85f, 0.4f) : new Color(0.9f, 0.4f, 0.4f)) },
                        fontStyle = FontStyle.Bold
                    };
                    string displayText = id == 0 ? "— (ID 입력 대기)" : $"▶ {itemName}";
                    EditorGUI.LabelField(nameRect, displayText, nameStyle);

                    // --- [두 번째 줄] 고정 토글 + 판매 수량 ---
                    float fixedWidth = 90f;
                    var fixedRect = new Rect(rect.x, y2, fixedWidth, lineHeight);
                    isFixedProp.boolValue = EditorGUI.ToggleLeft(fixedRect, "고정 입고", isFixedProp.boolValue);

                    var stockLabelRect = new Rect(rect.x + fixedWidth + 10, y2, 45, lineHeight);
                    EditorGUI.LabelField(stockLabelRect, "수량:");

                    float stockFieldWidth = 80f;
                    var stockRect = new Rect(rect.x + fixedWidth + 55, y2, stockFieldWidth, lineHeight);
                    EditorGUI.PropertyField(stockRect, stockProp, GUIContent.none);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_categoryIdProp);
            EditorGUILayout.PropertyField(_displayNameProp);
            EditorGUILayout.PropertyField(_iconProp);

            EditorGUILayout.Space(6);

            int randomPoolCount = 0;
            for (int i = 0; i < _itemsProp.arraySize; i++)
            {
                var item = _itemsProp.GetArrayElementAtIndex(i);
                if (!item.FindPropertyRelative("isFixed").boolValue)
                {
                    randomPoolCount++;
                }
            }

            EditorGUILayout.LabelField("유동 입고 설정", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min", GUILayout.Width(28));
            int minVal = EditorGUILayout.IntField(_minRandomPickCountProp.intValue, GUILayout.Width(50));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            int maxVal = EditorGUILayout.IntField(_maxRandomPickCountProp.intValue, GUILayout.Width(50));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"(최대 등록: {randomPoolCount}개)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // 음수 방지 (0 이상으로 제한)
            minVal = Mathf.Max(0, minVal);
            maxVal = Mathf.Max(0, maxVal);

            // Min <= Max 보정
            if (minVal > maxVal)
            {
                minVal = maxVal;
            }

            _minRandomPickCountProp.intValue = minVal;
            _maxRandomPickCountProp.intValue = maxVal;

            if (randomPoolCount > 0)
            {
                if (_maxRandomPickCountProp.intValue > randomPoolCount)
                {
                    EditorGUILayout.HelpBox($"Max 추첨 수량({_maxRandomPickCountProp.intValue})이 등록된 유동 아이템 총 개수({randomPoolCount}개)보다 큽니다. 실제 추첨 시에는 최대 {randomPoolCount}개로 자동 제한됩니다.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("현재 등록된 유동(고정 해제) 아이템이 없습니다. 유동 입고를 쓰시려면 [고정 입고] 체크를 끈 아이템을 등록하세요.", MessageType.Info);
            }

            EditorGUILayout.Space(8);
            _reorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void LoadDatabases()
        {
            _nameCache.Clear();

            string[] matGuids = AssetDatabase.FindAssets("t:SO_MaterialDatabase");
            if (matGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(matGuids[0]);
                _materialDatabase = AssetDatabase.LoadAssetAtPath<SO_MaterialDatabase>(path);
                _materialDatabase?.Init();
            }

            string[] wepGuids = AssetDatabase.FindAssets("t:SO_WeaponDatabase");
            if (wepGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(wepGuids[0]);
                _weaponDatabase = AssetDatabase.LoadAssetAtPath<SO_WeaponDatabase>(path);
                _weaponDatabase?.Init();
            }

            string[] runeGuids = AssetDatabase.FindAssets("t:SO_RuneDatabase");
            if (runeGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(runeGuids[0]);
                _runeDatabase = AssetDatabase.LoadAssetAtPath<SO_RuneDatabase>(path);
                _runeDatabase?.Init();
            }
        }

        private string ResolveItemName(uint id)
        {
            if (id == 0) return string.Empty;

            if (_nameCache.TryGetValue(id, out var cachedName))
            {
                return cachedName;
            }

            string foundName = null;

            if (_materialDatabase != null)
            {
                var mat = _materialDatabase.Get(id);
                if (mat != null)
                {
                    foundName = mat.materialName;
                }
            }

            if (foundName == null && _weaponDatabase != null)
            {
                var wep = _weaponDatabase.Get(id);
                if (wep != null)
                {
                    foundName = wep.weaponName;
                }
            }

            if (foundName == null && _runeDatabase != null)
            {
                var rune = _runeDatabase.Get(id);
                if (rune != null)
                {
                    foundName = rune.Name;
                }
            }

            if (foundName == null)
            {
                string[] guids = AssetDatabase.FindAssets($"{id} t:ScriptableObject");
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset is SO_MaterialData m && m.Id == id) { foundName = m.materialName; break; }
                    if (asset is SO_WeaponData w && w.Id == id) { foundName = w.weaponName; break; }
                    if (asset is SO_RuneData r && r.Id == id) { foundName = r.Name; break; }
                }
            }

            string result = foundName ?? "미등록 ID";
            _nameCache[id] = result;
            return result;
        }
    }
}
#endif
