using System.Collections.Generic;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Events
{
    /// <summary>
    /// 재료 선택 UI에 비움 및 선택 상태 반영을 요청하는 이벤트
    /// </summary>
    public readonly struct ChangeSelectedMaterialsEvent
    {
        public readonly bool ClearMaterials;
        public readonly IReadOnlyDictionary<uint, uint> SelectedMaterials;

        public ChangeSelectedMaterialsEvent(
            bool clearMaterials = true, 
            IReadOnlyDictionary<uint, uint> selectedMaterials = null)
        {
            ClearMaterials = clearMaterials;
            SelectedMaterials = selectedMaterials;
        }

        public ChangeSelectedMaterialsEvent(
            bool clearMaterials,
            IEnumerable<KeyValuePair<BaseMaterialData, uint>> selectedMaterials)
        {
            ClearMaterials = clearMaterials;
            if (selectedMaterials != null)
            {
                var dict = new Dictionary<uint, uint>();
                foreach (var kv in selectedMaterials)
                {
                    if (kv.Key != null)
                    {
                        dict[kv.Key.Id] = kv.Value;
                    }
                }
                SelectedMaterials = dict;
            }
            else
            {
                SelectedMaterials = null;
            }
        }
    }
}
