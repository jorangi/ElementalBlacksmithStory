using System.Collections.Generic;

namespace ElementalBlacksmithStory.Events
{
    /// <summary>
    /// 대화 시작 이벤트 (순수 데이터 전달 DTO)
    /// </summary>
    public readonly struct StartDialogueEvent
    {
        public readonly uint dialogueId;
        public readonly IReadOnlyDictionary<string, object> parameters;

        public StartDialogueEvent(uint dialogueId, IReadOnlyDictionary<string, object> parameters = null)
        {
            this.dialogueId = dialogueId;
            this.parameters = parameters;
        }
    }
}