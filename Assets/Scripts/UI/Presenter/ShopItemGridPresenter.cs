using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Inventory;
using MessagePipe;
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
        private readonly RuneSpriteLoader _runeSpriteLoader;
        private readonly MaterialInventory _materialInventory;
        private readonly EquipmentInventory _equipmentInventory;
        private readonly RuneInventory _runeInventory;
        private readonly IItemShopFactory _itemShopFactory;
        private readonly ShopCartPresenter _shopCartPresenter;
        private readonly ShopService _shopService;
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
            RuneSpriteLoader runeSpriteLoader,
            MaterialInventory materialInventory,
            EquipmentInventory equipmentInventory,
            RuneInventory runeInventory,
            IItemShopFactory itemShopFactory,
            ShopCartPresenter shopCartPresenter,
            ShopService shopService = null,
            ISubscriber<ChangeMoneyEvent> moneySubscriber = null
        )
        {
            _view = view;
            _weaponSpriteLoader = weaponSpriteLoader;
            _materialSpriteLoader = materialSpriteLoader;
            _runeSpriteLoader = runeSpriteLoader;
            _materialInventory = materialInventory;
            _equipmentInventory = equipmentInventory;
            _runeInventory = runeInventory;
            _itemShopFactory = itemShopFactory;
            _shopCartPresenter = shopCartPresenter;
            _shopService = shopService;

            moneySubscriber?.Subscribe(_ =>
            {
                UpdateAllItemsAffordability();
            }).AddTo(_disposables);

            _shopCartPresenter?.OnCartChanged
                .Subscribe(_ =>
                {
                    UpdateAllItemsAffordability();
                })
                .AddTo(_disposables);

            // 재료 인벤토리 수량 변경 시 실시간 그리드 갱신
            if (_materialInventory != null)
            {
                _materialInventory.OnItemCountChangedAsObservable
                    .Subscribe(OnMaterialCountChanged)
                    .AddTo(_disposables);
            }

            // 룬 인벤토리 수량 변경 시 실시간 그리드 갱신
            if (_runeInventory != null)
            {
                _runeInventory.OnItemCountChangedAsObservable
                    .Subscribe(OnRuneCountChanged)
                    .AddTo(_disposables);
            }

            // 장비(무기) 인벤토리 변경 시 실시간 그리드 갱신
            if (_equipmentInventory != null)
            {
                _equipmentInventory.OnAddWeapon
                    .Subscribe(weapon =>
                    {
                        if (!_isSellMode || weapon == null) return;
                        if (_categoryFilter == null || _categoryFilter(weapon))
                        {
                            AddItem(weapon).Forget();
                        }
                    })
                    .AddTo(_disposables);

                _equipmentInventory.OnRemoveWeapon
                    .Subscribe(_ =>
                    {
                        if (_isSellMode)
                        {
                            RefreshGridAsync().Forget();
                        }
                    })
                    .AddTo(_disposables);
            }
        }

        private void OnMaterialCountChanged((SO_MaterialData item, uint count) data)
        {
            if (data.item == null) return;

            if (_isSellMode)
            {
                if (data.count == 0)
                {
                    RemoveItem(data.item.Id);
                }
                else if (_displayItems.TryGetValue(data.item.Id, out var viewItem))
                {
                    viewItem.SetAmountInPocket(data.count);
                }
                else
                {
                    var item = new MaterialShopItem(data.item, data.count);
                    if (_categoryFilter == null || _categoryFilter(item))
                    {
                        AddItem(item).Forget();
                    }
                }
            }
            else
            {
                if (_displayItems.TryGetValue(data.item.Id, out var viewItem))
                {
                    viewItem.SetAmountInPocket(data.count);
                }
            }
        }

        private void OnRuneCountChanged((SO_RuneData item, uint count) data)
        {
            if (data.item == null) return;

            if (_isSellMode)
            {
                if (data.count == 0)
                {
                    RemoveItem(data.item.Id);
                }
                else if (_displayItems.TryGetValue(data.item.Id, out var viewItem))
                {
                    viewItem.SetAmountInPocket(data.count);
                }
                else
                {
                    var item = new Rune(data.item);
                    if (_categoryFilter == null || _categoryFilter(item))
                    {
                        AddItem(item).Forget();
                    }
                }
            }
            else
            {
                if (_displayItems.TryGetValue(data.item.Id, out var viewItem))
                {
                    viewItem.SetAmountInPocket(data.count);
                }
            }
        }

        public async UniTask StartAsync(CancellationToken ct = default)
        {
            _isInitialized = true;
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// SO_ShopData 기반으로 상점 판매 물품 목록 교체 (소속 카테고리의 모든 입고 아이템 취합)
        /// </summary>
        public void SetShopGoods(SO_ShopData shopData)
        {
            _shopBuyGoods.Clear();

            if (shopData != null)
            {
                var stockedItems = shopData.GetAllStockedItems();
                foreach (var entry in stockedItems)
                {
                    var item = _itemShopFactory.Create(entry.itemId, entry.stock, 1.0f);
                    if (item != null)
                    {
                        _shopBuyGoods.Add(item);
                    }
                }
            }

            if (_isInitialized && !_isSellMode)
            {
                RefreshGridAsync().Forget();
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
            for (int i = 0; i < _shopBuyGoods.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var item = _shopBuyGoods[i];
                if (_categoryFilter != null && !_categoryFilter(item)) continue;
                await AddItem(item);
            }
        }

        /// <summary>
        /// 판매 탭 아이템 목록 생성 (보유 재료, 룬, 무기 중 현재 카테고리 필터를 만족하는 항목)
        /// </summary>
        private async UniTask PopulateSellItemsAsync(CancellationToken ct)
        {
            // 1. 재료 인벤토리
            if (_materialInventory != null)
            {
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

            // 2. 룬 인벤토리
            if (_runeInventory != null)
            {
                foreach (var pair in _runeInventory.GetAll())
                {
                    ct.ThrowIfCancellationRequested();
                    if (pair.Key != null && pair.Value > 0)
                    {
                        var item = new Rune(pair.Key);
                        if (_categoryFilter != null && !_categoryFilter(item)) continue;
                        await AddItem(item);
                    }
                }
            }

            // 3. 장비(무기) 인벤토리
            if (_equipmentInventory != null)
            {
                foreach (var weapon in _equipmentInventory.GetAllActive())
                {
                    ct.ThrowIfCancellationRequested();
                    if (weapon != null)
                    {
                        if (_categoryFilter != null && !_categoryFilter(weapon)) continue;
                        await AddItem(weapon);
                    }
                }
            }
        }

        public async UniTask AddItem(IShopItem item)
        {
            Sprite icon = null;
            bool isUniqueItem = false;
            switch (ItemIdHelper.GetCategory(item.SpriteId))
            {
                case ItemCategory.Weapon:
                    icon = await _weaponSpriteLoader.GetSprite(item.SpriteId);
                    isUniqueItem = true;
                    break;
                case ItemCategory.Material:
                    icon = await _materialSpriteLoader.GetSprite(item.SpriteId);
                    isUniqueItem = false;
                    break;
                case ItemCategory.Rune:
                    icon = await _runeSpriteLoader.GetSprite(item.SpriteId);
                    isUniqueItem = false;
                    break;
                default:
                    Debug.LogError($"[ShopItemGridPresenter]: {item.SpriteId}은 유효한 Sprite ID가 아닙니다.");
                    return;
            }

            ShopItemView itemView = _view.CreateItem(item.Id, item.Name, item.Value, icon, isUniqueItem);
            if (itemView == null) return;

            ulong availableMoney = _shopCartPresenter != null
                ? _shopCartPresenter.ExpectedRemainingMoney
                : (_shopService != null ? _shopService.CurrentMoney : 0);
            bool canAfford = _isSellMode || (availableMoney >= item.Value);
            itemView.SetInteractable(canAfford);

            if (_itemDisposables.TryGetValue(item.Id, out var prevDisp))
            {
                prevDisp.Dispose();
                _itemDisposables.Remove(item.Id);
            }

            var clickSub = itemView.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    ulong currentExpected = _shopCartPresenter != null
                        ? _shopCartPresenter.ExpectedRemainingMoney
                        : (_shopService != null ? _shopService.CurrentMoney : 0);
                    if (!_isSellMode && currentExpected < item.Value)
                    {
                        return;
                    }
                    _shopCartPresenter.OpenAmountModal(item).Forget();
                });
            _itemDisposables[item.Id] = clickSub;

            if (!isUniqueItem)
            {
                if (item is MaterialShopItem materialItem && materialItem.Data != null && _materialInventory != null)
                {
                    uint currentCount = _materialInventory.GetCount(materialItem.Data);
                    itemView.SetAmountInPocket(currentCount);
                }
                else if (item is Rune runeItem && runeItem.Data != null && _runeInventory != null)
                {
                    uint currentCount = _runeInventory.GetCount(runeItem.Data);
                    itemView.SetAmountInPocket(currentCount);
                }
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

        private void UpdateAllItemsAffordability()
        {
            if (_isSellMode)
            {
                foreach (var kvp in _displayItems)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.SetInteractable(true);
                    }
                }
                return;
            }

            ulong availableMoney = _shopCartPresenter != null
                ? _shopCartPresenter.ExpectedRemainingMoney
                : (_shopService != null ? _shopService.CurrentMoney : 0);

            foreach (var kvp in _displayItems)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetInteractable(availableMoney >= kvp.Value.Price);
                }
            }
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