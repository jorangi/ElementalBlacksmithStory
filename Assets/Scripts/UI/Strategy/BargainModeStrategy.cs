using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Inventory;
using ElementalBlacksmithStory.UI;
using VContainer;

namespace ElementalBlacksmithStory.UI.Strategy
{
    /// <summary>
    /// 교역소(TradingPost) 판매 모드(흥정 출품 및 손님 대기열)의 아이템 상호작용 전략
    /// </summary>
    public class BargainModeStrategy : IShopModeStrategy, IItemRegisterableShopMode
    {
        private readonly CustomerListPresenter _customerListPresenter;
        private readonly TradingPostService _tradingPostService;
        private readonly PlayerData _playerData;
        public bool CanRefresh => false;

        [Inject]
        public BargainModeStrategy(
            CustomerListPresenter customerListPresenter,
            PlayerData playerData,
            TradingPostService tradingPostService = null
        )
        {
            _customerListPresenter = customerListPresenter;
            _playerData = playerData;
            _tradingPostService = tradingPostService;
        }

        public bool IsBargainCard => true;

        /// <summary>
        /// 해당 물품의 가격 대신 현재 거래 희망을 건 손님들의 수를 반환
        /// </summary>
        public ulong GetPrice(IShopItem item)
        {
            if (item == null) return 0;

            return GetCustomerCount(item);
        }

        /// <summary>
        /// 해당 물품을 상호작용(클릭) 가능한 상태인지 여부
        /// </summary>
        public bool CanInteract(IShopItem item)
        {
            return item != null;
        }

        /// <summary>
        /// 아이템 클릭 시 해당 물품에 거래 희망을 건 손님 목록(CustomerListView) 오픈
        /// </summary>
        public void OnItemClicked(IShopItem item)
        {
            if (item == null || _customerListPresenter == null) return;
            _customerListPresenter.SetItem(item).Forget();
            _customerListPresenter.Show();
        }

        /// <summary>
        /// 판매 등록(+ 버튼) 카드 클릭 시 호출
        /// </summary>
        public void OnRegisterCardClicked()
        {
            // TODO: 판매 등록 모달/인벤토리 열기
            UnityEngine.Debug.Log("[BargainModeStrategy] 판매 등록(+ 카드) 클릭됨");
        }

        /// <summary>
        /// 특정 물품에 거래 희망을 건 손님 수 반환
        /// </summary>
        private ulong GetCustomerCount(IShopItem item)
        {
            if (item == null || _tradingPostService == null) return 0;

            // TODO: TradingPostService에 추가될 출품 물품별 손님 컬렉션(CustomerData 목록)과 연동
            return 0;
        }

        public UniTask GetDisplayItemAsync(List<IShopItem> results, bool isSellMode, CancellationToken ct = default)
        {
            return UniTask.CompletedTask;
        }
    }
}
