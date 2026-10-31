using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.UI.Strategy;
using MessagePipe;
using R3;
using VContainer;
using VContainer.Unity;

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
        private readonly ShopTabPresenter _tabPresenter;
        private readonly CompositeDisposable _disposable = new();
        private readonly IPublisher<StartShopDialogueEvent> _shopDialoguePublisher;
        private MarketType _currentMarketType;

        private readonly StandardShopModeStrategy _standardShopStrategy;
        private readonly BidModeStrategy _bidStrategy;
        private readonly BargainModeStrategy _bargainStrategy;

        private readonly ShopService _shopService;

        [Inject]
        public MarketPresenter(
            MarketView marketView,
            ShopCategoriesPresenter categoriesPresenter,
            ShopItemGridPresenter itemGridPresenter,
            ShopKeeperPresenter shopKeeperPresenter,
            ShopTabPresenter tabPresenter,
            IPublisher<StartShopDialogueEvent> shopDialoguePublisher,
            StandardShopModeStrategy standardShopStrategy,
            BidModeStrategy bidStrategy,
            BargainModeStrategy bargainStrategy,
            ShopService shopService
            )
        {
            _view = marketView;
            _categoriesPresenter = categoriesPresenter;
            _itemGridPresenter = itemGridPresenter;
            _shopKeeperPresenter = shopKeeperPresenter;
            _tabPresenter = tabPresenter;
            _shopDialoguePublisher = shopDialoguePublisher;

            _standardShopStrategy = standardShopStrategy;
            _bidStrategy = bidStrategy;
            _bargainStrategy = bargainStrategy;
            _shopService = shopService;


            _tabPresenter.OnTabSwitchedAsObservable()
                .Subscribe(isSellMode => UpdateShopModeStrategy(isSellMode))
                .AddTo(_disposable);

            _view.OnGeneralShopBtnClickAsObservable
                .Subscribe(_ => OpenShop(MarketType.GENERAL))
                .AddTo(_disposable);

            _view.OnEquipmentShopBtnClickAsObservable
                .Subscribe(_ => OpenShop(MarketType.WEAPON))
                .AddTo(_disposable);

            _view.OnTradingPostBtnClickAsObservable
                .Subscribe(_ => OpenShop(MarketType.TRADINGPOST))
                .AddTo(_disposable);

            _view.OnCloseBtnClickAsObservable
                .Subscribe(_ =>
                {
                    _shopKeeperPresenter.ClearView();
                    _itemGridPresenter.HideShop();
                    _view.Hide();
                })
                .AddTo(_disposable);
        }

        public void Start()
        {
            InitializeShop(MarketType.GENERAL);
            InitializeShop(MarketType.WEAPON);
        }
        private void InitializeShop(MarketType type)
        {
            var shopData = _view.GetShopData(type);
            if (shopData != null)
            {
                _shopService.InitializeShop(shopData);
            }
        }

        /// <summary>
        /// 특정 상점 타입에 맞는 데이터로 세션을 열고 UI를 초기화
        /// </summary>
        private void OpenShop(MarketType type)
        {
            _currentMarketType = type;
            var shopData = _view.GetShopData(type);

            if (type != MarketType.TRADINGPOST && shopData != null)
            {
                _standardShopStrategy.SetShopData(shopData);
            }
            _itemGridPresenter.OpenShop();
            _shopKeeperPresenter.ClearView();
            _view.Show();



            if (shopData != null)
            {
                if (shopData.NpcId > 0)
                {
                    _shopKeeperPresenter.SetNPCAsync(shopData.NpcId).Forget();
                }

                _categoriesPresenter.SetCategories(shopData.Categories);
                _tabPresenter.SetShop(shopData);

                if (shopData.BuyDialogueId > 0)
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
                    _shopDialoguePublisher.Publish(new StartShopDialogueEvent(shopData.BuyDialogueId, parameters));
                }
            }
            UpdateShopModeStrategy(false);
        }

        private void UpdateShopModeStrategy(bool isSellMode)
        {
            if (_currentMarketType == MarketType.TRADINGPOST)
            {
                if (isSellMode && _bargainStrategy != null)
                {
                    _itemGridPresenter.SetInteractionStrategy(_bargainStrategy);
                }
                else
                {
                    _itemGridPresenter.SetInteractionStrategy(_bidStrategy);
                }
            }
            else
            {
                _itemGridPresenter.SetInteractionStrategy(_standardShopStrategy);
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}