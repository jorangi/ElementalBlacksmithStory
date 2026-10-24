using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using ObservableCollections;
using R3;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class TradingPostService : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly PlayerData _playerData;
        private readonly ObservableDictionary<uint, BidContext> _highestBids = new();
        public ObservableDictionary<uint, BidContext> HighestBids => _highestBids;

        [Inject]
        public TradingPostService(PlayerData playerData)
        {
            _playerData = playerData;
        }
        public void AddItem(uint itemId, BidContext bidContext)
        {
            _highestBids[itemId] = bidContext;
        }
        public void RemoveItem(uint itemId)
        {
            _highestBids.Remove(itemId);
        }
        public void ClearItem()
        {
            _highestBids.Clear();
        }
        public void Bid(uint targetId, BidContext bidContext)
        {
            if (!_highestBids.TryGetValue(targetId, out var current) || current.Price < bidContext.Price)
            {
                AddItem(targetId, bidContext);
            }
        }
        public void Dispose()
        {
            _highestBids.Clear();
            _disposables.Dispose();
        }
    }
}