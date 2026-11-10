using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 등록할 아이템 목록을 보여주는 프리젠터
    /// </summary>
    public class BargainItemGridPresenter : IDisposable
    {
        private static readonly ItemCategory[] ValideCategories = new[]
        {
            ItemCategory.Weapon,
            ItemCategory.Material,
            ItemCategory.Rune,
        };
        private readonly CompositeDisposable _disposables = new();
        private readonly PlayerData _playerData;
        private readonly TradingPostService _tradingPostService;
        private readonly BargainItemGridView _view;
        private readonly UIBackPanelPresenter _uiBackPanelPresenter;
        private readonly WeaponSpriteLoader _weaponSpriteLoader;
        private readonly MaterialSpriteLoader _materialSpriteLoader;
        private readonly RuneSpriteLoader _runeSpriteLoader;
        /// <summary>
        /// 현재 뷰에 표시되고 있는 아이템들을 담는 딕셔너리
        /// </summary>
        private readonly Dictionary<uint, IShopItem> _displayItems = new(128);
        private readonly HashSet<uint> _checkedItemIds = new(128);
        private readonly Dictionary<uint, uint> _selectedAmounts = new(128);
        public readonly List<IShopItem> _registeredBuffer = new(128);
        private ReactiveProperty<int> _currentCategoryIndex = new(0);
        private readonly BargainRegisterItemDetailPresenter _detailPresenter;
        private readonly ForgeManager _forgeManager;
        [Inject]
        public BargainItemGridPresenter(
            ForgeManager forgeManager,
            PlayerData playerData,
            TradingPostService tradingPostService,
            WeaponSpriteLoader weaponSpriteLoader,
            MaterialSpriteLoader materialSpriteLoader,
            RuneSpriteLoader runeSpriteLoader,
            UIBackPanelPresenter uiBackPanelPresenter,
            BargainItemGridView view,
            BargainRegisterItemDetailPresenter detailPresenter,
            ISubscriber<RegisterBargainItemEvent> _registerEventSubscriber,
            ShopItemGridPresenter shopItemGridPresenter
        )
        {
            _playerData = playerData;
            _forgeManager = forgeManager;
            _tradingPostService = tradingPostService;
            _view = view;
            _detailPresenter = detailPresenter;
            _uiBackPanelPresenter = uiBackPanelPresenter;
            _weaponSpriteLoader = weaponSpriteLoader;
            _materialSpriteLoader = materialSpriteLoader;
            _runeSpriteLoader = runeSpriteLoader;
            _currentCategoryIndex.Subscribe(this, static (index, self) =>
            {
                ItemCategory category = ValideCategories[index];
                self._view.SetCategoryName(GetCategoryName(category));
                self.LoadItemsByCategory(category).Forget();
            }).AddTo(_disposables);
            _view.OnRegisterButtonAsObservable()
                        .Subscribe((this, shopItemGridPresenter), static (_, state) =>
                        {
                            state.Item1._registeredBuffer.Clear();
                            foreach (var id in state.Item1._checkedItemIds)
                            {
                                if (state.Item1._displayItems.TryGetValue(id, out var item))
                                {
                                    uint selectedAmount = state.Item1._selectedAmounts.TryGetValue(id, out var amt) ? amt : item.Count;
                                    if (item is MaterialShopItem matItem)
                                    {
                                        state.Item1._registeredBuffer.Add(new MaterialShopItem(matItem.Data, selectedAmount, MaterialShopItem.GenerateInstanceId()));
                                    }
                                    else if (item is Rune runeItem)
                                    {
                                        state.Item1._registeredBuffer.Add(new Rune(runeItem.Data, selectedAmount, Rune.GenerateInstanceId()));
                                    }
                                    else
                                    {
                                        state.Item1._registeredBuffer.Add(item);
                                    }
                                }
                            }
                            state.Item1._tradingPostService.RegisterBargainItems(state.Item1._registeredBuffer);
                            state.Item1.Hide();
                            state.shopItemGridPresenter.RefreshGridAsync().Forget();
                        })
                        .AddTo(_disposables);
            _view.OnCancelButtonAsObservable()
                        .Subscribe(this, static (_, self) =>
                        {
                            self.Hide();
                        })
                        .AddTo(_disposables);
            _view.OnNextCategoryButtonAsObservable()
                        .Subscribe(this, static (_, self) =>
                        {
                            self.NextCategory();
                        })
                        .AddTo(_disposables);
            _view.OnPrevCategoryButtonAsObservable()
                        .Subscribe(this, static (_, self) =>
                        {
                            self.PrevCategory();
                        })
                        .AddTo(_disposables);
            _registerEventSubscriber.Subscribe(e =>
            {
                if (_displayItems.TryGetValue(e._itemId, out var item))
                {
                    _checkedItemIds.Add(e._itemId);
                    _selectedAmounts[e._itemId] = e._amount;
                    _view.SetCardChecked(e._itemId, true, e._amount);
                }
            }).AddTo(_disposables);
            _view.OnCardClick
                .ThrottleFirst(TimeSpan.FromMilliseconds(100))
                        .Subscribe(this, static (clickedItem, self) =>
                        {
                            if (self._checkedItemIds.Remove(clickedItem.ItemId))
                            {
                                self._selectedAmounts.Remove(clickedItem.ItemId);
                                clickedItem.Check(false);
                                return;
                            }
                            if (clickedItem.IsMutitple)
                            {
                                if (self._displayItems.TryGetValue(clickedItem.ItemId, out var item))
                                {
                                    self._detailPresenter.SetData(item).Forget();
                                    self._detailPresenter.Show();
                                    return;
                                }
                                Debug.LogWarning($"[BargainItemGridPresenter] 아이템을 찾을 수 없습니다 ItemId : {clickedItem.ItemId}");
                                return;
                            }
                            else
                            {
                                self._checkedItemIds.Add(clickedItem.ItemId);
                                self._selectedAmounts[clickedItem.ItemId] = 1;
                                clickedItem.Check(true);
                            }
                        }).AddTo(_disposables);
        }
        /// <summary>
        /// 플레이어 인벤토리에서 지정한 카테고리에 해당하는 아이템을 조회해 뷰에 등록하는 함수
        /// </summary>
        /// <param name="category"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async UniTask LoadItemsByCategory(ItemCategory category, CancellationToken ct = default)
        {
            _view.Clear();
            switch (category)
            {
                case ItemCategory.Weapon:
                    var weapons = _playerData.EquipmentInventory.GetAllActive();
                    foreach (var weapon in weapons)
                    {
                        if (weapon == null || weapon.WeaponId % 1000 == 1) continue;
                        _displayItems[weapon.Id] = weapon;
                        Sprite icon = await _weaponSpriteLoader.GetSprite(weapon.WeaponId);
                        var card = _view.CreatItem(weapon.Id, icon, 1);
                        if (_checkedItemIds.Contains(weapon.Id))
                        {
                            card.Check(true);
                        }
                    }
                    break;
                case ItemCategory.Material:
                    var materials = _playerData.MaterialInventory.GetAll();
                    foreach (var pair in materials)
                    {
                        if (pair.Key == null || pair.Value == 0) continue;
                        var item = new MaterialShopItem(pair.Key, pair.Value);
                        _displayItems[item.Id] = item;
                        Sprite icon = await _materialSpriteLoader.GetSprite(pair.Key.Id);
                        var card = _view.CreatItem(pair.Key.Id, icon, pair.Value);
                        if (_checkedItemIds.Contains(item.Id))
                        {
                            uint selectedAmount = _selectedAmounts.TryGetValue(item.Id, out var amt) ? amt : pair.Value;
                            card.SetAmount(selectedAmount);
                            card.Check(true);
                        }
                    }
                    break;
                case ItemCategory.Rune:
                    var runes = _playerData.RuneInventory.GetAll();
                    foreach (var pair in runes)
                    {
                        if (pair.Key == null || pair.Value == 0) continue;
                        var item = new Rune(pair.Key, pair.Value);
                        _displayItems[item.Id] = item;
                        Sprite icon = await _runeSpriteLoader.GetSprite(pair.Key.Id);
                        var card = _view.CreatItem(pair.Key.Id, icon, pair.Value);
                        if (_checkedItemIds.Contains(item.Id))
                        {
                            uint selectedAmount = _selectedAmounts.TryGetValue(item.Id, out var amt) ? amt : pair.Value;
                            card.SetAmount(selectedAmount);
                            card.Check(true);
                        }
                    }
                    break;
            }
        }
        /// <summary>
        /// 카테고리 별 한글 이름 치환 헬퍼 함수
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private static string GetCategoryName(ItemCategory category) =>
        category switch
        {
            ItemCategory.Weapon => "무기",
            ItemCategory.Material => "재료",
            ItemCategory.Rune => "룬",
            _ => string.Empty,
        };
        private void NextCategory()
        {
            _currentCategoryIndex.Value = (_currentCategoryIndex.Value + 1) % ValideCategories.Length;
        }
        private void PrevCategory()
        {
            _currentCategoryIndex.Value = (_currentCategoryIndex.Value - 1 + ValideCategories.Length) % ValideCategories.Length;
        }
        public void Show()
        {
            ItemCategory category = ValideCategories[_currentCategoryIndex.Value];
            _view.SetCategoryName(GetCategoryName(category));
            LoadItemsByCategory(category).Forget();
            _uiBackPanelPresenter.Show();
            _view.Show().Forget();
        }
        public void Hide()
        {
            _view.UnCheckAll();
            _checkedItemIds.Clear();
            _selectedAmounts.Clear();
            _view.Hide().Forget();
            _uiBackPanelPresenter.Hide();
        }
        public void Dispose()
        {
            _disposables.Dispose();
            _disposables.Clear();
            _checkedItemIds.Clear();
            _selectedAmounts.Clear();
            _displayItems.Clear();
            _registeredBuffer.Clear();
        }
    }
}