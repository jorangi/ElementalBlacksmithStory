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
using System.Collections.ObjectModel;
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
        private readonly ISubscriber<ChangeMoneyEvent> _moneySubscriber;
        private readonly ShopService _shopService;
        private readonly MaterialInventory _materialInventory;

        private readonly Dictionary<uint, (ShopCartItemView, uint)> _cartItems = new();
        private readonly Dictionary<uint, uint> _purchaseBuffer = new();
        private readonly Dictionary<ShopCartItemView, CompositeDisposable> _itemDisposables = new();
        private readonly CompositeDisposable _disposables = new();

        private const uint MAX_AMOUNT = 999;

        private ulong _currentMoney = 0;
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

                    _purchaseBuffer.Clear();
                    foreach (var kvp in _cartItems)
                    {
                        _purchaseBuffer[kvp.Key] = kvp.Value.Item2;
                    }

                    ulong totalCost = GetTotalCartCost();
                    var result = _shopService.TryPurchase(_purchaseBuffer, totalCost);
                    if (result)
                    {
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
                    uint maxAmount = GetMaxAffordableAmount(_modalItemId, _modalUnitPrice);
                    _modalAmount = Math.Clamp(_modalAmount + 1, 0, maxAmount);
                    _selectShopAmountView.SetAmount(_modalAmount);
                    UpdateModalPriceAndWallet();
                })
                .AddTo(_disposables);

            _selectShopAmountView.OnDecreaseAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(_ =>
                {
                    uint maxAmount = GetMaxAffordableAmount(_modalItemId, _modalUnitPrice);
                    _modalAmount = Math.Clamp(_modalAmount - 1, 0, maxAmount);
                    _selectShopAmountView.SetAmount(_modalAmount);
                    UpdateModalPriceAndWallet();
                })
                .AddTo(_disposables);

            _selectShopAmountView.OnChangedAmountAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(amount =>
                {
                    uint maxAmount = GetMaxAffordableAmount(_modalItemId, _modalUnitPrice);
                    _modalAmount = Math.Clamp(amount, 0, maxAmount);
                    _selectShopAmountView.SetAmount(_modalAmount);
                    UpdateModalPriceAndWallet();
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
            if (!_cartItems.ContainsKey(_modalItemId))
            {
                _selectShopAmountView.Hide();
                return;
            }

            if (_modalAmount == 0)
            {
                RemoveItem(_modalItemId, _cartItems[_modalItemId].Item2);
            }
            else
            {
                _cartItems[_modalItemId] = (_cartItems[_modalItemId].Item1, _modalAmount);
                _cartItems[_modalItemId].Item1.SetEA(_modalAmount);
            }

            _selectShopAmountView.Hide();
        }

        private ulong GetItemUnitPrice(uint itemId)
        {
            var material = _materialDatabase.GetMaterial(itemId);
            return material != null ? material.Value : 0;
        }

        private ulong GetCartTotalCostExcept(uint excludeItemId)
        {
            ulong total = 0;
            foreach (var kvp in _cartItems)
            {
                if (kvp.Key == excludeItemId) continue;
                ulong unitPrice = GetItemUnitPrice(kvp.Key);
                total += unitPrice * (ulong)kvp.Value.Item2;
            }
            return total;
        }

        public ulong GetTotalCartCost()
        {
            ulong total = 0;
            foreach (var kvp in _cartItems)
            {
                ulong unitPrice = GetItemUnitPrice(kvp.Key);
                total += unitPrice * (ulong)kvp.Value.Item2;
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

        private async UniTask OpenAmountModal(uint itemId)
        {
            if (!_cartItems.ContainsKey(itemId)) return;

            _modalItemId = itemId;
            _modalAmount = _cartItems[itemId].Item2;

            var sprite = await _materialSpriteLoader.GetSprite(itemId);
            var material = _materialDatabase.GetMaterial(itemId);

            string itemName = material != null ? material.materialName : itemId.ToString();
            _modalUnitPrice = material != null ? material.Value : 0;

            uint maxAffordable = GetMaxAffordableAmount(itemId, _modalUnitPrice);
            _selectShopAmountView.SetData(sprite, itemName, _modalUnitPrice, _modalAmount, maxAffordable);

            uint pocketEA = material != null ? _materialInventory.GetCount(material) : 0;
            _selectShopAmountView.SetPocketEA(pocketEA);

            UpdateModalPriceAndWallet();
        }

        public async UniTask SetItem(uint itemId, int ea)
        {
            if (ea >= 0) await AddItem(itemId, (uint)ea);
            else RemoveItem(itemId, (uint)(-ea));
        }

        /// <summary>
        /// 카트에 아이템 추가(개수 만큼)
        /// </summary>
        private async UniTask AddItem(uint itemId, uint ea)
        {
            _view.gameObject.SetActive(true);
            if (_cartItems.ContainsKey(itemId))
            {
                uint newEA = _cartItems[itemId].Item2 + ea;
                _cartItems[itemId] = (_cartItems[itemId].Item1, newEA);
                _cartItems[itemId].Item1.SetEA(newEA);
            }
            else
            {
                Sprite sprite = await _materialSpriteLoader.GetSprite(itemId);
                if (_cartItems.ContainsKey(itemId))
                {
                    uint newEA = _cartItems[itemId].Item2 + ea;
                    _cartItems[itemId] = (_cartItems[itemId].Item1, newEA);
                    _cartItems[itemId].Item1.SetEA(newEA);
                    return;
                }
                var newItem = _view.Create(itemId, ea, sprite);
                _cartItems.Add(itemId, (newItem, ea));

                CompositeDisposable d = new();
                newItem.OnLongPressAsObservable()
                    .Subscribe(_ =>
                    {
                        RemoveItem(itemId, _cartItems[itemId].Item2);
                    }).AddTo(d);

                newItem.OnClickAsObservable()
                    .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                    .Subscribe(_ =>
                    {
                        OpenAmountModal(itemId).Forget();
                    }).AddTo(d);

                _itemDisposables[newItem] = d;
            }
        }

        /// <summary>
        /// 카트에서 아이템 제거(개수 만큼 빼기)
        /// </summary>
        private void RemoveItem(uint itemId, uint ea)
        {
            if (_cartItems.ContainsKey(itemId))
            {
                if (_cartItems[itemId].Item2 > ea)
                {
                    uint newEA = _cartItems[itemId].Item2 - ea;
                    _cartItems[itemId] = (_cartItems[itemId].Item1, newEA);
                    _cartItems[itemId].Item1.SetEA(newEA);
                }
                else
                {
                    var itemView = _cartItems[itemId].Item1;
                    _itemDisposables[itemView].Dispose();
                    _itemDisposables.Remove(itemView);
                    _cartItems.Remove(itemId);
                    UnityEngine.Object.Destroy(itemView.gameObject);
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
            foreach (var d in _itemDisposables)
            {
                d.Value.Dispose();
                if (d.Key != null && d.Key.gameObject != null)
                {
                    UnityEngine.Object.Destroy(d.Key.gameObject);
                }
            }
            _itemDisposables.Clear();
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
