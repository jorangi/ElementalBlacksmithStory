#if UNITY_EDITOR
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ElementalBlacksmithStory.Editor
{
    public class BatchAssetRenamerWindow : EditorWindow
    {
        public enum RenameMode
        {
            Replace, //문자열 변경
            AddAffix, // 접두사/접미사 추가 및 제거
            Numbering // 일괄 이름 지정 + 순번 넘버링
        }

        private RenameMode _mode = RenameMode.Replace;

        private string _findString = "";
        private string _replaceString = "";
        private bool _matchCase = true;
        private bool _useRegex = false;

        private string _prefix = "";
        private string _suffix = "";


        private string _baseName = "Asset";
        private int _startIndex = 1;
        private int _digits = 3;

        private readonly List<UnityEngine.Object> _targetAssets = new();
        private UnityEngine.Vector2 _scrollPos;

        [MenuItem("Tools/에셋 일괄 이름 변경")]
        public static void Open()
        {
            var win = GetWindow<BatchAssetRenamerWindow>("Batch Renamer");
            win.minSize = new UnityEngine.Vector2(520, 560);
        }
        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("범용 에셋 이름 일괄 변경", EditorStyles.boldLabel);

            DrawDropArea();

            EditorGUILayout.Space(6);

            _mode = (RenameMode)GUILayout.Toolbar((int)_mode, new[]
            {
                "찾기 & 바꾸기", "접두사 / 접미사,", "일괄 넘버링"
            });
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawModeSettings();
            }
            EditorGUILayout.Space(6);
            DrawPreviewList();
            EditorGUILayout.Space(8);
            DrawActionButtons();
        }
        private void DrawDropArea()
        {
            var dropRect = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "에셋이나 폴더를 드래그 앤 드롭 하세요.", EditorStyles.helpBox);

            var evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition)) return;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        string path = AssetDatabase.GetAssetPath(obj);
                        if (AssetDatabase.IsValidFolder(path))
                        {
                            string[] guids = AssetDatabase.FindAssets("", new[] { path });
                            foreach (var guid in guids)
                            {
                                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                                if (AssetDatabase.IsValidFolder(assetPath)) continue;
                                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                                if (asset != null && !_targetAssets.Contains(asset))
                                    _targetAssets.Add(asset);
                            }
                        }
                        else
                        {
                            if (obj != null && !_targetAssets.Contains(obj))
                                _targetAssets.Add(obj);
                        }
                    }
                }
                evt.Use();
            }
        }
        private void DrawModeSettings()
        {
            switch (_mode)
            {
                case RenameMode.Replace:
                    _findString = EditorGUILayout.TextField("찾을 문자열", _findString);
                    _replaceString = EditorGUILayout.TextField("바꿀 문자열", _replaceString);
                    EditorGUILayout.BeginHorizontal();
                    _matchCase = EditorGUILayout.ToggleLeft("대소문자 구분", _matchCase, GUILayout.Width(110));
                    _useRegex = EditorGUILayout.ToggleLeft("정규식(Regex)", _useRegex, GUILayout.Width(130));
                    EditorGUILayout.EndHorizontal();
                    break;
                case RenameMode.AddAffix:
                    _prefix = EditorGUILayout.TextField("접두사", _prefix);
                    _suffix = EditorGUILayout.TextField("접미사", _suffix);
                    break;
                case RenameMode.Numbering:
                    _baseName = EditorGUILayout.TextField("기본 이름", _baseName);
                    _startIndex = EditorGUILayout.IntField("시작 번호", _startIndex);
                    _digits = EditorGUILayout.IntSlider("자리수", _digits, 1, 6);
                    EditorGUILayout.LabelField($"예시: {_baseName}_{_startIndex.ToString(new string('0', _digits))}", EditorStyles.miniLabel);
                    break;
            }
        }
        private void DrawPreviewList()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📋 대상 목록 ({_targetAssets.Count}개)", EditorStyles.boldLabel);
            if (GUILayout.Button("목록 비우기", GUILayout.Width(80)))
            {
                _targetAssets.Clear();
            }
            EditorGUILayout.EndHorizontal();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, EditorStyles.helpBox);
            if (_targetAssets.Count == 0)
            {
                EditorGUILayout.HelpBox("목록이 비어있습니다. 에셋이나 폴더를 위 드래그 영역에 올려놓으세요.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < _targetAssets.Count; i++)
                {
                    var asset = _targetAssets[i];
                    if (asset == null) continue;
                    string currentName = asset.name;
                    string newName = CalculateNewName(currentName, i);
                    bool isChanged = currentName != newName;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(asset, typeof(UnityEngine.Object), false, GUILayout.Width(150));
                    EditorGUILayout.LabelField("➔", GUILayout.Width(20));
                    var style = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = isChanged ? new Color(0.35f, 0.85f, 0.4f) : Color.gray },
                        fontStyle = isChanged ? FontStyle.Bold : FontStyle.Normal
                    };
                    EditorGUILayout.LabelField(newName, style, GUILayout.Width(180));
                    EditorGUILayout.LabelField(isChanged ? "[변경]" : "[동일]", EditorStyles.miniLabel, GUILayout.Width(45));
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        _targetAssets.RemoveAt(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }
        private void DrawActionButtons()
        {
            using (new EditorGUI.DisabledScope(_targetAssets.Count == 0))
            {
                if (GUILayout.Button("🚀 일괄 이름 변경 실행 (Safe Rename)", GUILayout.Height(36)))
                {
                    ExecuteRenameAll();
                }
            }
        }
        private string CalculateNewName(string originalName, int index)
        {
            switch (_mode)
            {
                case RenameMode.Replace:
                    if (string.IsNullOrEmpty(_findString)) return originalName;
                    if (_useRegex)
                    {
                        try { return Regex.Replace(originalName, _findString, _replaceString); }
                        catch { return originalName; }
                    }
                    var comparison = _matchCase ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase;
                    return originalName.Replace(_findString, _replaceString, comparison);
                case RenameMode.AddAffix:
                    return $"{_prefix}{originalName}{_suffix}";
                case RenameMode.Numbering:
                    string numberFormat = new string('0', _digits);
                    return $"{_baseName}_{(_startIndex + index).ToString(numberFormat)}";
                default:
                    return originalName;
            }
        }
        private void ExecuteRenameAll()
        {
            if (!EditorUtility.DisplayDialog("확인", $"총 {_targetAssets.Count}개 에셋의 이름을 안전하게 변경하시겠습니까?\n(유니티 .meta 파일과 GUID 참조가 유지됩니다)", "실행", "취소"))
                return;
            int changedCount = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < _targetAssets.Count; i++)
                {
                    var asset = _targetAssets[i];
                    if (asset == null) continue;
                    string currentName = asset.name;
                    string newName = CalculateNewName(currentName, i);
                    if (currentName != newName)
                    {
                        string path = AssetDatabase.GetAssetPath(asset);
                        string error = AssetDatabase.RenameAsset(path, newName);
                        if (string.IsNullOrEmpty(error))
                        {
                            changedCount++;
                        }
                        else
                        {
                            Debug.LogError($"[이름 변경 실패] {currentName} -> {newName}: {error}");
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료", $"{changedCount}개의 에셋 이름이 안전하게 변경되었습니다.", "확인");
        }
    }
}



#endif