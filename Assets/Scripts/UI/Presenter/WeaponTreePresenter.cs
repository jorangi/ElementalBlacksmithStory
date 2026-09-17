using UnityEngine;
using R3;
using System;
using MessagePipe;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using System.Threading;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using System.Collections.Generic;
using ElementalBlacksmithStory.Core;

namespace ElementalBlacksmithStory.UI
{

    public class WeaponTreePresenter : IStartable, IDisposable
    {
        private readonly WeaponTreeBuilder _treeBuilder;
        private readonly WeaponSpriteLoader _spriteLoader;
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly IPublisher<ChangeRecipeFlagEvent> _recipeFlagPublisher;
        private readonly RecipeUnlockService _recipeUnlockService;
        private readonly CompositeDisposable _disposables = new();
        private Dictionary<uint, WeaponNodeUI> _nodes = new();
        private ForgeManager _forgeManager;
        List<uint> path;
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
            ISubscriber<EnhanceButtonPositionEvent> positionSubscriber
            )
        {
            _forgeManager = forgeManager;
            _treeBuilder = treeBuilder;
            _spriteLoader = spriteLoader;
            _weaponDatabase = weaponDatabase;
            _recipeUnlockService = recipeUnlockService;
            _recipeFlagPublisher = recipeFlagPublisher;
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

                if(path != null)
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
                }
                path = WeaponTreePathFinder.FindPath(_forgeManager.CurrentWeapon.WeaponId, e._recipeId, _weaponDatabase);
                if(path == null) return;
                for(int i = 0; i < path.Count - 1; i++)
                {
                    _nodes[path[i]].SetRouteNode();
                    if (_treeBuilder.Branches.TryGetValue((path[i], path[i + 1]), out var branch))
                    {
                        branch.SetHighlight(true);
                    }
                }
                _nodes[path[^1]].SetDestinationNode();
                _recipeFlagPublisher.Publish(new ChangeRecipeFlagEvent { _recipeId = e._recipeId });
            }).AddTo(_disposables);
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