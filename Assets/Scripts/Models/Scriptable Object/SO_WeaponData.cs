using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [CreateAssetMenu(fileName = "SO_WeaponData", menuName = "Scriptable Objects/SO_WeaponData")]
    public class SO_WeaponData : BaseMaterialData
    {
        [Header("무기 기본 정보")]
        public string weaponName;
        public float atk;
        public float ats;
        public ulong cost;
        [Header("레시피 목록")]
        public List<SO_CraftRecipe> recipes;
        [Header("외곽선 숨김 여부")]
        public bool hideOutline;
        [Header("속성")]
        public List<ElementType> elementTypes;
        [Header("특성")]
        public List<TraitData> traits;
        [Header("기본 마진 범위")]
        [Range(0f, 99f)]
        public float margin;
        [Header("판매 가격")]
        public ulong basePrice; // 외부에서 책정됨
        public ulong price; // 외부에서 책정됨
        public ulong cumulativeBasePrice;
        public override ulong Value => basePrice;
#if UNITY_EDITOR
        [ContextMenu("SO_WeaponData 자동 등록")]
        public void AddressableRegister()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("$[SO_WeaponData] AddressablesSettings를 찾을 수 없습니다.");
                return;
            }
            var targetGroup = settings.FindGroup("WeaponData");
            if (targetGroup == null)
            {
                targetGroup = settings.CreateGroup(
                    "WeaponData",
                    setAsDefaultGroup: false,
                    readOnly: false,
                    postEvent: true,
                    schemasToCopy: settings.DefaultGroup.Schemas
                );
            }

            string[] guids = AssetDatabase.FindAssets("t:SO_WeaponData");
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string cleanedAddress = Path.GetFileNameWithoutExtension(path);

                    var entry = settings.CreateOrMoveEntry(guid, targetGroup, postEvent: false);
                    entry?.SetAddress(cleanedAddress);

                }
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(this);

                Debug.Log($"[SO_WeaponData] {targetGroup.Name} 그룹에 {guids.Length}개 등록되었습니다.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
#endif
    }
}