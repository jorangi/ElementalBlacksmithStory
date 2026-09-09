using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace ElementalBlacksmithStory.Data
{
    public interface IReadOnlyNPCData
    {
        uint Id {get;}
        string NpcName {get;}
        string NpcFriendlyName {get;}
        IReadOnlyList<SO_DialogueData> Dialogues {get;}
        IReadOnlyList<uint> PreferGift {get;}
    }
    /// <summary>
    /// NPC 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "SO NPCData", menuName = "Scriptable Objects/SO_NPCData")]
    public class SO_NPCData : ScriptableObject, IReadOnlyNPCData
    {
        public uint Id => uint.TryParse(name, out var id) ? id : 0;
        public string npcName;
        public string NpcName => npcName;
        public string npcFriendlyName;
        public string NpcFriendlyName => npcFriendlyName;
        public List<SO_DialogueData> dialogues = new();
        public IReadOnlyList<SO_DialogueData> Dialogues => dialogues;
        public List<uint> preferGift = new();
        public IReadOnlyList<uint> PreferGift => preferGift;
        [TextArea(5, 30)]
        public string memo;

        
        #if UNITY_EDITOR
        [ContextMenu("ID 기반 대사 자동 등록")]
        private void AutoLinkDialogue()
        {
            string npcId = Id.ToString().Substring(2, 3);
            Debug.Log(npcId);
            var setting = AddressableAssetSettingsDefaultObject.Settings;
            if(setting == null)
            {
                Debug.LogError("[SO_DialogueData] AddressablesSettings를 찾을 수 없습니다.");
                return;
            }
            dialogues.Clear();
            string[] guids = AssetDatabase.FindAssets($"6{npcId}* t:SO_DialogueData");

            foreach(var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string cleanedAddress = System.IO.Path.GetFileNameWithoutExtension(path);
                var targetGroup = setting.FindGroup("DialogueData");
                if(targetGroup == null)
                {
                    targetGroup = setting.CreateGroup(
                        "DialogueData",
                        setAsDefaultGroup:false,
                        readOnly:false,
                        postEvent:true,
                        schemasToCopy:setting.DefaultGroup.Schemas
                    );
                }
                var entry = setting.CreateOrMoveEntry(guid, targetGroup);
                if(entry != null)
                {
                    entry.SetAddress(cleanedAddress);
                }
                var dialogue = AssetDatabase.LoadAssetAtPath<SO_DialogueData>(path);
                if(dialogue != null)
                {
                    dialogues.Add(dialogue);
                }
                setting.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(this);
                Debug.Log($"[SO_NPCData] 대사 데이터 {dialogue.Id}를 등록했습니다.");
            }
            Debug.Log($"[SO_NPCData] 총 {dialogues.Count}개의 대사 데이터를 등록했습니다.");
        }
        #endif
    }
}