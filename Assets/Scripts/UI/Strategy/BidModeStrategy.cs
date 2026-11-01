using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.UI.Strategy
{
    /// <summary>
    /// 교역소(TradingPost) 경매 입찰 모드의 출품 추첨, 묶음 산출, 시작가 책정 및 상호작용 전략
    /// </summary>
    public class BidModeStrategy : IShopModeStrategy
    {
        public const float MIN_STARTING_BID_RATIO = 0.02f; // 표준가의 최소 2%
        public const float MAX_STARTING_BID_RATIO = 0.40f; // 표준가의 최대 40%

        public const int MIN_AUCTION_ITEM_COUNT = 6;       // 회차당 최소 출품 개수
        public const int MAX_AUCTION_ITEM_COUNT = 10;      // 회차당 최대 출품 개수

        public const uint MIN_MATERIAL_BUNDLE_COUNT = 1;     // 최소 1개
        public const uint MAX_MATERIAL_BUNDLE_COUNT = 200;   // 하급 재료 최대 200개

        public const float TOP_MATERIAL_AVG_MIN_COUNT = 3.0f; // 초반 최상급 재료 기준 평균 개수
        public const float TOP_MATERIAL_AVG_MAX_COUNT = 5.0f; // 후반 최상급 재료 기준 평균 개수

        public const float BUNDLE_LUCK_MIN_MULTIPLIER = 0.35f; // 최소 운 계수 (1개까지 하락 가능)
        public const float BUNDLE_LUCK_MAX_MULTIPLIER = 2.20f; // 최대 대박 계수 (최상급 재료도 운 좋으면 10개 등장)

        public const float MIN_CANDIDATE_WEIGHT = 0.05f;
        public const float TIER_DISTANCE_PENALTY = 1.35f;
        public const float ENDGAME_LOW_TIER_PENALTY_MULTIPLIER = 0.15f;

        /// <summary>
        /// 현재 모루 위 무기의 직속 파생 레시피에서 부족한 재료에 부여하는 은근한 가중치 배율 (+25%)
        /// </summary>
        public const float CURRENT_BRANCH_DEFICIT_WEIGHT_MULTIPLIER = 1.25f;

        /// <summary>
        /// 현재 모루 위 무기의 차상위(2단계 위) 브랜치 레시피에 필요한 재료에 부여하는 은근한 가중치 배율 (+15%)
        /// </summary>
        public const float UPPER_BRANCH_NEEDED_WEIGHT_MULTIPLIER = 1.15f;

        private readonly struct AuctionCandidate
        {
            public readonly uint ItemId;
            public readonly ItemCategory Category;
            public readonly ulong UnitValue;
            public readonly float Weight;

            public AuctionCandidate(uint itemId, ItemCategory category, ulong unitValue, float weight)
            {
                ItemId = itemId;
                Category = category;
                UnitValue = unitValue;
                Weight = weight;
            }
        }

        private readonly BidPresenter _presenter;
        private readonly TradingPostService _tradingPostService;
        private readonly IPlayerProgressionService _progressionService;
        private readonly IItemShopFactory _itemShopFactory;
        private readonly PlayerData _playerData;
        private readonly ForgeManager _forgeManager;
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly SO_MaterialDatabase _materialDatabase;
        private readonly SO_RuneDatabase _runeDatabase;

        private readonly List<IShopItem> _bidGoods = new(16);
        private readonly List<AuctionCandidate> _candidateBuffer = new(128);
        private readonly HashSet<uint> _currentBranchDeficitMatIds = new(16);
        private readonly HashSet<uint> _upperBranchMatIds = new(32);

        private ulong _cachedMaxWeaponValue;
        private ulong _cachedMaxMaterialValue;
        private ulong _cachedMaxRuneValue;

        public bool CanRefresh => false;
        [Inject]
        public BidModeStrategy(
            BidPresenter presenter,
            TradingPostService tradingPostService = null,
            IPlayerProgressionService progressionService = null,
            IItemShopFactory itemShopFactory = null,
            PlayerData playerData = null,
            ForgeManager forgeManager = null,
            SO_WeaponDatabase weaponDatabase = null,
            SO_MaterialDatabase materialDatabase = null,
            SO_RuneDatabase runeDatabase = null
        )
        {
            _presenter = presenter;
            _tradingPostService = tradingPostService;
            _progressionService = progressionService;
            _itemShopFactory = itemShopFactory;
            _playerData = playerData;
            _forgeManager = forgeManager;
            _weaponDatabase = weaponDatabase;
            _materialDatabase = materialDatabase;
            _runeDatabase = runeDatabase;
        }

        public ulong GetPrice(IShopItem item)
        {
            if (item == null) return 0;

            if (_tradingPostService != null && _tradingPostService.HighestBids.TryGetValue(item.Id, out var bid))
            {
                return bid.Price;
            }

            ulong startPrice = CalculateStartingBid(item);
            _tradingPostService?.AddItem(item, new BidContext(string.Empty, startPrice));
            return startPrice;
        }

        /// <summary>
        /// 경매에 입찰하는 것 자체에는 Interact가 가능함
        /// </summary>
        public bool CanInteract(IShopItem item) => item != null;

        /// <summary>
        /// 경매 품목은 통째로 입찰/낙찰받는 고정 묶음(번들) 단위이므로 item.Count를 번들 수량으로 반환
        /// </summary>
        public uint GetBundleCount(IShopItem item) => item != null && item.Count > 0 ? item.Count : 1u;

        /// <summary>
        /// 아이템 클릭 시 경매 입찰 모달을 오픈
        /// </summary>
        public void OnItemClicked(IShopItem item)
        {
            if (item == null || _presenter == null) return;
            _presenter.SetItem(item).Forget();
            _presenter.Show();
        }

        public UniTask GetDisplayItemAsync(List<IShopItem> results, bool isSellMode, CancellationToken ct = default)
        {
            results.Clear();
            if (isSellMode) return UniTask.CompletedTask;

            if (_bidGoods.Count == 0)
            {
                RefreshAuctionItems();
            }

            for (int i = 0; i < _bidGoods.Count; i++)
            {
                results.Add(_bidGoods[i]);
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 새로운 경매 회차의 출품 목록(6~10개)을 플레이어 진행도 및 제작 브랜치 상황에 맞춰 추첨하고 시작가를 등록
        /// </summary>
        public void RefreshAuctionItems()
        {
            _bidGoods.Clear();
            _tradingPostService?.ClearItem();

            if (_itemShopFactory == null) return;

            EnsureDatabaseMaxValuesCached();
            CollectBranchMaterialNeeds();

            float progression = _progressionService != null
                ? _progressionService.GetProgressionRatio()
                : 0.1f;

            BuildCandidatePool(progression);

            if (_candidateBuffer.Count == 0) return;

            int pickCount = UnityEngine.Random.Range(MIN_AUCTION_ITEM_COUNT, MAX_AUCTION_ITEM_COUNT + 1);
            if (pickCount > _candidateBuffer.Count)
            {
                pickCount = _candidateBuffer.Count;
            }

            for (int i = 0; i < pickCount; i++)
            {
                int selectedIndex = PickWeightedIndex(_candidateBuffer);
                if (selectedIndex < 0) break;

                AuctionCandidate picked = _candidateBuffer[selectedIndex];

                // 중복 추첨 방지를 위해 선택된 항목을 마지막 원소와 스왑 후 제거 (O(1), 무할당)
                int lastIndex = _candidateBuffer.Count - 1;
                _candidateBuffer[selectedIndex] = _candidateBuffer[lastIndex];
                _candidateBuffer.RemoveAt(lastIndex);

                uint stock = 1;
                if (picked.Category == ItemCategory.Material)
                {
                    stock = CalculateMaterialBundleCount(picked.UnitValue, progression);
                }

                IShopItem shopItem = _itemShopFactory.Create(picked.ItemId, stock, 1.0f);
                if (shopItem == null) continue;

                ulong startPrice = CalculateStartingBid(shopItem);
                _tradingPostService?.AddItem(shopItem, new BidContext(string.Empty, startPrice));
                _bidGoods.Add(shopItem);
            }
        }

        /// <summary>
        /// 아이템 표준 가치(재료의 경우 단가 * 묶음 수량)의 2% ~ 40% 사이로 시작 입찰가를 책정
        /// </summary>
        public static ulong CalculateStartingBid(IShopItem item)
        {
            if (item == null) return 0;

            uint count = item.Count > 0 ? item.Count : 1u;
            ulong standardTotalValue = item.Value * count;
            float ratio = UnityEngine.Random.Range(MIN_STARTING_BID_RATIO, MAX_STARTING_BID_RATIO);

            return (ulong)Math.Max(1.0, Math.Round(standardTotalValue * ratio));
        }

        /// <summary>
        /// 재료 단가에 반비례하고 DB 최고가 재료 및 진행도에 연동되는 동적 묶음 수량(1~200개)을 계산
        /// </summary>
        private uint CalculateMaterialBundleCount(ulong materialUnitValue, float progression)
        {
            ulong safeUnitValue = materialUnitValue > 0 ? materialUnitValue : 1UL;
            ulong maxMatValue = _cachedMaxMaterialValue > 0 ? _cachedMaxMaterialValue : safeUnitValue;

            // 진행도에 따라 최상급 재료의 기준 목표 개수를 3.0 ~ 5.0개로 설정
            float topMatTargetCount = Mathf.Lerp(TOP_MATERIAL_AVG_MIN_COUNT, TOP_MATERIAL_AVG_MAX_COUNT, progression);
            float dynamicTargetBudget = maxMatValue * topMatTargetCount;

            // 제곱 분포(t^2)를 적용하여 평소에는 평균치(3~5개) 부근이 자주 뜨고, 운이 좋으면 최대 2.2배(약 10개)까지 등장
            float t = UnityEngine.Random.value;
            float luckMultiplier = Mathf.Lerp(BUNDLE_LUCK_MIN_MULTIPLIER, BUNDLE_LUCK_MAX_MULTIPLIER, t * t);

            float rawCount = (dynamicTargetBudget / safeUnitValue) * luckMultiplier;
            int clamped = Mathf.Clamp(
                Mathf.RoundToInt(rawCount),
                (int)MIN_MATERIAL_BUNDLE_COUNT,
                (int)MAX_MATERIAL_BUNDLE_COUNT);

            return (uint)clamped;
        }

        /// <summary>
        /// 현재 모루 위 무기의 직속 파생 레시피(부족한 재료)와 차상위 티어 레시피에 필요한 재료 ID를 수집
        /// </summary>
        private void CollectBranchMaterialNeeds()
        {
            _currentBranchDeficitMatIds.Clear();
            _upperBranchMatIds.Clear();

            Weapon currentWeapon = _forgeManager != null ? _forgeManager.CurrentWeapon : null;
            if (currentWeapon == null || currentWeapon.Data == null || currentWeapon.Data.recipes == null)
            {
                return;
            }

            var directRecipes = currentWeapon.Data.recipes;
            for (int i = 0; i < directRecipes.Count; i++)
            {
                SO_CraftRecipe recipe = directRecipes[i];
                if (recipe == null) continue;

                // 현재 단계에서 바로 다음 무기로 갈 때 부족한 재료 파악
                if (recipe.recipeMaterials != null)
                {
                    for (int m = 0; m < recipe.recipeMaterials.Count; m++)
                    {
                        RecipeMaterial req = recipe.recipeMaterials[m];
                        if (req.material == null) continue;

                        uint owned = _playerData != null && _playerData.MaterialInventory != null
                            ? _playerData.MaterialInventory.GetCount(req.material)
                            : 0;

                        if (owned < req.count)
                        {
                            _currentBranchDeficitMatIds.Add(req.material.Id);
                        }
                    }
                }

                //해당 브랜치의 상위 티어(다음 결과 무기의 파생 레시피들)에 필요한 재료 파악
                SO_WeaponData nextWeapon = recipe.recipeOutcome.resultWeapon;
                if (nextWeapon != null && nextWeapon.recipes != null)
                {
                    var upperRecipes = nextWeapon.recipes;
                    for (int u = 0; u < upperRecipes.Count; u++)
                    {
                        SO_CraftRecipe upperRecipe = upperRecipes[u];
                        if (upperRecipe == null || upperRecipe.recipeMaterials == null) continue;

                        for (int um = 0; um < upperRecipe.recipeMaterials.Count; um++)
                        {
                            RecipeMaterial upperReq = upperRecipe.recipeMaterials[um];
                            if (upperReq.material != null)
                            {
                                _upperBranchMatIds.Add(upperReq.material.Id);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 무기, 재료, 룬 DB의 모든 거래 가능 품목을 순회하며 플레이어 진행도 및 재료 니즈가 반영된 가중치 풀을 구성합니다.
        /// </summary>
        private void BuildCandidatePool(float progression)
        {
            _candidateBuffer.Clear();

            // 무기 풀 추가
            if (_weaponDatabase != null && _weaponDatabase.Items != null)
            {
                var weapons = _weaponDatabase.Items;
                for (int i = 0; i < weapons.Count; i++)
                {
                    SO_WeaponData wep = weapons[i];
                    if (wep == null || wep.basePrice == 0) continue;

                    float tier = CalculateNormalizedLogTier(wep.basePrice, _cachedMaxWeaponValue);
                    float weight = CalculateProgressionTierWeight(progression, tier);
                    _candidateBuffer.Add(new AuctionCandidate(wep.Id, ItemCategory.Weapon, wep.basePrice, weight));
                }
            }

            // 재료 풀 추가 (현재 브랜치 부족 재료 및 상위 티어 재료에 은근한 가중치 보정 적용)
            if (_materialDatabase != null && _materialDatabase.Items != null)
            {
                var materials = _materialDatabase.Items;
                for (int i = 0; i < materials.Count; i++)
                {
                    SO_MaterialData mat = materials[i];
                    if (mat == null || mat.Value == 0) continue;

                    float tier = CalculateNormalizedLogTier(mat.Value, _cachedMaxMaterialValue);
                    float weight = CalculateProgressionTierWeight(progression, tier);

                    if (_currentBranchDeficitMatIds.Contains(mat.Id))
                    {
                        weight *= CURRENT_BRANCH_DEFICIT_WEIGHT_MULTIPLIER;
                    }
                    else if (_upperBranchMatIds.Contains(mat.Id))
                    {
                        weight *= UPPER_BRANCH_NEEDED_WEIGHT_MULTIPLIER;
                    }

                    _candidateBuffer.Add(new AuctionCandidate(mat.Id, ItemCategory.Material, mat.Value, weight));
                }
            }

            // 룬 풀 추가
            if (_runeDatabase != null && _runeDatabase.Items != null)
            {
                var runes = _runeDatabase.Items;
                for (int i = 0; i < runes.Count; i++)
                {
                    SO_RuneData rune = runes[i];
                    if (rune == null || rune.Value == 0) continue;

                    float tier = CalculateNormalizedLogTier(rune.Value, _cachedMaxRuneValue);
                    float weight = CalculateProgressionTierWeight(progression, tier);
                    _candidateBuffer.Add(new AuctionCandidate(rune.Id, ItemCategory.Rune, rune.Value, weight));
                }
            }
        }

        private static float CalculateNormalizedLogTier(ulong value, ulong maxValue)
        {
            if (value <= 1 || maxValue <= 1) return 0f;
            double logVal = Math.Log10(value);
            double logMax = Math.Log10(maxValue);
            if (logMax <= 0.0001) return 0f;
            return Mathf.Clamp01((float)(logVal / logMax));
        }

        private static float CalculateProgressionTierWeight(float progression, float itemTier)
        {
            float distance = Mathf.Abs(progression - itemTier);
            float weight = Mathf.Max(MIN_CANDIDATE_WEIGHT, 1.0f - distance * TIER_DISTANCE_PENALTY);

            // 극후반 플레이어(진행도 70% 이상)에게는 초반 저가치 품목(하위 25% 미만)의 등장 확률을 대폭 억제
            if (progression >= 0.70f && itemTier < 0.25f)
            {
                weight *= ENDGAME_LOW_TIER_PENALTY_MULTIPLIER;
            }

            return weight;
        }

        private static int PickWeightedIndex(List<AuctionCandidate> candidates)
        {
            float totalWeight = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                totalWeight += candidates[i].Weight;
            }

            if (totalWeight <= 0f) return candidates.Count > 0 ? 0 : -1;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += candidates[i].Weight;
                if (roll <= cumulative)
                {
                    return i;
                }
            }

            return candidates.Count - 1;
        }

        private void EnsureDatabaseMaxValuesCached()
        {
            if (_cachedMaxWeaponValue == 0 && _weaponDatabase != null && _weaponDatabase.Items != null)
            {
                var list = _weaponDatabase.Items;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && list[i].basePrice > _cachedMaxWeaponValue)
                    {
                        _cachedMaxWeaponValue = list[i].basePrice;
                    }
                }
            }

            if (_cachedMaxMaterialValue == 0 && _materialDatabase != null && _materialDatabase.Items != null)
            {
                var list = _materialDatabase.Items;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && list[i].Value > _cachedMaxMaterialValue)
                    {
                        _cachedMaxMaterialValue = list[i].Value;
                    }
                }
            }

            if (_cachedMaxRuneValue == 0 && _runeDatabase != null && _runeDatabase.Items != null)
            {
                var list = _runeDatabase.Items;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && list[i].Value > _cachedMaxRuneValue)
                    {
                        _cachedMaxRuneValue = list[i].Value;
                    }
                }
            }
        }
    }
}