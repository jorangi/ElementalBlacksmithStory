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
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{

    public class WeaponTreePresenter : IStartable, IDisposable
    {
        private readonly WeaponTreeBuilder _treeBuilder;
        private readonly WeaponSpriteLoader _spriteLoader;
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly IPublisher<ChangeRecipeFlagEvent> _recipeFlagPublisher;
        private readonly RecipeUnlockService _recipeUnlockService;
        private readonly EnhancementService _enhancementService;
        private readonly CompositeDisposable _disposables = new();
        private Dictionary<uint, WeaponNodeUI> _nodes = new();
        private ForgeManager _forgeManager;
        private List<uint> path;
        private uint _pinnedRecipeId;
        public bool IsActivated => _treeBuilder.IsActivated;
        public void Hide() => _treeBuilder.Hide();

        [Inject]
        public WeaponTreePresenter(
            ForgeManager forgeManager,
            WeaponTreeBuilder treeBuilder,
            WeaponSpriteLoader spriteLoader,
            SO_WeaponDatabase weaponDatabase,
            RecipeUnlockService recipeUnlockService,
            IPublisher<ChangeRecipeFlagEvent> recipeFlagPublisher,
            ISubscriber<EnhanceButtonPositionEvent> positionSubscriber,
            EnhancementService enhancementService,
            ISubscriber<ChangeWeaponEvent> weaponChangeSubscriber
            )
        {
            _forgeManager = forgeManager;
            _treeBuilder = treeBuilder;
            _spriteLoader = spriteLoader;
            _weaponDatabase = weaponDatabase;
            _recipeUnlockService = recipeUnlockService;
            _recipeFlagPublisher = recipeFlagPublisher;
            _enhancementService = enhancementService;

            // 레시피 고정 토글 동기화
            _enhancementService.FixedRecipe = _treeBuilder.IsFixRecipeChecked;
            _treeBuilder.OnFixRecipeChangedAsObservable.Subscribe(isOn =>
            {
                _enhancementService.FixedRecipe = isOn;
                Debug.Log($"[WeaponTreePresenter] 토글 OnValueChanged 수신됨 -> isOn: {isOn}, 현재 _pinnedRecipeId: {_pinnedRecipeId}");
                if (isOn && _pinnedRecipeId != 0)
                {
                    var curWeaponId = _forgeManager.CurrentWeapon?.WeaponId ?? 10001;
                    RerouteFrom(curWeaponId, _pinnedRecipeId);
                }
            }).AddTo(_disposables);

            // 무기 변경 시: 바뀐 무기에서 선택한 무기까지의 루트를 탐색/리루트
            weaponChangeSubscriber.Subscribe(e =>
            {
                HandleWeaponChanged(e.WeaponId);
            }).AddTo(_disposables);

            // 화면 스크롤 동기화
            positionSubscriber.Subscribe(e =>
            {
                if (e.positionY <= -1000f)
                {
                    _treeBuilder.HidingButtonAnimation().Forget();
                }
                else if (Mathf.Approximately(e.positionY, 0f))
                {
                    _treeBuilder.ShowingButtonAnimation().Forget();
                }
                else
                {
                    _treeBuilder.SyncButtonYPosition(e.positionY);
                }
            }).AddTo(_disposables);

            // 레시피 해금 시 노드 잠금 해제 갱신
            _recipeUnlockService.OnRecipeUnlockedAsObservable.Subscribe(_ =>
            {
                RefreshAllNodeLockStates();
            }).AddTo(_disposables);

            // 레시피 변경 동기화
            _treeBuilder.OnChangeRecipeFlagEventAsObservable.Subscribe(e =>
            {
                // 클릭한 대상 노드가 해금 가능한 상태(루트로부터 모든 경로 단계가 해금됨)인지 검사
                if (!IsNodeFullyUnlocked(e._recipeId))
                {
                    Debug.LogWarning($"[WeaponTreePresenter] 무기 ID {e._recipeId}는 이전 단계 레시피가 모두 해금되지 않아 선택할 수 없습니다.");
                    return;
                }

                // 현재 들고 있는 무기 노드를 선택한 경우: 핀 경로 해제 및 자유 재료 조합 모드로 전환
                if (_forgeManager.CurrentWeapon != null && e._recipeId == _forgeManager.CurrentWeapon.WeaponId)
                {
                    ClearPinnedPath();
                    _pinnedRecipeId = 0;
                    _recipeFlagPublisher.Publish(new ChangeRecipeFlagEvent { _recipeId = 0 });
                    return;
                }

                RerouteFrom(_forgeManager.CurrentWeapon.WeaponId, e._recipeId);
            }).AddTo(_disposables);
        }

        private void HandleWeaponChanged(uint currentWeaponId)
        {
            Debug.Log($"[WeaponTreePresenter] HandleWeaponChanged 수신 -> currentWeaponId: {currentWeaponId}, _pinnedRecipeId: {_pinnedRecipeId}, FixedRecipe: {_enhancementService.FixedRecipe}");

            // 핀 찍은 레시피가 없는 경우
            if (_pinnedRecipeId == 0)
            {
                ClearPinnedPath();
                return;
            }

            // 바뀐 무기가 이미 핀 목표 노드에 도달한 경우
            if (currentWeaponId == _pinnedRecipeId)
            {
                if (!_enhancementService.FixedRecipe)
                {
                    ClearPinnedPath();
                    _pinnedRecipeId = 0;
                }
                else
                {
                    ClearPinnedPath();
                    if (_nodes.TryGetValue(_pinnedRecipeId, out var dNode))
                    {
                        dNode.SetDestinationNode();
                    }
                }
                return;
            }

            // 바뀐 무기에서 선택한 목표 무기까지의 루트를 탐색
            RerouteFrom(currentWeaponId, _pinnedRecipeId);
        }

        private void ClearPinnedPath()
        {
            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    uint weaponId = path[i];
                    if (_nodes.TryGetValue(weaponId, out var node))
                    {
                        if (IsNodeFullyUnlocked(weaponId))
                        {
                            node.SetNormalNode();
                        }
                        else
                        {
                            node.SetLockedNode();
                        }
                    }

                    if (i < path.Count - 1)
                    {
                        if (_treeBuilder.Branches.TryGetValue((path[i], path[i + 1]), out var branch))
                        {
                            branch.SetHighlight(false);
                        }
                    }
                }
                path = null;
            }
        }

        private void RerouteFrom(uint fromWeaponId, uint targetWeaponId)
        {
            ClearPinnedPath();
            _pinnedRecipeId = targetWeaponId;

            path = WeaponTreePathFinder.FindPath(fromWeaponId, targetWeaponId, _weaponDatabase);
            if (path == null) return;

            for (int i = 0; i < path.Count - 1; i++)
            {
                if (_nodes.TryGetValue(path[i], out var rNode))
                {
                    rNode.SetRouteNode();
                }
                if (_treeBuilder.Branches.TryGetValue((path[i], path[i + 1]), out var branch))
                {
                    branch.SetHighlight(true);
                }
            }
            if (_nodes.TryGetValue(path[^1], out var dNode))
            {
                dNode.SetDestinationNode();
            }
            _recipeFlagPublisher.Publish(new ChangeRecipeFlagEvent { _recipeId = targetWeaponId });
        }

        public void Start()
        {
            var rootWeapon = _weaponDatabase.GetWeapon(10001);
            if (rootWeapon != null)
            {
                _nodes = _treeBuilder.GenerateTree(rootWeapon);
                RefreshAllNodeLockStates();
            }
        }

        /// <summary>
        /// 모든 노드의 해금 상태를 반영하여 Normal/Locked 적용
        /// </summary>
        private void RefreshAllNodeLockStates()
        {
            foreach (var kvp in _nodes)
            {
                uint weaponId = kvp.Key;
                WeaponNodeUI node = kvp.Value;
                if (IsNodeFullyUnlocked(weaponId))
                {
                    node.SetNormalNode();
                }
                else
                {
                    node.SetLockedNode();
                }
            }
        }

        /// <summary>
        /// 루트 무기(10001)부터 targetWeaponId까지의 경로 상 모든 레시피가 해금되어 있는지 검사
        /// </summary>
        private bool IsNodeFullyUnlocked(uint targetWeaponId)
        {
            if (targetWeaponId == 10001) return true;

            var fullPath = WeaponTreePathFinder.FindPath(10001, targetWeaponId, _weaponDatabase);
            if (fullPath == null || fullPath.Count < 2) return false;

            for (int i = 0; i < fullPath.Count - 1; i++)
            {
                uint fromId = fullPath[i];
                uint toId = fullPath[i + 1];

                SO_WeaponData fromWeapon = _weaponDatabase.GetWeapon(fromId);
                if (fromWeapon == null || fromWeapon.recipes == null) return false;

                bool stepUnlocked = false;
                foreach (var recipe in fromWeapon.recipes)
                {
                    if (recipe != null && recipe.recipeOutcome.resultWeapon != null && recipe.recipeOutcome.resultWeapon.Id == toId)
                    {
                        if (_recipeUnlockService.IsUnlocked(recipe.Id))
                        {
                            stepUnlocked = true;
                            break;
                        }
                    }
                }

                if (!stepUnlocked)
                {
                    return false;
                }
            }

            return true;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}