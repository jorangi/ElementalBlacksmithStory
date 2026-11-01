namespace ElementalBlacksmithStory.Core
{
    /// <summary>
    /// 아이템 및 데이터의 ID 범주 분류
    /// </summary>
    public enum ItemCategory
    {
        Unknown = 0,
        Weapon = 1,    // 무기
        Recipe = 2,    // 레시피
        Material = 3,  // 재료
        Sounds = 4, // 오디오
        NPC = 5,    //NPC 데이터
        Dialogue = 6,  // 600000 ~ 699999 (대사)
        Rune = 7,      //(룬)
    }

    /// <summary>
    /// ID를 분석하여 무할당(Zero-Allocation) 정수 연산으로 카테고리를 판별하는 헬퍼 클래스
    /// </summary>
    public static class ItemIdHelper
    {
        /// <summary>
        /// ID를 기반으로 카테고리를 반환합니다.
        /// </summary>
        public static ItemCategory GetCategory(uint id)
        {
            // 6자리 이상 (현재는 대사 데이터)
            if (id >= 100000)
            {
                return (ItemCategory)(id / 100000);
            }

            // 5자리 아이템 (10,000 ~ 99,999)
            if (id >= 10000)
            {
                return (ItemCategory)(id / 10000);
            }

            return ItemCategory.Unknown;
        }
    }
}
