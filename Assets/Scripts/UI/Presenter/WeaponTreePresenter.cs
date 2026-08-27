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

        [Inject]
        public WeaponTreePresenter(
            WeaponTreeBuilder treeBuilder,
            WeaponSpriteLoader spriteLoader,
            SO_WeaponDatabase weaponDatabase,
            IPublisher<ChangeRecipeFlagEvent> recipeFlagPublisher)
        {
            _treeBuilder = treeBuilder;
            _spriteLoader = spriteLoader;
            _weaponDatabase = weaponDatabase;
            _recipeFlagPublisher = recipeFlagPublisher;
        }

        public void Start()
        {
            _treeBuilder.OnChangeRecipeFlagEventAsObservable
                .ThrottleFirst(TimeSpan.FromMilliseconds(100))
                .Subscribe(e =>
                {
                    _recipeFlagPublisher.Publish(new ChangeRecipeFlagEvent { _recipeId = e._recipeId });
                })
                .AddTo(_disposables);
            var rootWeapon = _weaponDatabase.GetWeapon(10001);
            if (rootWeapon != null)
            {
                _treeBuilder.GenerateTree(rootWeapon);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}