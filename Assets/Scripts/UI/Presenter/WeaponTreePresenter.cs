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

        [Inject]
        public WeaponTreePresenter(
            ForgeManager forgeManager,
            WeaponTreeBuilder treeBuilder,
            WeaponSpriteLoader spriteLoader,
            SO_WeaponDatabase weaponDatabase,
            IPublisher<ChangeRecipeFlagEvent> recipeFlagPublisher)
        {
            _forgeManager = forgeManager;
            _treeBuilder = treeBuilder;
            _spriteLoader = spriteLoader;
            _weaponDatabase = weaponDatabase;
            _recipeFlagPublisher = recipeFlagPublisher;
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