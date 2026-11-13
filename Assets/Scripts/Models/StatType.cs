namespace ElementalBlacksmithStory.Data
{
    public enum StatType
    {
        HP, //체력 (단, 0이 되더라도 던전 내에서 회복함-대신 시간이 소모됨)
        ATK, //공격력
        ATS, //공격속도
        SPD, //이동속도(던전을 빨리 이동할 수 있음)
        DEF, //방어력
        SELLBONUS, //판매 보너스
        GOLDBONUS, //골드 보너스
        MAG, //마력
    }
    [System.Serializable]
    public struct BaseStat
    {
        public StatType StatType;
        public float Value;
    }
}