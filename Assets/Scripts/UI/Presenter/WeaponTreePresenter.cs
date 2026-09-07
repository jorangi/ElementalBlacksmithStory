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
            IPublisher<ChangeRecipeFlagEvent> recipeFlagPublisher,
            ISubscriber<EnhanceButtonPositionEvent> positionSubscriber
            )
        {
            _forgeManager = forgeManager;
            _treeBuilder = treeBuilder;
            _spriteLoader = spriteLoader;
            _weaponDatabase = weaponDatabase;
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

            // 레시피 변경 동기화
            _treeBuilder.OnChangeRecipeFlagEventAsObservable.Subscribe(e =>
            {
                if(path != null)
                {
                    for (int i = 0; i < path.Count; i++)
                    {
                        _nodes[path[i]].SetNormalNode();

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
                    _recipeFlagPublisher.Publish(new ChangeRecipeFlagEvent { _recipeId = e._recipeId });
                    
                }
                _nodes[path[^1]].SetDestinationNode();
            }).AddTo(_disposables);
        }

        public void Start()
        {
            var rootWeapon = _weaponDatabase.GetWeapon(10001);
            if (rootWeapon != null)
            {
                _nodes = _treeBuilder.GenerateTree(rootWeapon);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}