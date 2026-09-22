using System.Collections.Generic;

namespace ElementalBlacksmithStory.Events
{
    /// <summary>
    /// 상점 대화 시작 이벤트 (순수 데이터 전달 DTO)
    /// </summary>
    public readonly struct StartShopDialogueEvent
    {
        public readonly uint dialogueId;
        public readonly IReadOnlyDictionary<string, object> parameters;

        public StartShopDialogueEvent(uint dialogueId, IReadOnlyDictionary<string, object> parameters = null)
        {
            this.dialogueId = dialogueId;
            this.parameters = parameters;
        }
    }
}
