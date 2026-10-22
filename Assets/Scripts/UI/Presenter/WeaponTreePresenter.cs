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
        private readonly WeaponTreePathService _pathService;
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
            WeaponTreePathService pathService,
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
            _pathService = pathService;
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

                HandleNodeClick(e._recipeId);
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

        private void HandleNodeClick(uint clickedWeaponId)
        {
            var curWeaponId = _forgeManager.CurrentWeapon?.WeaponId ?? 10001;

            // 1. 현재 장착 무기 자체를 클릭한 경우: 핀 해제
            if (clickedWeaponId == curWeaponId)
            {
                ClearPinnedPath();
                _pinnedRecipeId = 0;
                _recipeFlagPublisher.Publish(new ChangeRecipeFlagEvent { _recipeId = 0 });
                return;
            }

            // 2. 이미 잡힌 경로가 있고 기존 핀이 존재하는 경우 (캐시 활용)
            if (path != null && path.Count > 0 && _pinnedRecipeId != 0)
            {
                // 2-1. 기존 경로에 이미 포함된 중간 노드를 클릭한 경우: 그 노드까지만 경로 축소
                int existingIndex = -1;
                for (int i = 0; i < path.Count; i++)
                {
                    if (path[i] == clickedWeaponId)
                    {
                        existingIndex = i;
                        break;
                    }
                }

                if (existingIndex >= 0)
                {
                    if (existingIndex == path.Count - 1) return; // 이미 목표 노드임

                    // existingIndex 이후 노드 및 브랜치 하이라이트 해제
                    for (int i = existingIndex + 1; i < path.Count; i++)
                    {
                        if (_nodes.TryGetValue(path[i], out var node))
                        {
                            node.SetNormalNode();
                        }
                    }
                    for (int i = existingIndex; i < path.Count - 1; i++)
                    {
                        if (_treeBuilder.Branches.TryGetValue((path[i], path[i + 1]), out var branch))
                        {
                            branch.SetHighlight(false);
                        }
                    }

                    // 경로 축소
                    path.RemoveRange(existingIndex + 1, path.Count - (existingIndex + 1));
                    _pinnedRecipeId = clickedWeaponId;

                    if (_nodes.TryGetValue(clickedWeaponId, out var newDestNode))
                    {
                        newDestNode.SetDestinationNode();
                    }

                    _recipeFlagPublisher.Publish(new ChangeRecipeFlagEvent { _recipeId = clickedWeaponId });
                    return;
                }

                // 2-2. 기존 핀 목표 노드(_pinnedRecipeId)에서 새로 클릭한 노드로 연장 가능한 경우
                var extension = _pathService.FindPath(_pinnedRecipeId, clickedWeaponId);
                if (extension != null && extension.Count >= 2)
                {
                    // 이전 최종 목적지 노드를 RouteNode로 변경
                    if (_nodes.TryGetValue(_pinnedRecipeId, out var prevDestNode))
                    {
                        prevDestNode.SetRouteNode();
                    }

                    // 연장 구간 브랜치 및 노드 하이라이트
                    for (int i = 0; i < extension.Count - 1; i++)
                    {
                        uint from = extension[i];
                        uint to = extension[i + 1];

                        if (_treeBuilder.Branches.TryGetValue((from, to), out var branch))
                        {
                            branch.SetHighlight(true);
                        }

                        if (i + 1 < extension.Count - 1)
                        {
                            if (_nodes.TryGetValue(to, out var midNode))
                            {
                                midNode.SetRouteNode();
                            }
                        }
                    }

                    for (int i = 1; i < extension.Count; i++)
                    {
                        path.Add(extension[i]);
                    }

                    _pinnedRecipeId = clickedWeaponId;
                    if (_nodes.TryGetValue(clickedWeaponId, out var finalDestNode))
                    {
                        finalDestNode.SetDestinationNode();
                    }

                    _recipeFlagPublisher.Publish(new ChangeRecipeFlagEvent { _recipeId = clickedWeaponId });
                    return;
                }
            }

            // 3. 기존 경로가 없거나, 연장할 수 없는 다른 노드를 클릭한 경우: 현재 무기부터 새로 경로 탐색
            RerouteFrom(curWeaponId, clickedWeaponId);
        }

        private void RerouteFrom(uint fromWeaponId, uint targetWeaponId)
        {
            ClearPinnedPath();
            _pinnedRecipeId = targetWeaponId;

            path = _pathService.FindPath(fromWeaponId, targetWeaponId);
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
            var rootWeapon = _weaponDatabase.Get(10001);
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
                //테스트용 해금
                // if (node.WeaponData != null && node.WeaponData.recipes != null)
                // {
                //     foreach (var recipe in node.WeaponData.recipes)
                //     {
                //         if (recipe != null)
                //         {
                //             _recipeUnlockService.Unlock(recipe.Id);
                //         }
                //     }
                // }
                // node.SetNormalNode();
                // continue;
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

            var fullPath = _pathService.FindPath(10001, targetWeaponId);
            if (fullPath == null || fullPath.Count < 2) return false;

            for (int i = 0; i < fullPath.Count - 1; i++)
            {
                uint fromId = fullPath[i];
                uint toId = fullPath[i + 1];

                SO_WeaponData fromWeapon = _weaponDatabase.Get(fromId);
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