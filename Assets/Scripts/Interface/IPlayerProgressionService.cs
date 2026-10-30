namespace ElementalBlacksmithStory.Core
{
    /// <summary>
    /// 플레이어의 종합 진행도 점수를 ulong 정수로 산출하고 구간 기준점과 비교하는 서비스 인터페이스
    /// </summary>
    public interface IPlayerProgressionService
    {
        /// <summary>
        /// 중반부 진입 기준 점수
        /// </summary>
        ulong MidGameScoreThreshold { get; }

        /// <summary>
        /// 극후반부 진입 기준 점수
        /// </summary>
        ulong EndGameScoreThreshold { get; }

        /// <summary>
        /// 재력, 장비, 룬, 재료 점수를 합산한 플레이어의 총 진행도 점수(ulong, 최소 단위 1점)를 반환합니다.
        /// </summary>
        ulong GetTotalProgressionScore();

        /// <summary>
        /// 총 진행도 점수를 중반부(0.5) 및 극후반부(1.0) 기준점에 맞춰 매핑한 진행 비율(0.0 ~ 1.0)을 반환합니다.
        /// </summary>
        float GetProgressionRatio();
    }
}
