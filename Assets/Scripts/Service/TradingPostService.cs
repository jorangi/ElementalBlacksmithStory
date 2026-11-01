using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using ObservableCollections;
using R3;
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
        private readonly IPublisher<AcquireItemEvent> _acquireItemPublisher;
        private readonly Dictionary<ItemCategory, List<IShopItem>> _bargainItems = new();

        [Inject]
        public TradingPostService(
            PlayerData playerData,
            IPublisher<AcquireItemEvent> acquireItemPublisher
        )
        {
            _playerData = playerData;
            _acquireItemPublisher = acquireItemPublisher;

            _bargainItems[ItemCategory.Weapon] = new List<IShopItem>();
            _bargainItems[ItemCategory.Material] = new List<IShopItem>();
            _bargainItems[ItemCategory.Rune] = new List<IShopItem>();
        }
        public void AddItem(IShopItem item, BidContext bidContext)
        {
            _auctionItems[item.Id] = item;
            _highestBids[item.Id] = bidContext;
        }
        public void RemoveItem(uint itemId)
        {
            _highestBids.Remove(itemId);
        }
        public void ClearItem()
        {
            _highestBids.Clear();
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
                IShopItem item = _auctionItems[targetId];
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
                Debug.LogWarning($"[TradingPostService] {targetId} 아이템 낙찰에 성공했습니다. 낙찰액: {playerSpent}");
            }
            else if (playerSpent > 0)
            {
                _playerData.AddMoney(playerSpent);
                Debug.LogWarning($"[TradingPostService] {targetId} 아이템 낙찰에 실패했습니다. 입찰금: {playerSpent}, 낙찰액: {highestBid}");
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
                ItemCategory category = ItemIdHelper.GetCategory(item.ItemId);
                if (_bargainItems.TryGetValue(category, out var list))
                {
                    list.Add(item);
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