using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.UI.Strategy
{
    public interface IShopModeStrategy
    {
        /// <summary>
        /// 상점 모드에 맞는 진열 아이템 목록을 반환
        /// </summary>
        /// <param name="isSellMode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        UniTask GetDisplayItemAsync(List<IShopItem> results, bool isSellMode, CancellationToken ct = default);
        /// <summary>
        /// 해당 아이템의 그리드 표시 가격(상점 원가, 경매 시작가/최고가 등)을 반환
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public ulong GetPrice(IShopItem item);

        /// <summary>
        /// 해당 아이템을 상호작용(클릭) 가능한 상태인지
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CanInteract(IShopItem item);
        /// <summary>
        /// 해당 아이템을 클릭 했을 때
        /// </summary>
        /// <param name="item"></param>
        public void OnItemClicked(IShopItem item);
        /// <summary>
        /// 흥정(Bargain) 전용 카드 프리팹을 사용할지 여부
        /// </summary>
        public bool IsBargainCard => false;

        /// <summary>
        /// 아이템의 묶음(번들) 판매 단위 수량을 반환합니다.
        /// 단순 재고/소지량(Amount)이 아닌 고정 묶음 거래(Bundle)일 때만 2 이상을 반환합니다.
        /// </summary>
        public uint GetBundleCount(IShopItem item) => 1;
    }
}