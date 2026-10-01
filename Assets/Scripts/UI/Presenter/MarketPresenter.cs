using System;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using R3;
using MessagePipe;
using VContainer;
using VContainer.Unity;
using System.Collections.Generic;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 상점가 진입 버튼 및 상점 세션 라이프사이클 제어 Presenter
    /// </summary>
    public class MarketPresenter : IStartable, IDisposable
    {
        private readonly MarketView _view;
        private readonly ShopCategoriesPresenter _categoriesPresenter;
        private readonly ShopItemGridPresenter _itemGridPresenter;
        private readonly ShopKeeperPresenter _shopKeeperPresenter;
        private readonly CompositeDisposable _disposable = new();
        IPublisher<StartShopDialogueEvent> _shopDialoguePublisher;

        [Inject]
        public MarketPresenter(
            MarketView marketView,
            ShopCategoriesPresenter categoriesPresenter,
            ShopItemGridPresenter itemGridPresenter,
            ShopKeeperPresenter shopKeeperPresenter,
            IPublisher<StartShopDialogueEvent> shopDialoguePublisher
            )
        {
            _view = marketView;
            _categoriesPresenter = categoriesPresenter;
            _itemGridPresenter = itemGridPresenter;
            _shopKeeperPresenter = shopKeeperPresenter;
            _shopDialoguePublisher = shopDialoguePublisher;

            _view.OnGeneralShopBtnClickAsObservable
                .Subscribe(_ => OpenShop(MarketType.GENERAL))
                .AddTo(_disposable);

            _view.OnEquipmentShopBtnClickAsObservable
                .Subscribe(_ => OpenShop(MarketType.WEAPON))
                .AddTo(_disposable);

            _view.OnAuctionBtnClickAsObservable
                .Subscribe(_ =>
                {
                    // TODO: 경매장 세션은 아마 따로 팔 듯
                })
                .AddTo(_disposable);

            _view.OnCloseBtnClickAsObservable
                .Subscribe(_ =>
                {
                    _shopKeeperPresenter.ClearView();
                    _view.Hide();
                })
                .AddTo(_disposable);
        }

        public void Start() { }

        /// <summary>
        /// 특정 상점 타입에 맞는 데이터로 세션을 열고 UI를 초기화
        /// </summary>
        private void OpenShop(MarketType type)
        {
            var shopData = _view.GetShopData(type);
            _shopKeeperPresenter.ClearView();
            _view.Show(type);

            if (shopData != null)
            {
                if (shopData.NpcId > 0)
                {
                    _shopKeeperPresenter.SetNPCAsync(shopData.NpcId).Forget();
                }

                _categoriesPresenter.SetCategories(shopData.Categories);
                _itemGridPresenter.SetShopGoods(shopData);

                if (shopData.GreetingDialogueId > 0)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "itemCount", 0 },
                        { "_cartItems.Count", 0 },
                        { "totalAmount", 0 },
                        { "totalCost", "0 골드" },
                        { "totalPrice", "0 골드" },
                        { "isSellMode", false }
                    };
                    _shopDialoguePublisher.Publish(new StartShopDialogueEvent(shopData.GreetingDialogueId, parameters));
                }
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}