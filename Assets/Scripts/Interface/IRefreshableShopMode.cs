using System;

namespace ElementalBlacksmithStory.UI
{
    public interface IRefreshableShopMode
    {
        /// <summary>
        /// 상점 목록 갱신
        /// </summary>
        public void OnRefresh();
        public TimeSpan RemainingTime { get; }
    }
}