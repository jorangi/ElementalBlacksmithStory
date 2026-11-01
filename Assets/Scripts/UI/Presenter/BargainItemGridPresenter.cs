using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using Mono.Cecil.Cil;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.UI
{
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
        private readonly Dictionary<uint, IShopItem> _displayItems = new(128);
        private readonly HashSet<uint> _checkedItemIds = new(128);
        public readonly List<IShopItem> _registeredBuffer = new(128);
        private ReactiveProperty<int> _currentCategoryIndex = new(0);
        [Inject]
        public BargainItemGridPresenter(
            PlayerData playerData,
            TradingPostService tradingPostService,
            WeaponSpriteLoader weaponSpriteLoader,
            MaterialSpriteLoader materialSpriteLoader,
            RuneSpriteLoader runeSpriteLoader,
            UIBackPanelPresenter uiBackPanelPresenter,
            BargainItemGridView view
        )
        {
            _playerData = playerData;
            _tradingPostService = tradingPostService;
            _view = view;
            _uiBackPanelPresenter = uiBackPanelPresenter;
            _weaponSpriteLoader = weaponSpriteLoader;
            _materialSpriteLoader = materialSpriteLoader;
            _runeSpriteLoader = runeSpriteLoader;
            _currentCategoryIndex.Subscribe(index =>
            {
                ItemCategory category = ValideCategories[index];
                _view.SetCategoryName(GetCategoryName(category));
                LoadItemsByCategory(category).Forget();
            }).AddTo(_disposables);
            _view.OnRegisterButtonAsObservable()
                .Subscribe(_ =>
                {
                    foreach (var id in _checkedItemIds)
                    {
                        if (_displayItems.TryGetValue(id, out var item))
                        {
                            _registeredBuffer.Add(item);
                        }
                    }
                    _tradingPostService.RegisterBargainItems(_registeredBuffer);
                    _view.UnCheckAll();
                    _checkedItemIds.Clear();
                    Hide();
                })
                .AddTo(_disposables);
            _view.OnCancleButtonAsObservable()
                .Subscribe(_ =>
                {
                    _view.UnCheckAll();
                    _checkedItemIds.Clear();
                    Hide();
                })
                .AddTo(_disposables);
            _view.OnNextCategoryButtonAsObservable()
                .Subscribe(_ =>
                {
                    NextCategory();
                })
                .AddTo(_disposables);
            _view.OnPrevCategoryButtonAsObservable()
                .Subscribe(_ =>
                {
                    PrevCategory();
                })
                .AddTo(_disposables);
            _view.OnCardClick
                .ThrottleFirst(TimeSpan.FromMilliseconds(100))
                .Subscribe(clickedItem =>
                {
                    if (_checkedItemIds.Remove(clickedItem.ItemId))
                    {
                        clickedItem.Check(false);
                    }
                    else
                    {
                        _checkedItemIds.Add(clickedItem.ItemId);
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
                        if (weapon == null) continue;
                        Sprite icon = await _weaponSpriteLoader.GetSprite(weapon.Id);
                        _view.CreatItem(weapon.Id, icon, 1);
                    }
                    break;
                case ItemCategory.Material:
                    var materials = _playerData.MaterialInventory.GetAll();
                    foreach (var pair in materials)
                    {
                        if (pair.Key == null || pair.Value == 0) continue;
                        Sprite icon = await _materialSpriteLoader.GetSprite(pair.Key.Id);
                        _view.CreatItem(pair.Key.Id, icon, pair.Value);
                    }
                    break;
                case ItemCategory.Rune:
                    var runes = _playerData.RuneInventory.GetAll();
                    foreach (var pair in runes)
                    {
                        if (pair.Key == null || pair.Value == 0) continue;
                        Sprite icon = await _runeSpriteLoader.GetSprite(pair.Key.Id);
                        _view.CreatItem(pair.Key.Id, icon, pair.Value);
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
            _uiBackPanelPresenter.Show();
            _view.Show().Forget();
        }
        public void Hide()
        {
            _view.Hide().Forget();
            _uiBackPanelPresenter.Hide();
        }
        public void Dispose()
        {
            _disposables.Dispose();
            _disposables.Clear();
        }
    }
}