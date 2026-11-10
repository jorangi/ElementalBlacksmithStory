using System;
using System.Collections.Generic;
using Cysharp.Text;
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
        private readonly BargainSimulatingService _bargainService;
        private readonly PlayerData _playerData;
        private readonly CompositeDisposable _disposables = new();
        private ulong _offeredPrice;
        private ulong _cachedOfferPrice;
        private IShopItem _itemData;
        private CustomerData _customerData;
        [Inject]
        public BargainPresenter(
            BargainView view,
            TradingPostService tradingPostService,
            PlayerData playerData,
            UIBackPanelPresenter uiBackPanelPresenter,
            WeaponSpriteLoader weaponSpriteLoader,
            MaterialSpriteLoader materialSpriteLoader,
            RuneSpriteLoader runeSpriteLoader,
            BargainSimulatingService bargainService
        )
        {
            _tradingPostService = tradingPostService;
            _bargainService = bargainService;
            _playerData = playerData;
            _uiBackPanelPresenter = uiBackPanelPresenter;
            _view = view;
            _weaponSpriteLoader = weaponSpriteLoader;
            _materialSpriteLoader = materialSpriteLoader;
            _runeSpriteLoader = runeSpriteLoader;

            _view.OnOfferButtonAsObservable()
                .Subscribe(this, static (_, self) =>
                {
                    if (!self._view._canBargain)
                    {
                        self.Hide();
                        return;
                    }
                    if (self._customerData != null && self._offeredPrice < self._customerData.CurrentPrice)
                    {
                        self._view.SetOfferPrice(self._offeredPrice, isLower: true, self._itemData.Count);
                        self.ShowWarning();
                        return;
                    }
                    self._cachedOfferPrice = self._offeredPrice;

                    string dialogue = self._bargainService.TryBargain(self._itemData, self._customerData, self._offeredPrice, out ulong customerPrice);
                    self._view.SetDialogue(dialogue);
                    if (customerPrice == 0)
                    {
                        //결렬
                        self._view.SetInteract(false);
                    }
                    else if (self._cachedOfferPrice == customerPrice)
                    {
                        var (highest, lowest, average) = self._playerData.GetPriceHistory(self._itemData.ItemId);
                        self._view.SetPriceHistory(highest, lowest, average);
                        //성사
                        self._view.SetInteract(false);
                    }
                    else
                    {
                        //역제시
                        self._view.SetInteract(true);
                        self._offeredPrice = customerPrice;
                        self._view.SetOfferPrice(customerPrice, isLower: false, self._itemData.Count);
                    }
                }).AddTo(_disposables);

            //플레이어가 거래 취소
            _view.OnCancelButtonAsObservable()
                .Subscribe(this, static (_, self) =>
                {
                    self._bargainService.CancelBargain(self._itemData.Id, self._customerData);
                    self.Hide();
                }).AddTo(_disposables);
            _view.OnIncreaseButtonAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(this, static (_, self) =>
                {
                    self._offeredPrice = Math.Clamp(self._offeredPrice + self.GetPriceStep(self._offeredPrice), 0, ulong.MaxValue);
                    bool isLower = self._customerData != null && self._offeredPrice < self._customerData.CurrentPrice;
                    self._view.SetOfferPrice(self._offeredPrice, isLower, self._itemData.Count);
                }).AddTo(_disposables);
            _view.OnDecreaseButtonAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(this, static (_, self) =>
                {
                    self._offeredPrice = Math.Clamp(self._offeredPrice - self.GetPriceStep(self._offeredPrice), 0, ulong.MaxValue);
                    bool isLower = self._customerData != null && self._offeredPrice < self._customerData.CurrentPrice;
                    self._view.SetOfferPrice(self._offeredPrice, isLower, self._itemData.Count);
                }).AddTo(_disposables);
            _view.OnChangedOfferPriceAsObservable()
                .Subscribe(this, static (e, self) =>
                {
                    self._offeredPrice = Math.Clamp(e, 0, ulong.MaxValue);
                    bool isLower = self._customerData != null && self._offeredPrice < self._customerData.CurrentPrice;
                    self._view.SetOfferPrice(self._offeredPrice, isLower, self._itemData.Count);
                }).AddTo(_disposables);
            _view.OnWarningAcceptButtonAsObservable()
                .Subscribe(this, static (_, self) =>
                {
                    self._bargainService.SettleBargain(self._itemData, self._customerData, self._offeredPrice);
                    self._view.SetInteract(false);
                    self._view.HideWarning().Forget();
                    self.Hide();
                }).AddTo(_disposables);
            _view.OnWarningCancelButtonAsObservable()
                .Subscribe(this, static (_, self) =>
                {
                    self._view.HideWarning().Forget();
                }).AddTo(_disposables);
        }
        public async UniTask SetData(IShopItem item, CustomerData customer)
        {
            _itemData = item;
            _customerData = customer;
            Sprite sprite = null;

            try
            {
                switch (item.Category)
                {
                    case ShopItemCategory.Sword:
                    case ShopItemCategory.Bow:
                        sprite = await _weaponSpriteLoader.GetSprite(item.ItemId);
                        break;
                    case ShopItemCategory.Material:
                        sprite = await _materialSpriteLoader.GetSprite(item.ItemId);
                        break;
                    case ShopItemCategory.Rune:
                        sprite = await _runeSpriteLoader.GetSprite(item.ItemId);
                        break;
                    default:
                        break;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogWarning($"[BargainPresenter] {e.Message}");
            }
            _view.SetItem(item.Name, sprite, item.Count);
            ulong price = customer.FirstOfferPrice > 0 ? customer.FirstOfferPrice : 0;
            _view.SetOfferPrice(price, false, _itemData.Count);
            _offeredPrice = price;
            _cachedOfferPrice = price;

            string firstOfferDialogue = _bargainService.GetFirstOfferDialogue(item, customer);
            _view.SetDialogue(firstOfferDialogue);
            _view.SetNPCName(ZString.Format("{0} {1}", customer.Group._customerGroupTitle, customer.Name));
            var (highest, lowest, average) = _playerData.GetPriceHistory(item.ItemId);
            _view.SetPriceHistory(highest, lowest, average);
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