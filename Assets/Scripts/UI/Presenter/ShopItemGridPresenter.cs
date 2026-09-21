using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Inventory;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class ShopItemGridPresenter : IAsyncStartable, IDisposable
    {
        private readonly ShopItemGridView _view;
        private readonly WeaponSpriteLoader _weaponSpriteLoader;
        private readonly MaterialSpriteLoader _materialSpriteLoader;
        private readonly MaterialInventory _materialInventory;
        private readonly SO_MaterialDatabase _materialDatabase;
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly ShopCartPresenter _shopCartPresenter;
        private readonly Dictionary<uint, ShopItemView> _displayItems = new();
        private readonly Dictionary<uint, IDisposable> _itemDisposables = new();
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public ShopItemGridPresenter(
            ShopItemGridView view,
            WeaponSpriteLoader weaponSpriteLoader,
            MaterialSpriteLoader materialSpriteLoader,
            MaterialInventory materialInventory,
            SO_MaterialDatabase materialDatabase,
            SO_WeaponDatabase weaponDatabase,
            ShopCartPresenter shopCartPresenter
        )
        {
            _view = view;
            _weaponSpriteLoader = weaponSpriteLoader;
            _materialSpriteLoader = materialSpriteLoader;
            _materialInventory = materialInventory;
            _materialDatabase = materialDatabase;
            _weaponDatabase = weaponDatabase;
            _shopCartPresenter = shopCartPresenter;

            // 인벤토리 수량 변경 시 화면의 슬롯 보유 수량 실시간 갱신
            _materialInventory.OnItemCountChangedAsObservable
                .Subscribe(data =>
                {
                    if (data.material != null && _displayItems.TryGetValue(data.material.Id, out var viewItem))
                    {
                        viewItem.SetAmountInPocket(data.count);
                    }
                })
                .AddTo(_disposables);
        }

        public async UniTask StartAsync(CancellationToken ct = default)
        {
            // 테스트용 아이템 등록
            if (_materialDatabase != null)
            {
                var mat30001 = _materialDatabase.GetMaterial(30001);
                if (mat30001 != null)
                {
                    await AddItem(new MaterialShopItem(mat30001, 5));
                }

                var mat30002 = _materialDatabase.GetMaterial(30002);
                if (mat30002 != null)
                {
                    await AddItem(new MaterialShopItem(mat30002, 10));
                }
            }

            if (_weaponDatabase != null)
            {
                var wep10001 = _weaponDatabase.GetWeapon(10001);
                if (wep10001 != null)
                {
                    var testWeapon = new Weapon(wep10001);
                    testWeapon.SetMargin(1.2f);
                    await AddItem(testWeapon);
                }
            }
        }

        public async UniTask AddItem(IShopItem item)
        {
            Sprite icon = null;
            bool isUniqueItem = false;
            switch (item.SpriteId.ToString()[0])
            {
                case '1':
                    icon = await _weaponSpriteLoader.GetSprite(item.SpriteId);
                    isUniqueItem = true;
                    break;
                case '3':
                    icon = await _materialSpriteLoader.GetSprite(item.SpriteId);
                    isUniqueItem = false;
                    break;
                default:
                    Debug.LogError($"[ShopItemGridPresenter]: {item.SpriteId}은 유효한 Sprite ID가 아닙니다.");
                    return;
            }

            ShopItemView itemView = _view.CreateItem(item.Id, item.Name, item.Price, icon, isUniqueItem);
            if (itemView == null) return;

            if (_itemDisposables.TryGetValue(item.Id, out var prevDisp))
            {
                prevDisp.Dispose();
                _itemDisposables.Remove(item.Id);
            }

            var clickSub = itemView.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    _shopCartPresenter.OpenAmountModal(item).Forget();
                });
            _itemDisposables[item.Id] = clickSub;

            if (!isUniqueItem && item is MaterialShopItem materialItem && materialItem.Data != null)
            {
                uint currentCount = _materialInventory.GetCount(materialItem.Data);
                itemView.SetAmountInPocket(currentCount);
            }

            _displayItems[item.Id] = itemView;
        }

        public void RemoveItem(uint id)
        {
            if (_itemDisposables.TryGetValue(id, out var disp))
            {
                disp.Dispose();
                _itemDisposables.Remove(id);
            }

            _displayItems.Remove(id);
            _view.RemoveItem(id);
        }

        public void ClearItems()
        {
            foreach (var disp in _itemDisposables.Values)
            {
                disp.Dispose();
            }
            _itemDisposables.Clear();

            _displayItems.Clear();
            _view.Clear();
        }

        public void Dispose()
        {
            ClearItems();
            _disposables.Dispose();
        }
    }
}