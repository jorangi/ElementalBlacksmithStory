using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using Cysharp.Threading.Tasks;
using MessagePipe;
using R3;
using VContainer;
using VContainer.Unity;
using ElementalBlacksmithStory.Inventory;

namespace ElementalBlacksmithStory.UI
{
    public class ShopCartPresenter : IAsyncStartable, IDisposable
    {
        private readonly ShopCartView _view;
        private readonly SelectShopAmountView _selectShopAmountView;
        private readonly MaterialSpriteLoader _materialSpriteLoader;
        private readonly WeaponSpriteLoader _weaponSpriteLoader;
        private readonly SO_MaterialDatabase _materialDatabase;
        private readonly IPublisher<PlaySoundEvent> _soundPublisher;
        private readonly ISubscriber<ChangeMoneyEvent> _moneySubscriber;
        private readonly ShopService _shopService;
        private readonly MaterialInventory _materialInventory;

        private readonly Dictionary<uint, (ShopCartItemView view, IShopItem item, uint ea)> _cartItems = new();
        private readonly Dictionary<ShopCartItemView, CompositeDisposable> _itemDisposables = new();
        private readonly CompositeDisposable _disposables = new();

        private const uint MAX_AMOUNT = 999;

        private ulong _currentMoney = 0;
        private IShopItem _currentModalItem;
        private uint _modalItemId;
        private uint _modalAmount;
        private ulong _modalUnitPrice;

        [Inject]
        public ShopCartPresenter(
            ShopCartView view,
            SelectShopAmountView selectShopAmountView,
            MaterialSpriteLoader materialSpriteLoader,
            WeaponSpriteLoader weaponSpriteLoader,
            SO_MaterialDatabase materialDatabase,
            IPublisher<PlaySoundEvent> soundPublisher,
            ISubscriber<ChangeMoneyEvent> moneySubscriber,
            ShopService shopService,
            MaterialInventory materialInventory
        )
        {
            _view = view;
            _selectShopAmountView = selectShopAmountView;
            _materialSpriteLoader = materialSpriteLoader;
            _weaponSpriteLoader = weaponSpriteLoader;
            _materialDatabase = materialDatabase;
            _soundPublisher = soundPublisher;
            _moneySubscriber = moneySubscriber;
            _shopService = shopService;
            _materialInventory = materialInventory;

            BindAmountModal();
            BindCartButtons();
        }

        public async UniTask StartAsync(CancellationToken ct = default)
        {
            await SetItem(30001, 1);
            await SetItem(30001, 1);
            await SetItem(30002, 3);
            await SetItem(30004, 5);
            await SetItem(30004, 5);
            await SetItem(30004, -3);
        }

        private void BindCartButtons()
        {
            _view.OnClickAcceptAsObservable()
                .Subscribe(_ =>
                {
                    if (_cartItems.Count == 0) return;

                    var purchaseList = new List<(IShopItem item, uint amount)>();
                    foreach (var entry in _cartItems.Values)
                    {
                        purchaseList.Add((entry.item, entry.ea));
                    }

                    ulong totalCost = GetTotalCartCost();
                    var result = _shopService.TryPurchase(purchaseList, totalCost);
                    if (result)
                    {
                        _soundPublisher.Publish(new PlaySoundEvent(40107));
                        ClearCart();
                    }
                })
                .AddTo(_disposables);

            _view.OnClickCancelAsObservable()
                .Subscribe(_ =>
                {
                    ClearCart();
                })
                .AddTo(_disposables);
        }

        private void BindAmountModal()
        {
            _moneySubscriber.Subscribe(e =>
            {
                _currentMoney = e.MoneyChange;
                if (_selectShopAmountView != null && _selectShopAmountView.IsActivated)
                {
                    UpdateModalPriceAndWallet();
                }
            }).AddTo(_disposables);

            _selectShopAmountView.OnIncreaseAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(_ =>
                {
                    if (_currentModalItem is Weapon) return;

                    int maxAmount = (int)GetMaxAffordableAmount(_modalItemId, _modalUnitPrice);
                    _modalAmount = (uint)Mathf.Min((int)_modalAmount + 1, maxAmount);
                    _selectShopAmountView.SetAmount(_modalAmount);
                    UpdateModalPriceAndWallet();
                })
                .AddTo(_disposables);

            _selectShopAmountView.OnDecreaseAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(_ =>
                {
                    if (_currentModalItem is Weapon) return;

                    int maxAmount = (int)GetMaxAffordableAmount(_modalItemId, _modalUnitPrice);
                    _modalAmount = (uint)Mathf.Max((int)_modalAmount - 1, 0);
                    _selectShopAmountView.SetAmount(_modalAmount);
                    UpdateModalPriceAndWallet();
                })
                .AddTo(_disposables);

            _selectShopAmountView.OnChangedAmountAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(amount =>
                {
                    if (_currentModalItem is Weapon) return;

                    int maxAmount = (int)GetMaxAffordableAmount(_modalItemId, _modalUnitPrice);
                    _modalAmount = (uint)Mathf.Clamp((int)amount, 0, maxAmount);
                    _selectShopAmountView.SetAmount(_modalAmount);
                    UpdateModalPriceAndWallet();
                })
                .AddTo(_disposables);

            _selectShopAmountView.OnPurchaseAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    if (_modalAmount == 0 || _currentModalItem == null) return;

                    uint purchaseItemId = _modalItemId;
                    uint amount = _modalAmount;
                    ulong totalPrice = _modalUnitPrice * (ulong)amount;

                    var singlePurchase = new List<(IShopItem item, uint amount)> { (_currentModalItem, amount) };
                    var result = _shopService.TryPurchase(singlePurchase, totalPrice);
                    if (result)
                    {
                        _soundPublisher.Publish(new PlaySoundEvent(40107));
                        if (_cartItems.ContainsKey(purchaseItemId))
                        {
                            RemoveItem(purchaseItemId, _cartItems[purchaseItemId].ea);
                        }
                        _selectShopAmountView.Hide();
                    }
                })
                .AddTo(_disposables);

            _selectShopAmountView.OnSubmitAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    OnModalSubmit();
                })
                .AddTo(_disposables);

            _selectShopAmountView.OnCancelAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    _selectShopAmountView.Hide();
                })
                .AddTo(_disposables);
        }

        private void OnModalSubmit()
        {
            if (_currentModalItem == null)
            {
                _selectShopAmountView.Hide();
                return;
            }

            if (_cartItems.TryGetValue(_modalItemId, out var entry))
            {
                if (_modalAmount == 0)
                {
                    RemoveItem(_modalItemId, entry.ea);
                }
                else
                {
                    _cartItems[_modalItemId] = (entry.view, entry.item, _modalAmount);
                    entry.view.SetEA(_modalAmount);
                }
            }
            else
            {
                if (_modalAmount > 0)
                {
                    AddItem(_currentModalItem, _modalAmount).Forget();
                }
            }

            _selectShopAmountView.Hide();
        }

        private ulong GetItemUnitPrice(uint itemId)
        {
            if (_cartItems.TryGetValue(itemId, out var entry))
            {
                return entry.item.Price;
            }

            var material = _materialDatabase.GetMaterial(itemId);
            return material != null ? material.Value : 0;
        }

        private ulong GetCartTotalCostExcept(uint excludeItemId)
        {
            ulong total = 0;
            foreach (var kvp in _cartItems)
            {
                if (kvp.Key == excludeItemId) continue;
                total += kvp.Value.item.Price * (ulong)kvp.Value.ea;
            }
            return total;
        }

        public ulong GetTotalCartCost()
        {
            ulong total = 0;
            foreach (var kvp in _cartItems)
            {
                total += kvp.Value.item.Price * (ulong)kvp.Value.ea;
            }
            return total;
        }

        private uint GetMaxAffordableAmount(uint itemId, ulong unitPrice)
        {
            if (unitPrice == 0) return MAX_AMOUNT;

            ulong otherCost = GetCartTotalCostExcept(itemId);
            if (_currentMoney <= otherCost) return 0;

            ulong availableMoney = _currentMoney - otherCost;
            ulong maxByMoney = availableMoney / unitPrice;

            return (uint)Math.Min((ulong)MAX_AMOUNT, maxByMoney);
        }

        private void UpdateModalPriceAndWallet()
        {
            ulong currentItemCost = (ulong)_modalAmount * _modalUnitPrice;
            _selectShopAmountView.SetFinalPrice(currentItemCost);

            ulong otherCost = GetCartTotalCostExcept(_modalItemId);
            ulong totalCartCost = otherCost + currentItemCost;
            ulong remaining = _currentMoney >= totalCartCost ? _currentMoney - totalCartCost : 0;
            _selectShopAmountView.SetRemainingWallet(remaining);
        }

        public async UniTask OpenAmountModal(IShopItem item)
        {
            if (item == null) return;

            _currentModalItem = item;
            _modalItemId = item.Id;

            bool isWeapon = item is Weapon;
            uint currentCartAmount = _cartItems.TryGetValue(item.Id, out var entry) ? entry.ea : 1;
            _modalAmount = isWeapon ? 1 : currentCartAmount;

            Sprite sprite = null;
            if (item.SpriteId.ToString()[0] == '1')
            {
                sprite = await _weaponSpriteLoader.GetSprite(item.SpriteId);
            }
            else
            {
                sprite = await _materialSpriteLoader.GetSprite(item.SpriteId);
            }

            string itemName = item.Name;
            _modalUnitPrice = item.Price;

            uint maxAffordable = isWeapon ? 0 : GetMaxAffordableAmount(item.Id, _modalUnitPrice);
            _selectShopAmountView.SetData(sprite, itemName, _modalUnitPrice, _modalAmount, maxAffordable);

            uint pocketEA = 0;
            if (item is MaterialShopItem matItem && matItem.Data != null)
            {
                pocketEA = _materialInventory.GetCount(matItem.Data);
            }
            _selectShopAmountView.SetPocketEA(pocketEA);

            UpdateModalPriceAndWallet();
        }

        public async UniTask SetItem(uint itemId, int ea)
        {
            if (ea >= 0)
            {
                var mat = _materialDatabase.GetMaterial(itemId);
                if (mat != null)
                {
                    await AddItem(new MaterialShopItem(mat, (uint)ea), (uint)ea);
                }
            }
            else
            {
                RemoveItem(itemId, (uint)(-ea));
            }
        }

        public async UniTask AddItem(IShopItem item, uint ea = 1)
        {
            if (item == null) return;

            _view.gameObject.SetActive(true);

            if (_cartItems.TryGetValue(item.Id, out var existing))
            {
                if (item is Weapon)
                {
                    Debug.LogWarning($"[ShopCartPresenter] {item.Name}은(는) 이미 장바구니에 담겨 있습니다.");
                    return;
                }

                uint newEA = existing.ea + ea;
                _cartItems[item.Id] = (existing.view, existing.item, newEA);
                existing.view.SetEA(newEA);
                return;
            }

            Sprite sprite = null;
            if (item.SpriteId.ToString()[0] == '1')
            {
                sprite = await _weaponSpriteLoader.GetSprite(item.SpriteId);
            }
            else
            {
                sprite = await _materialSpriteLoader.GetSprite(item.SpriteId);
            }

            var newItem = _view.Create(item.Id, ea, sprite);
            _cartItems.Add(item.Id, (newItem, item, ea));

            CompositeDisposable d = new();
            newItem.OnLongPressAsObservable()
                .Subscribe(_ =>
                {
                    RemoveItem(item.Id, _cartItems[item.Id].ea);
                }).AddTo(d);

            newItem.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    OpenAmountModal(item).Forget();
                }).AddTo(d);

            _itemDisposables[newItem] = d;
        }

        /// <summary>
        /// 카트에서 아이템 제거(개수 만큼 빼기)
        /// </summary>
        public void RemoveItem(uint itemId, uint ea)
        {
            if (_cartItems.TryGetValue(itemId, out var entry))
            {
                if (entry.ea > ea)
                {
                    uint newEA = entry.ea - ea;
                    _cartItems[itemId] = (entry.view, entry.item, newEA);
                    entry.view.SetEA(newEA);
                }
                else
                {
                    var itemView = entry.view;
                    if (_itemDisposables.TryGetValue(itemView, out var d))
                    {
                        d.Dispose();
                        _itemDisposables.Remove(itemView);
                    }

                    _cartItems.Remove(itemId);
                    if (itemView != null && itemView.gameObject != null)
                    {
                        UnityEngine.Object.Destroy(itemView.gameObject);
                    }
                }
            }

            if (_cartItems.Count == 0)
            {
                _view.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 장바구니 비우기
        /// </summary>
        public void ClearCart()
        {
            foreach (var d in _itemDisposables.Values)
            {
                d.Dispose();
            }
            _itemDisposables.Clear();

            foreach (var entry in _cartItems.Values)
            {
                if (entry.view != null && entry.view.gameObject != null)
                {
                    UnityEngine.Object.Destroy(entry.view.gameObject);
                }
            }
            _cartItems.Clear();

            _view.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            foreach (var d in _itemDisposables.Values)
            {
                d.Dispose();
            }
            _itemDisposables.Clear();
        }
    }
}

