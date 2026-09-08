using UnityEngine;
using System.Collections.Generic;

namespace ElementalBlacksmithStory.Data
{
    [System.Serializable]
    public struct DialogueLine
    {
        public uint speakerId;
        [TextArea(2, 5)]
        public string text;
    }
    /// <summary>
    /// NPC 대화 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "SO DialogueData", menuName = "Scriptable Objects/SO_DialogueData")]
    public class SO_DialogueData : ScriptableObject
    {
        public uint Id => uint.TryParse(name, out var id) ? id : 0;
        [TextArea(2, 5)]
        public string description;
        public List<DialogueLine> contents = new();
    }
}