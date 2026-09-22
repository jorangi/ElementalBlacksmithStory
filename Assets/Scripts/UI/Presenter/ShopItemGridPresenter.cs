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

        private readonly List<IShopItem> _shopBuyGoods = new();
        private bool _isSellMode = false;
        private bool _isInitialized = false;
        private CancellationTokenSource _refreshCts;
        private Func<IShopItem, bool> _categoryFilter = _ => true;

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

            // 인벤토리 수량 변경 시 실시간 그리드 갱신
            _materialInventory.OnItemCountChangedAsObservable
                .Subscribe(data =>
                {
                    if (data.material == null) return;

                    if (_isSellMode)
                    {
                        // 판매 모드: 수량이 0이면 슬롯 제거, 0보다 크면 수량 갱신 또는 신규 등록
                        if (data.count == 0)
                        {
                            RemoveItem(data.material.Id);
                        }
                        else if (_displayItems.TryGetValue(data.material.Id, out var viewItem))
                        {
                            viewItem.SetAmountInPocket(data.count);
                        }
                        else
                        {
                            var item = new MaterialShopItem(data.material, data.count);
                            if (_categoryFilter == null || _categoryFilter(item))
                            {
                                AddItem(item).Forget();
                            }
                        }
                    }
                    else
                    {
                        // 구매 모드: 해당 아이템이 상점에 진열되어 있다면 인벤 보유량 텍스트만 갱신
                        if (_displayItems.TryGetValue(data.material.Id, out var viewItem))
                        {
                            viewItem.SetAmountInPocket(data.count);
                        }
                    }
                })
                .AddTo(_disposables);
        }

        public async UniTask StartAsync(CancellationToken ct = default)
        {
            InitializeShopGoods();
            _isInitialized = true;
            await RefreshGridAsync();
        }

        /// <summary>
        /// 상점 기본 판매 물품(구매 탭용) 초기화
        /// </summary>
        private void InitializeShopGoods()
        {
            if (_shopBuyGoods.Count > 0) return;

            if (_materialDatabase != null)
            {
                var mat30001 = _materialDatabase.GetMaterial(30001);
                if (mat30001 != null)
                {
                    _shopBuyGoods.Add(new MaterialShopItem(mat30001, 5));
                }

                var mat30002 = _materialDatabase.GetMaterial(30002);
                if (mat30002 != null)
                {
                    _shopBuyGoods.Add(new MaterialShopItem(mat30002, 10));
                }
            }

            if (_weaponDatabase != null)
            {
                var wep10001 = _weaponDatabase.GetWeapon(10001);
                if (wep10001 != null)
                {
                    var testWeapon = new Weapon(wep10001);
                    testWeapon.SetMargin(1.2f);
                    _shopBuyGoods.Add(testWeapon);
                }
            }
        }

        /// <summary>
        /// 구매/판매 모드 전환
        /// </summary>
        public void SetSellMode(bool isSellMode)
        {
            if (_isInitialized && _isSellMode == isSellMode) return;

            _isSellMode = isSellMode;
            if (_isInitialized)
            {
                RefreshGridAsync().Forget();
            }
        }

        /// <summary>
        /// 카테고리 필터 조건 변경 및 그리드 갱신
        /// </summary>
        public void SetCategoryFilter(Func<IShopItem, bool> filterPredicate)
        {
            _categoryFilter = filterPredicate ?? (_ => true);
            if (_isInitialized)
            {
                RefreshGridAsync().Forget();
            }
        }

        /// <summary>
        /// 그리드 아이템 목록 갱신
        /// </summary>
        public async UniTask RefreshGridAsync()
        {
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshCts = new CancellationTokenSource();
            var ct = _refreshCts.Token;

            ClearItems();

            try
            {
                if (_isSellMode)
                {
                    await PopulateSellItemsAsync(ct);
                }
                else
                {
                    await PopulateBuyItemsAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // 빠른 탭 전환 등으로 취소된 경우 무시
            }
        }

        /// <summary>
        /// 구매 탭 아이템 목록 생성 (상점 판매 상품 중 현재 카테고리 필터를 만족하는 항목)
        /// </summary>
        private async UniTask PopulateBuyItemsAsync(CancellationToken ct)
        {
            foreach (var item in _shopBuyGoods)
            {
                ct.ThrowIfCancellationRequested();
                if (_categoryFilter != null && !_categoryFilter(item)) continue;
                await AddItem(item);
            }
        }

        /// <summary>
        /// 판매 탭 아이템 목록 생성 (현재 잡화점 정책: MaterialInventory의 보유 재료 중 카테고리 필터를 만족하는 항목)
        /// 추후 다른 인벤토리(IInventory 등) 확장 시 여기에 매입 대상 목록 추가 가능
        /// </summary>
        private async UniTask PopulateSellItemsAsync(CancellationToken ct)
        {
            if (_materialInventory == null) return;

            foreach (var pair in _materialInventory.GetAll())
            {
                ct.ThrowIfCancellationRequested();
                if (pair.Key != null && pair.Value > 0)
                {
                    var item = new MaterialShopItem(pair.Key, pair.Value);
                    if (_categoryFilter != null && !_categoryFilter(item)) continue;
                    await AddItem(item);
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
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshCts = null;

            ClearItems();
            _disposables.Dispose();
        }
    }
}