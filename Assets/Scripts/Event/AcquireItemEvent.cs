using ElementalBlacksmithStory.Core;

namespace ElementalBlacksmithStory.Events
{
    /// <summary>
    /// 아이템(무기, 재료, 룬 등) 획득 통합 이벤트 (무할당 struct)
    /// </summary>
    public readonly struct AcquireItemEvent
    {
        public readonly uint ItemId;
        public readonly uint Amount;
        public readonly string Sender;
        public readonly string Target;
        public readonly string Reason;
        public readonly Weapon Weapon;

        /// <summary>
        /// ID 기반 아이템 획득 (재료, 룬, 또는 기본 무기 등)
        /// </summary>
        public AcquireItemEvent(
            uint itemId,
            uint amount = 1,
            string sender = null,
            string target = null,
            string reason = null)
        {
            ItemId = itemId;
            Amount = amount;
            Sender = sender;
            Target = target;
            Reason = reason;
            Weapon = null;
        }

        /// <summary>
        /// 무기 인스턴스 기반 획득 (모루 기본 무기 지급, 완성된 커스텀 무기 등)
        /// </summary>
        public AcquireItemEvent(
            Weapon weapon,
            string sender = null,
            string target = null,
            string reason = null)
        {
            Weapon = weapon;
            ItemId = weapon?.WeaponId ?? 0;
            Amount = 1;
            Sender = sender;
            Target = target;
            Reason = reason;
        }
    }
}
