using UnityEngine;
using ElementalBlacksmithStory.Data;
namespace ElementalBlacksmithStory.Core
{
    public class NPC
    {
        private SO_NPCData _data;
        private uint _affinity;

        public NPC(SO_NPCData data, uint affinity=0)
        {
            _data = data;
            _affinity = affinity;
        }
        public IReadOnlyNPCData GetNPCData()=>_data;
        public void AddAffinity(int val) => _affinity = (uint)Mathf.Clamp(_affinity + val, 0, 100);
    }
}