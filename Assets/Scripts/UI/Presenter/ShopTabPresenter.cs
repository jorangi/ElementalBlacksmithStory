using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 상점 구매/판매 탭 전환 제어 Presenter
    /// </summary>
    public class ShopTabPresenter : IStartable, IDisposable
    {
        private readonly ShopTabView _view;
        private readonly ShopCartPresenter _cartPresenter;
        private readonly ShopItemGridPresenter _gridPresenter;
        private readonly IPublisher<PlaySoundEvent> _soundPublisher;
        private readonly IPublisher<StartShopDialogueEvent> _shopDialoguePublisher;
        private readonly CompositeDisposable _disposables = new();

        private bool _isSellMode = false;
        public bool IsSellMode => _isSellMode;

        [Inject]
        public ShopTabPresenter(
            ShopTabView view,
            ShopCartPresenter cartPresenter,
            ShopItemGridPresenter gridPresenter,
            IPublisher<PlaySoundEvent> soundPublisher,
            IPublisher<StartShopDialogueEvent> shopDialoguePublisher)
        {
            _view = view;
            _cartPresenter = cartPresenter;
            _gridPresenter = gridPresenter;
            _soundPublisher = soundPublisher;
            _shopDialoguePublisher = shopDialoguePublisher;
        }

        public void Start()
        {
            if (_view == null) return;

            // 초기 상태: 구매 탭 활성화 및 첫 입장 대사 발행
            _isSellMode = false;
            _view.SetTabVisual(_isSellMode);
            _cartPresenter.SetSellMode(_isSellMode);
            _gridPresenter.SetSellMode(_isSellMode);
            PublishTabDialogue(_isSellMode);

            _view.OnClickBuyTabAsObservable()
                .Subscribe(_ => SwitchTab(isSellMode: false))
                .AddTo(_disposables);

            _view.OnClickSellTabAsObservable()
                .Subscribe(_ => SwitchTab(isSellMode: true))
                .AddTo(_disposables);
        }

        /// <summary>
        /// 탭 전환 처리
        /// </summary>
        public void SwitchTab(bool isSellMode)
        {
            if (_isSellMode == isSellMode) return;

            _isSellMode = isSellMode;
            _view.SetTabVisual(_isSellMode);
            _cartPresenter.SetSellMode(_isSellMode);
            _gridPresenter.SetSellMode(_isSellMode);
            _soundPublisher?.Publish(new PlaySoundEvent(40106));

            // 탭 전환 시 해당 탭(구매/판매)의 첫 입장 대사 발행
            PublishTabDialogue(_isSellMode);
        }

        private void PublishTabDialogue(bool isSellMode)
        {
            uint dialogueId = isSellMode ? _view.SellDialogueId : _view.BuyDialogueId;
            var parameters = new Dictionary<string, object>
            {
                { "itemCount", 0 },
                { "_cartItems.Count", 0 },
                { "totalAmount", 0 },
                { "totalCost", "0 골드" },
                { "totalPrice", "0 골드" },
                { "isSellMode", isSellMode }
            };

            _shopDialoguePublisher.Publish(new StartShopDialogueEvent(dialogueId, parameters));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
