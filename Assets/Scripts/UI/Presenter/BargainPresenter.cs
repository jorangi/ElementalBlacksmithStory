using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.UI
{
    public class BargainPresenter : IDisposable
    {
        private readonly BargainView _view;
        private readonly WeaponSpriteLoader _weaponSpriteLoader;
        private readonly MaterialSpriteLoader _materialSpriteLoader;
        private readonly RuneSpriteLoader _runeSpriteLoader;
        private readonly UIBackPanelPresenter _uiBackPanelPresenter;
        private readonly TradingPostService _tradingPostService;
        private readonly PlayerData _playerData;
        private readonly CompositeDisposable _disposables = new();
        private ulong _offeredPrice;
        private ulong _cachedOfferPrice;
        [Inject]
        public BargainPresenter(
            BargainView view,
            TradingPostService service,
            PlayerData playerData,
            UIBackPanelPresenter uiBackPanelPresenter,
            WeaponSpriteLoader weaponSpriteLoader,
            MaterialSpriteLoader materialSpriteLoader,
            RuneSpriteLoader runeSpriteLoader
        )
        {
            _tradingPostService = service;
            _playerData = playerData;
            _uiBackPanelPresenter = uiBackPanelPresenter;
            _view = view;
            _weaponSpriteLoader = weaponSpriteLoader;
            _materialSpriteLoader = materialSpriteLoader;
            _runeSpriteLoader = runeSpriteLoader;

            _view.OnOfferButtonAsObservable()
                .Subscribe(_ =>
                {
                    //TODO: `TradingPostService`에 연결하여 제시가에 따른 흥정 로직 추가
                    _cachedOfferPrice = _offeredPrice;
                }).AddTo(_disposables);
            _view.OnCancelButtonAsObservable()
        .Subscribe(_ =>
                {
                    //TODO: `TradingPostService`에 연결하여 흥정 종료 처리
                    Hide();
                }).AddTo(_disposables);
            _view.OnIncreaseButtonAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(_ =>
                {
                    _offeredPrice = Math.Clamp(_offeredPrice + GetPriceStep(_offeredPrice), 0, ulong.MaxValue);
                    bool isLower = _cachedOfferPrice > _offeredPrice;
                    _view.SetOfferPrice(_offeredPrice, isLower);
                }).AddTo(_disposables);
            _view.OnDecreaseButtonAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(_ =>
                {
                    _offeredPrice = Math.Clamp(_offeredPrice - GetPriceStep(_offeredPrice), 0, ulong.MaxValue);
                    bool isLower = _cachedOfferPrice > _offeredPrice;
                    _view.SetOfferPrice(_offeredPrice, isLower);
                }).AddTo(_disposables);
            _view.OnChangedOfferPriceAsObservable()
                .Subscribe(e =>
                {
                    _offeredPrice = Math.Clamp(e, 0, ulong.MaxValue);
                    bool isLower = _cachedOfferPrice > _offeredPrice;
                    _view.SetOfferPrice(_offeredPrice, isLower);
                }).AddTo(_disposables);
            _view.OnWarningAcceptButtonAsObservable()
                .Subscribe(_ =>
                {
                    //TradingPostService를 통해 인벤토리에서 아이템 제거
                    _playerData.AddMoney(_offeredPrice);
                    _view.HideWarning().Forget();
                }).AddTo(_disposables);
            _view.OnWarningCancelButtonAsObservable()
                .Subscribe(_ =>
                {
                    _view.HideWarning().Forget();
                }).AddTo(_disposables);
        }
        public ulong GetPriceStep(ulong price)
        {
            if (price < 10) return 1;

            int power = (int)Math.Log10(price);
            return (ulong)Math.Pow(10, Math.Max(0, power - 1));
        }
        /// <summary>
        /// 패널을 표시하고 흥정 UI 활성화
        /// </summary>
        public void Show()
        {
            _uiBackPanelPresenter.Show();
            _view.Show().Forget();
        }
        /// <summary>
        /// 패널을 숨기고 흥정 UI 비활성화
        /// </summary>
        public void Hide()
        {
            _uiBackPanelPresenter.Hide();
            _view.Hide().Forget();
        }
        /// <summary>
        /// 낮은 가격을 제시했을 때 경고창 띄우기
        /// </summary>
        public void ShowWarning()
        {
            _view.ShowWarning().Forget();
        }
        /// <summary>
        /// 낮은 가격 제시 경고창 닫기
        /// </summary>
        public void HideWarning()
        {
            _view.HideWarning().Forget();
        }
        public void Dispose()
        {
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}