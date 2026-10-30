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
                .Subscribe(_ => { Hide(); })
                .AddTo(_disposables);
        }
        public async UniTask SetItem(IShopItem item)
        {
            if (item == null) return;
            Sprite sprite = null;
            switch (ItemIdHelper.GetCategory(item.SpriteId))
            {
                case ItemCategory.Weapon:
                    sprite = await _weaponSpriteLoader.GetSprite(item.SpriteId);
                    break;
                case ItemCategory.Material:
                    sprite = await _materialSpriteLoader.GetSprite(item.SpriteId);
                    break;
                case ItemCategory.Rune:
                    sprite = await _runeSpriteLoader.GetSprite(item.SpriteId);
                    break;
                default:
                    break;
            }
            _view.SetItem(item.Name, sprite, item.Count);
        }
        public void Show()
        {
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

        }
        public void Dispose()
        {
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}