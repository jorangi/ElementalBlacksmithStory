using System.Collections.Generic;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    public abstract class SO_BaseDatabase<T> : ScriptableObject, IDatabase where T : ScriptableObject, IIdentifiable
    {
        [SerializeField] protected List<T> _itemList = new();
        private Dictionary<uint, T> _itemDict = new();
        public void Init()
        {
            _itemDict = new();
            foreach (var i in _itemList)
            {
                if (i == null)
                {
                    Debug.LogWarning($"[SO_BaseDatabase<{typeof(T).Name}>] 리스트에 null이 포함되어 있습니다.");
                    continue;
                }
                if (!_itemDict.ContainsKey(i.Id))
                {
                    _itemDict.Add(i.Id, i);
                }
                else
                {
                    Debug.LogWarning($"[SO_BaseDatabase<{typeof(T).Name}>] Id가 {i.Id}인 데이터가 이미 딕셔너리에 포함되어 있습니다.");
                }
            }
        }
        public T Get(uint id) => _itemDict.GetValueOrDefault(id);

#if UNITY_EDITOR
        protected virtual string AddressableGroupName => typeof(T).Name.Replace("SO_", "");

        [ContextMenu("데이터 자동 등록")]
        public virtual void AutoRegister()
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError($"[{name}] AddressablesSettings를 찾을 수 없습니다.");
                return;
            }

            var targetGroup = settings.FindGroup(AddressableGroupName);
            if (targetGroup == null)
            {
                targetGroup = settings.CreateGroup(
                    AddressableGroupName,
                    setAsDefaultGroup: false,
                    readOnly: false,
                    postEvent: true,
                    schemasToCopy: settings.DefaultGroup.Schemas
                );
            }

            _itemList.Clear();
            string typeName = typeof(T).Name;
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeName}");

            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string cleanedAddress = System.IO.Path.GetFileNameWithoutExtension(path);

                var entry = settings.CreateOrMoveEntry(guid, targetGroup);
                if (entry != null)
                {
                    entry.SetAddress(cleanedAddress);
                }

                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                if (item != null)
                {
                    _itemList.Add(item);
                }
            }

            settings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{name}] 총 {_itemList.Count}개의 {typeName} 데이터를 Addressables [{AddressableGroupName}] 그룹에 등록했습니다.");
        }
#endif
    }
}