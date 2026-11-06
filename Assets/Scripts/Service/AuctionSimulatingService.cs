using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public class AuctionSimulatingService
    {
        private readonly ItemTierService _itemTierService;
        private readonly SO_CustomerGroupDatabase _customerGroupDatabase;

        private readonly List<SO_CustomerGroup> _eligiblePool = new(16);
        private readonly List<float> _weightPool = new(16);
        private readonly List<SO_CustomerGroup> _selectedGroups = new(16);
        private const float WEIGHT_PREFERRED = 1.3f;
        private const float WEIGHT_DEFAULT = 1.0f;

        // 입찰자 기본 성향 범위
        private const float MIN_BIDDER_DESIRE = 0.6f;
        private const float MAX_BIDDER_DESIRE = 1.0f;
        private const float MIN_BIDDER_AGGRESSIVENESS = 0.8f;
        private const float MAX_BIDDER_AGGRESSIVENESS = 1.2f;

        // 선호 아이템 출품 시 성향 보정치
        private const float PREFERRED_DESIRE_MULTIPLIER = 1.15f;
        private const float PREFERRED_PRICE_RATING_MULTIPLIER = 1.10f;

        /// <summary>
        /// 경매 참여자 목록 (key: 물품 인스턴스 id, value: 참여자 목록)
        /// </summary>
        private readonly Dictionary<uint, List<AuctionBidder>> _bidders = new();
        private Queue<AuctionBidder> _bidQueue = new();

        public IReadOnlyDictionary<uint, List<AuctionBidder>> Bidders => _bidders;

        private Func<uint, ulong> _getCurrentPrice;
        private Action<uint, BidContext> _onBidAction;

        private readonly Dictionary<uint, CancellationTokenSource> _itemCts = new();

        public static readonly Color MutedCoral = new Color(0.90f, 0.54f, 0.52f);

        [Inject]
        public AuctionSimulatingService(
            ItemTierService itemTierService,
            SO_CustomerGroupDatabase customerGroupDatabase = null)
        {
            _itemTierService = itemTierService;
            _customerGroupDatabase = customerGroupDatabase;
        }

        /// <summary>
        /// 아이템의 티어에 따른 입찰자 수를 산출하고 입찰 세팅을 초기화합니다.
        /// </summary>
        public void InitBidSetting(IShopItem item)
        {
            if (item == null || _customerGroupDatabase == null) return;

            float tier = _itemTierService.GetNormalizedTier(item);
            int bidderCount = DetermineBidderCount(tier);


            List<ElementType> elementTypes = (item is Weapon weapon && weapon.Data != null) ? weapon.Data.elementTypes : null;


            _eligiblePool.Clear();
            _weightPool.Clear();

            var allGroups = _customerGroupDatabase.Items;
            for (int i = 0; i < allGroups.Count; i++)
            {
                var group = allGroups[i];
                if (group == null) continue;
                if (group.IsItemEligible(item.Category, elementTypes))
                {
                    _eligiblePool.Add(group);
                    float weight = group.IsItemPreferred(item.Category, elementTypes) ? WEIGHT_PREFERRED : WEIGHT_DEFAULT;
                    _weightPool.Add(weight);
                }
            }
            _selectedGroups.Clear();
            int pickCount = Mathf.Min(bidderCount, _eligiblePool.Count);
            for (int step = 0; step < pickCount; step++)
            {
                float totalWeight = 0f;
                for (int i = 0; i < _weightPool.Count; i++)
                {
                    totalWeight += _weightPool[i];
                }
                float roll = UnityEngine.Random.Range(0f, totalWeight);
                float accumulated = 0f;
                int chosenIndex = 0;

                for (int i = 0; i < _weightPool.Count; i++)
                {
                    accumulated += _weightPool[i];
                    if (roll <= accumulated)
                    {
                        chosenIndex = i;
                        break;
                    }
                }
                _selectedGroups.Add(_eligiblePool[chosenIndex]);

                //선택된 그룹은 풀에서 제거
                int lastIdx = _eligiblePool.Count - 1;
                _eligiblePool[chosenIndex] = _eligiblePool[lastIdx];
                _weightPool[chosenIndex] = _weightPool[lastIdx];
                _eligiblePool.RemoveAt(lastIdx);
                _weightPool.RemoveAt(lastIdx);
            }
            var bidderList = new List<AuctionBidder>(_selectedGroups.Count);
            for (int i = 0; i < _selectedGroups.Count; i++)
            {
                var group = _selectedGroups[i];
                int nameCount = group._customerNames != null ? group._customerNames.Count : 0;
                string rawName = nameCount > 0
                    ? group._customerNames[UnityEngine.Random.Range(0, nameCount)]
                    : group._customerGroupName;
                string fullName = string.IsNullOrEmpty(group._customerGroupTitle) ? rawName : $"{group._customerGroupTitle} {rawName}";

                float maxPriceRating = UnityEngine.Random.Range(group.minEstimateRatio, group.maxEstimateRatio);
                float bidIncrement = UnityEngine.Random.Range(group.minInterpolation, group.maxInterpolation);
                float desire = UnityEngine.Random.Range(MIN_BIDDER_DESIRE, MAX_BIDDER_DESIRE);
                float aggressiveness = UnityEngine.Random.Range(MIN_BIDDER_AGGRESSIVENESS, MAX_BIDDER_AGGRESSIVENESS);

                // 선호 아이템 출품 시 성향 가중치 부여
                if (group.IsItemPreferred(item.Category, elementTypes))
                {
                    desire = Mathf.Clamp01(desire * PREFERRED_DESIRE_MULTIPLIER);
                    maxPriceRating *= PREFERRED_PRICE_RATING_MULTIPLIER;
                }

                var bidder = new AuctionBidder(
                    name: fullName,
                    group: group,
                    maxPriceRating: maxPriceRating,
                    bidIncrementRating: bidIncrement,
                    desire: desire,
                    aggressiveness: aggressiveness
                );

                bidderList.Add(bidder);
            }

            RegistBidders(item.Id, bidderList);
            var cts = new CancellationTokenSource();
            _itemCts[item.Id] = cts;
            Simulate(item, cts.Token).Forget();
        }

        private int DetermineBidderCount(float tier)
        {
            if (tier < 0.35f) return UnityEngine.Random.Range(3, 5); // 3 ~ 4명
            if (tier < 0.70f) return UnityEngine.Random.Range(5, 7); // 5 ~ 6명
            return UnityEngine.Random.Range(7, 10);                   // 7 ~ 9명
        }

        public void RegistBidders(uint itemId, List<AuctionBidder> bidders)
        {
            _bidders[itemId] = bidders;
        }

        public void RemoveBidders(uint itemId)
        {
            _bidders.Remove(itemId);
            if (_itemCts.TryGetValue(itemId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _itemCts.Remove(itemId);
            }
        }
        public async UniTask Simulate(IShopItem item, CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                float minCooldown = GetMinCooldown(item.Id);
                if (minCooldown == float.MaxValue) break;
                bool isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(minCooldown), cancellationToken: ct).SuppressCancellationThrow();
                if (isCancelled) break;

                ulong basePrice = item.Value * (item.Count > 0 ? item.Count : 1u);
                EnqueueExpiredBidders(item.Id, minCooldown);
                while (_bidQueue.Count > 0)
                {
                    var bidder = _bidQueue.Dequeue();
                    ulong currentPrice = _getCurrentPrice != null ? _getCurrentPrice(item.Id) : 0;
                    if (bidder.TryEvaluateBid(currentPrice, basePrice, out ulong nextBidPrice))
                    {
                        _onBidAction?.Invoke(item.Id, new BidContext(bidder.Name, nextBidPrice));
                    }
                    bidder.ResetTimer();
                }
            }
        }
        public void SetAuctionHandler(Func<uint, ulong> getCurerentPrice, Action<uint, BidContext> onBidAction)
        {
            _getCurrentPrice = getCurerentPrice;
            _onBidAction = onBidAction;
        }
        private void EnqueueExpiredBidders(uint itemId, float minCooldown)
        {
            if (!_bidders.TryGetValue(itemId, out var bidders)) return;
            for (int i = 0; i < bidders.Count; i++)
            {
                var bidder = bidders[i];
                if (bidder.Tick(minCooldown))
                {
                    _bidQueue.Enqueue(bidder);
                }
            }
        }

        private float GetMinCooldown(uint itemId)
        {
            if (!_bidders.TryGetValue(itemId, out var bidders) || bidders.Count == 0)
            {
                return float.MaxValue;
            }
            float minCooldown = float.MaxValue;
            for (int i = 0; i < bidders.Count; i++)
            {
                var bidder = bidders[i];
                if (bidder.IsFolded) continue;
                if (bidder.RemainingSeconds < minCooldown)
                {
                    minCooldown = bidder.RemainingSeconds;
                }
            }
            return minCooldown;
        }
    }
}
