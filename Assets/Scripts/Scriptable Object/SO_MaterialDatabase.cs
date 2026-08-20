using UnityEngine;
using System.Collections.Generic;

namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_MaterialDatabase", menuName = "Scriptable Objects/SO_MaterialDatabase")]
    public class SO_MaterialDatabase : ScriptableObject
    {
        [SerializeField] List<SO_MaterialData> materialList = new();
        private Dictionary<uint, SO_MaterialData> _materialDict = new();
        public void Init()
        {
            _materialDict = new();
            foreach(var w in materialList)
            {
                if (w == null)
                {
                    Debug.LogWarning("[SO_MaterialDatabase] materialList에 null 항목이 포함되어 있습니다.");
                    continue;
                }
                if (!_materialDict.ContainsKey(w.Id))
                {
                    _materialDict.Add(w.Id, w);
                }
                else
                {
                    Debug.LogWarning($"[SO_MaterialDatabase] ID {w.Id} ({w.materialName})가 이미 딕셔너리에 존재합니다.");
                }
            }
            Debug.Log($"[SO_MaterialDatabase] 총 {_materialDict.Count}개의 재료 데이터를 등록했습니다.");
        }
        public SO_MaterialData GetMaterial(uint id)
        {
            if(_materialDict.ContainsKey(id))
            {
                return _materialDict[id];
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[SO_MaterialDatabase] {id}가 존재하지 않습니다.");
                return null;
            }
        }
        #if UNITY_EDITOR
        [ContextMenu("SO_MaterialData 자동 등록 및 주소(ID) 정리")]
        private void AutoRegisterMaterialData()
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if(settings == null)
            {
                Debug.LogError("[SO_MaterialDatabase] AddressablesSettings를 찾을 수 없습니다.");
                return;
            }
            materialList.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SO_MaterialData");
            foreach(var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string cleanedAddress = System.IO.Path.GetFileNameWithoutExtension(path);
                var targetGroup = settings.FindGroup("MaterialData");
                if(targetGroup == null)
                {
                    targetGroup = settings.CreateGroup(
                        "MaterialData",
                        setAsDefaultGroup: false,
                        readOnly: false,
                        postEvent: true,
                        schemasToCopy: settings.DefaultGroup.Schemas
                    );
                }
                var entry = settings.CreateOrMoveEntry(guid, targetGroup);
                if (entry != null)
                {
                    entry.SetAddress(cleanedAddress);
                }
                var material = UnityEditor.AssetDatabase.LoadAssetAtPath<SO_MaterialData>(path);
                if(material != null)
                {
                    materialList.Add(material);
                }
            }
            settings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SO_MaterialDatabase] 총 {materialList.Count}개의 무기 데이터를 자동 등록하고 Addressables 주소를 정리했습니다.");
        }
        #endif
    }
}
