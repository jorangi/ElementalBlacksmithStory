using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using ObservableCollections;
using R3;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class TradingPostService : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly PlayerData _playerData;
        private readonly Dictionary<uint, IShopItem> _auctionItems = new();
        private readonly ObservableDictionary<uint, BidContext> _highestBids = new();
        public ObservableDictionary<uint, BidContext> HighestBids => _highestBids;
        private readonly Dictionary<uint, ulong> _playerBids = new();
        private readonly List<uint> _settleBuffer = new(16);
        private readonly IPublisher<AcquireItemEvent> _acquireItemPublisher;
        private readonly Dictionary<ItemCategory, List<IShopItem>> _bargainItems = new();
        private readonly SO_MaterialDatabase _materialDatabase;
        private readonly SO_RuneDatabase _runeDatabase;
        private readonly AuctionSimulatingService _auctionSimulateService;
        private readonly BargainSimulatingService _bargainSimulateService;
        private readonly ForgeManager _forgeManager;
        private readonly IPublisher<QuickSellEvent> _quickSellPublisher;
        public Observable<IShopItem> OnBargainItemSettledAsObservable => _bargainSimulateService.OnItemSettledAsObservable;
        public Observable<IShopItem> OnBargainItemWithdrawAsObservable => _bargainSimulateService.OnItemWithdrawAsObservable;

        [Inject]
        public TradingPostService(
            PlayerData playerData,
            AuctionSimulatingService auctionSimulateService,
            BargainSimulatingService bargainSimulateService,
            IPublisher<AcquireItemEvent> acquireItemPublisher,
            SO_MaterialDatabase materialDatabase,
            SO_RuneDatabase runeDatabase,
            ForgeManager forgeManager = null,
            IPublisher<QuickSellEvent> quickSellPublisher = null
        )
        {
            _playerData = playerData;
            _acquireItemPublisher = acquireItemPublisher;
            _materialDatabase = materialDatabase;
            _runeDatabase = runeDatabase;
            _auctionSimulateService = auctionSimulateService;
            _bargainSimulateService = bargainSimulateService;
            _forgeManager = forgeManager;
            _quickSellPublisher = quickSellPublisher;

            _auctionSimulateService.SetAuctionHandler(
                getCurerentPrice: itemId => _highestBids.TryGetValue(itemId, out var bid) ? bid.Price : 0,
                onBidAction: (itemId, bidContext) => Bid(itemId, bidContext)
            );

            _bargainItems[ItemCategory.Weapon] = new List<IShopItem>();
            _bargainItems[ItemCategory.Material] = new List<IShopItem>();
            _bargainItems[ItemCategory.Rune] = new List<IShopItem>();

            _bargainSimulateService.OnItemSettledAsObservable
                .Subscribe(this, static (item, self) =>
                {
                    self.RemoveBargainItem(item);
                }).AddTo(_disposables);
            _bargainSimulateService.OnItemWithdrawAsObservable
                .Subscribe(this, static (item, self) =>
                {
                    self.RemoveBargainItem(item);
                }).AddTo(_disposables);
        }
        /// <summary>
        /// 경매 아이템을 등록하는 함수
        /// </summary>
        /// <param name="item">등록할 아이템</param>
        /// <param name="bidContext">입찰 컨텍스트</param>
        public void RegisterBidItem(IShopItem item, BidContext bidContext)
        {
            _auctionItems[item.Id] = item;
            _highestBids[item.Id] = bidContext;
            _auctionSimulateService.InitBidSetting(item);
        }
        /// <summary>
        /// 경매에서 아이템을 등록 해제하는 함수
        /// </summary>
        /// <param name="itemId">등록 해제할 아이템 인스턴스 ID</param>
        public void UnregisterBidItem(uint itemId)
        {
            _highestBids.Remove(itemId);
            _auctionItems.Remove(itemId);
            _playerBids.Remove(itemId);
            _auctionSimulateService.RemoveBidders(itemId);
        }
        /// <summary>
        /// 경매 아이템을 모두 경매 종료 처리하는 함수
        /// </summary>
        public void ClearAuction()
        {
            _settleBuffer.Clear();
            foreach (var kvp in _auctionItems)
            {
                _settleBuffer.Add(kvp.Key);
            }

            for (int i = 0; i < _settleBuffer.Count; i++)
            {
                uint id = _settleBuffer[i];
                SettleAuction(id);
                _auctionSimulateService.RemoveBidders(id);
            }
            _settleBuffer.Clear();

            _auctionItems.Clear();
            _highestBids.Clear();
            _playerBids.Clear();
        }
        /// <summary>
        /// 아이템에 입찰하는 함수
        /// </summary>
        /// <param name="targetId"></param>
        /// <param name="bidContext"></param>
        /// <returns></returns>
        public bool Bid(uint targetId, BidContext bidContext)
        {
            if (_highestBids.TryGetValue(targetId, out var current))
            {
                bool canBid = string.IsNullOrEmpty(current.Bidder)
                    ? bidContext.Price >= current.Price
                    : bidContext.Price > current.Price;
                if (!canBid) return false;
            }
            if (bidContext.Bidder == "player")
            {
                _playerBids.TryGetValue(targetId, out ulong alreadyCommitted);
                ulong diff = bidContext.Price - alreadyCommitted;
                if (!_playerData.TrySpend(diff))
                {
                    return false;
                }
                _playerBids[targetId] = bidContext.Price;
            }
            _highestBids[targetId] = bidContext;
            return true;
        }
        /// <summary>
        /// 경매 낙찰 함수
        /// </summary>
        /// <param name="targetId"></param>
        public void SettleAuction(uint targetId)
        {
            if (!_highestBids.TryGetValue(targetId, out var highestBid)) return;
            _playerBids.TryGetValue(targetId, out ulong playerSpent);
            if (highestBid.Bidder == "player")
            {
                if (_auctionItems.TryGetValue(targetId, out var item) && item != null)
                {
                    if (item is Weapon _weapon)
                    {
                        _acquireItemPublisher.Publish(new AcquireItemEvent(
                            weapon: _weapon,
                            sender: "TradingPost",
                            target: "player",
                            reason: "AuctionSettle"
                        ));
                    }
                    else
                    {
                        _acquireItemPublisher.Publish(new AcquireItemEvent(
                            itemId: item.ItemId,
                            amount: item.Count,
                            sender: "TradingPost",
                            target: "player",
                            reason: "AuctionSettle"
                        ));
                    }
                }
                Debug.LogWarning($"[TradingPostService] {targetId} 아이템 낙찰에 성공했습니다. 낙찰액: {playerSpent}");
            }
            else if (playerSpent > 0)
            {
                _playerData.AddMoney(playerSpent);
                Debug.LogWarning($"[TradingPostService] {targetId} 아이템 낙찰에 실패했습니다. 환불액: {playerSpent}, 낙찰자: {highestBid.Bidder}");
            }
        }
        /// <summary>
        /// 거래 아이템 등록하는 함수
        /// </summary>
        /// <param name="items"></param>
        public void RegisterBargainItems(IReadOnlyList<IShopItem> items)
        {
            if (items == null)
            {
                Debug.LogWarning("[TradingPostService] 등록할 아이템이 없습니다.");
                return;
            }
            bool isEquippedWeaponRegistered = false;
            foreach (var item in items)
            {
                if (item == null)
                {
                    Debug.LogWarning($"[TradingPostService] {item.ItemId} 아이템이 유효하지 않아 등록할 수 없습니다.");
                    continue;
                }
                IShopItem registeredItem = item;
                ItemCategory category = ItemCategory.Unknown;
                switch (item.Category)
                {
                    case ShopItemCategory.Sword:
                    case ShopItemCategory.Bow:
                        category = ItemCategory.Weapon;
                        if (item is Weapon w)
                        {
                            if (_forgeManager != null && _forgeManager.CurrentWeapon != null &&
                                (_forgeManager.CurrentWeapon == w || _forgeManager.CurrentWeapon.Id == w.Id))
                            {
                                isEquippedWeaponRegistered = true;
                            }
                            _playerData.EquipmentInventory.Remove(w);
                        }
                        else
                        {
                            _playerData.EquipmentInventory.RemoveById(item.Id);
                        }
                        break;
                    case ShopItemCategory.Material:
                        category = ItemCategory.Material;
                        if (item is MaterialShopItem mItem)
                        {
                            _playerData.MaterialInventory.Get(mItem.Data, item.Count);
                        }
                        else
                        {
                            _playerData.MaterialInventory.Get(_materialDatabase.Get(item.ItemId), item.Count);
                        }
                        if (item.Id == item.ItemId && item is MaterialShopItem matItem)
                        {
                            registeredItem = new MaterialShopItem(matItem.Data, item.Count, MaterialShopItem.GenerateInstanceId());
                        }
                        break;
                    case ShopItemCategory.Rune:
                        category = ItemCategory.Rune;
                        if (item is Rune rItem)
                        {
                            _playerData.RuneInventory.Get(rItem.Data, item.Count);
                        }
                        else
                        {
                            _playerData.RuneInventory.Get(_runeDatabase.Get(item.ItemId), item.Count);
                        }
                        if (item.Id == item.ItemId && item is Rune runeItem)
                        {
                            registeredItem = new Rune(runeItem.Data, item.Count, Rune.GenerateInstanceId());
                        }
                        break;
                    default:
                        Debug.LogWarning($"[TradingPostService] {item.ItemId} ({item.Category}) 아이템이 유효하지 않습니다.");
                        break;
                }
                if (_bargainItems.TryGetValue(category, out var list))
                {
                    list.Add(registeredItem);
                }
                _bargainSimulateService.RegisterItem(registeredItem);
            }
            if (isEquippedWeaponRegistered)
            {
                _quickSellPublisher?.Publish(new QuickSellEvent(0));
            }
        }

        public IReadOnlyList<CustomerData> GetBargainCustomers(uint itemId)
        {
            return _bargainSimulateService.GetCustomers(itemId);
        }

        public int GetBargainCustomerCount(uint itemId)
        {
            return _bargainSimulateService.GetCustomerCount(itemId);
        }

        public bool RemoveBargainCustomer(uint itemId, CustomerData customer)
        {
            return _bargainSimulateService.RemoveCustomer(itemId, customer);
        }
        public Observable<(uint itemId, CustomerData customer)> OnCustomerArrived => _bargainSimulateService.OnCustomerArrived;
        public Observable<(uint itemId, CustomerData customer)> OnCustomerLeft => _bargainSimulateService.OnCustomerLeft;
        public IReadOnlyList<IShopItem> GetBargainItems(ItemCategory category)
        {
            if (_bargainItems.TryGetValue(category, out var list))
            {
                return list;
            }
            return Array.Empty<IShopItem>();
        }
        private void RemoveBargainItem(IShopItem item)
        {
            ShopItemCategory shopItemCategory = item.Category;
            var itemCategory = shopItemCategory switch
            {
                ShopItemCategory.Sword or ShopItemCategory.Bow => ItemCategory.Weapon,
                ShopItemCategory.Material => ItemCategory.Material,
                ShopItemCategory.Rune => ItemCategory.Rune,
                ShopItemCategory.Consumable => ItemCategory.Unknown,
                _ => ItemCategory.Unknown,
            };
            if (_bargainItems.TryGetValue(itemCategory, out var shopItems))
            {
                for (int i = shopItems.Count - 1; i >= 0; i--)
                {
                    if (shopItems[i].Id == item.Id)
                    {
                        shopItems.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public void Dispose()
        {
            _highestBids.Clear();
            _bargainSimulateService?.Clear();
            _disposables.Dispose();
        }
    }
}