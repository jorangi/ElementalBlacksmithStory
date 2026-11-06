using System;
using System.Collections.Generic;
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

        [Inject]
        public TradingPostService(
            PlayerData playerData,
            AuctionSimulatingService auctionSimulateService,
            IPublisher<AcquireItemEvent> acquireItemPublisher,
            SO_MaterialDatabase materialDatabase,
            SO_RuneDatabase runeDatabase
        )
        {
            _playerData = playerData;
            _acquireItemPublisher = acquireItemPublisher;
            _materialDatabase = materialDatabase;
            _runeDatabase = runeDatabase;
            _auctionSimulateService = auctionSimulateService;

            _auctionSimulateService.SetAuctionHandler(
                getCurerentPrice: itemId => _highestBids.TryGetValue(itemId, out var bid) ? bid.Price : 0,
                onBidAction: (itemId, bidContext) => Bid(itemId, bidContext)
            );

            _bargainItems[ItemCategory.Weapon] = new List<IShopItem>();
            _bargainItems[ItemCategory.Material] = new List<IShopItem>();
            _bargainItems[ItemCategory.Rune] = new List<IShopItem>();
        }
        public void AddItem(IShopItem item, BidContext bidContext)
        {
            _auctionItems[item.Id] = item;
            _highestBids[item.Id] = bidContext;
            _auctionSimulateService.InitBidSetting(item);
        }
        public void RemoveItem(uint itemId)
        {
            _highestBids.Remove(itemId);
            _auctionItems.Remove(itemId);
            _playerBids.Remove(itemId);
            _auctionSimulateService.RemoveBidders(itemId);
        }
        public void ClearItem()
        {
            ClearAuction();
        }
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
        public void RegisterBargainItems(IReadOnlyList<IShopItem> items)
        {
            if (items == null)
            {
                Debug.LogWarning("[TradingPostService] 등록할 아이템이 없습니다.");
                return;
            }
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
                        _playerData.EquipmentInventory.Remove(item.Id);
                        break;
                    case ShopItemCategory.Material:
                        category = ItemCategory.Material;
                        _playerData.MaterialInventory.Get(_materialDatabase.Get(item.ItemId), item.Count);
                        if (item.Id == item.ItemId && item is MaterialShopItem matItem)
                        {
                            registeredItem = new MaterialShopItem(matItem.Data, item.Count, MaterialShopItem.GenerateInstanceId());
                        }
                        break;
                    case ShopItemCategory.Rune:
                        category = ItemCategory.Rune;
                        _playerData.RuneInventory.Get(_runeDatabase.Get(item.ItemId), item.Count);
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
            }
        }
        public IReadOnlyList<IShopItem> GetBargainItems(ItemCategory category)
        {
            if (_bargainItems.TryGetValue(category, out var list))
            {
                return list;
            }
            return Array.Empty<IShopItem>();
        }
        public void Dispose()
        {
            _highestBids.Clear();
            _disposables.Dispose();
        }
    }
}