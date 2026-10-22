namespace ElementalBlacksmithStory.Effect
{
    /// <summary>
    /// 장비/아이템에 부여되는 효과의 최상위 인터페이스
    /// </summary>
    public interface IEffect
    {
        /// <summary>
        /// 해당 효과에 대한 설명
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 장착 시 호출
        /// </summary>
        public void OnEquip();

        /// <summary>
        /// 해제 시 호출
        /// </summary>
        public void OnUnequip();
    }
}