using System;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.UI
{
    public class CustomerListPresenter : IDisposable
    {
        private readonly CustomerListView _view;
        private readonly TradingPostService _service;
        private readonly UIBackPanelPresenter _uiBackPanelPresenter;
        private readonly WeaponSpriteLoader _weaponSpriteLoader;
        private readonly MaterialSpriteLoader _materialSpriteLoader;
        private readonly RuneSpriteLoader _runeSpriteLoader;
        private readonly CompositeDisposable _disposables = new();
        private IShopItem _currentItem;

        [Inject]
        public CustomerListPresenter(
            CustomerListView view,
            UIBackPanelPresenter uiBackPanelPresenter,
            TradingPostService service,
            WeaponSpriteLoader weaponSpriteLoader,
            MaterialSpriteLoader materialSpriteLoader,
            RuneSpriteLoader runeSpriteLoader
        )
        {
            _view = view;
            _uiBackPanelPresenter = uiBackPanelPresenter;
            _service = service;
            _weaponSpriteLoader = weaponSpriteLoader;
            _materialSpriteLoader = materialSpriteLoader;
            _runeSpriteLoader = runeSpriteLoader;

            _view.OnCloseButtonAsObservable()
                .Subscribe(this, static (_, self) => { self.Hide(); })
                .AddTo(_disposables);


            _service.OnCustomerArrived.
                Subscribe(this, static (e, self) =>
                {
                    if (self._currentItem != null && self._currentItem.Id == e.itemId)
                    {
                        self._view.Create(e.customer);
                    }
                }).AddTo(_disposables);

            _service.OnCustomerLeft
                .Subscribe(this, static (e, self) =>
                {
                    if (self._currentItem != null && self._currentItem.Id == e.itemId)
                    {
                        self._view.Remove(e.customer);
                    }
                }).AddTo(_disposables);
        }
        public async UniTask SetItem(IShopItem item)
        {
            if (item == null) return;
            _currentItem = item;
            Sprite sprite = null;
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
            _view.SetItem(item.Name, sprite, item.Count);
            RefreshList();
        }
        public void Show()
        {
            RefreshList();
            _uiBackPanelPresenter.Show();
            _view.Show().Forget();
        }
        public void Hide()
        {
            _uiBackPanelPresenter.Hide();
            _view.Hide().Forget();
        }
        public void RefreshList()
        {
            _view.Clear();
            if (_currentItem == null || _service == null) return;

            var customers = _service.GetBargainCustomers(_currentItem.Id);
            _view.ShowNoListAlert(customers.Count == 0);
            for (int i = 0; i < customers.Count; i++)
            {
                _view.Create(customers[i]);
            }
        }
        public void RemoveCustomer(CustomerData customer)
        {
            if (_currentItem == null || customer == null || _service == null) return;
            _service.RemoveBargainCustomer(_currentItem.Id, customer);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}