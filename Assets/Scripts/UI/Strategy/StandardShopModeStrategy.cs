using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using VContainer;

namespace ElementalBlacksmithStory.UI.Strategy
{
    /// <summary>
    /// 일반 상점 모드(장바구니 담기 및 수량 모달 오픈)의 아이템 상호작용 전략
    /// </summary>
    public class StandardShopModeStrategy : IShopModeStrategy
    {
        private readonly ShopCartPresenter _cartPresenter;
        private readonly PlayerData _playerData;
        private readonly IItemShopFactory _itemShopFactory;

        private readonly List<IShopItem> _shopBuyGoods = new();

        [Inject]
        public StandardShopModeStrategy(
            ShopCartPresenter cartPresenter,
            PlayerData playerData,
            IItemShopFactory itemShopFactory
            )
        {
            _cartPresenter = cartPresenter;
            _playerData = playerData;
            _itemShopFactory = itemShopFactory;
        }
        public void SetShopData(SO_ShopData shopData)
        {
            _shopBuyGoods.Clear();
            if (shopData == null) return;

            var stockedItem = shopData.GetAllStockedItems();
            for (int i = 0; i < stockedItem.Count; i++)
            {
                var entry = stockedItem[i];
                var item = _itemShopFactory.Create(entry.itemId, entry.stock, 1.0f);
                if (item != null)
                {
                    _shopBuyGoods.Add(item);
                }
            }
        }
        public ulong GetPrice(IShopItem item) => item?.Value ?? 0;

        public bool CanInteract(IShopItem item)
        {
            if (item == null || _cartPresenter == null) return false;

            // 판매
            if (_cartPresenter.IsSellMode) return true;

            // 구매
            bool isUniqueItem = item is Weapon || ItemIdHelper.GetCategory(item.SpriteId) == ItemCategory.Weapon;
            bool inCartAlready = isUniqueItem && _cartPresenter.ContainsCartItem(item.Id);

            // 장비(amount를 사용하지 않음)가 이미 카트에 있거나, 잔여 소지금이 아이템 가격보다 부족하면 비활성화
            return !inCartAlready && _cartPresenter.ExpectedRemainingMoney >= item.Value;
        }

        public void OnItemClicked(IShopItem item)
        {
            if (item == null || _cartPresenter == null) return;
            if (!CanInteract(item)) return;

            bool isUniqueItem = item is Weapon || ItemIdHelper.GetCategory(item.SpriteId) == ItemCategory.Weapon;

            if (isUniqueItem)
            {
                // 장비 아이템은 수량 선택 모달창 없이 1개를 카트에 즉시 추가
                if (!_cartPresenter.IsSellMode && _cartPresenter.ContainsCartItem(item.Id))
                {
                    return;
                }
                _cartPresenter.AddItem(item, 1).Forget();
            }
            else
            {
                // 이외 아이템은 수량 선택 모달창 오픈
                _cartPresenter.OpenAmountModal(item).Forget();
            }
        }

        public UniTask GetDisplayItemAsync(List<IShopItem> results, bool isSellMode, CancellationToken ct = default)
        {
            results.Clear();

            if (isSellMode)
            {
                foreach (var pair in _playerData.MaterialInventory.GetAll())
                {
                    if (pair.Key != null && pair.Value > 0)
                    {
                        results.Add(new MaterialShopItem(pair.Key, pair.Value));
                    }
                }
                foreach (var pair in _playerData.RuneInventory.GetAll())
                {
                    if (pair.Key != null && pair.Value > 0)
                    {
                        results.Add(new Rune(pair.Key, pair.Value));
                    }
                }
                foreach (var weapon in _playerData.EquipmentInventory.GetAllActive())
                {
                    if (weapon != null)
                    {
                        results.Add(weapon);
                    }
                }
            }
            else
            {
                for (int i = 0; i < _shopBuyGoods.Count; i++)
                {
                    results.Add(_shopBuyGoods[i]);
                }
            }
            return UniTask.CompletedTask;
        }
    }
}