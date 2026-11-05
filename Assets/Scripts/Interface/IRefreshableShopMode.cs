using System;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 주기적인 마감/갱신 타이머가 존재하는 상점 모드 인터페이스
    /// </summary>
    public interface ITimedShopMode
    {
        public TimeSpan RemainingTime { get; }
        public string TimerPrefix => "재입고";
    }

    /// <summary>
    /// 버튼을 눌러 수동으로 즉시 갱신(리롤)할 수 있는 상점 모드 인터페이스
    /// </summary>
    public interface IRefreshableShopMode : ITimedShopMode
    {
        /// <summary>
        /// 상점 목록 갱신
        /// </summary>
        public void OnRefresh();
    }
}
