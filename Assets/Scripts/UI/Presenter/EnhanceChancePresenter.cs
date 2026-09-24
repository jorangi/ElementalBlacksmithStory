using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class EnhanceChancePresenter : IStartable, IDisposable
    {
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly EnhanceChanceView _view;
        private readonly CompositeDisposable _disposables = new();
        private float _currentChance = 0f;
        private ulong _currentCost = 0;

        [Inject]
        public EnhanceChancePresenter(
            SO_WeaponDatabase weaponDatabase,
            EnhanceChanceView view,
            ISubscriber<UpdateEnhanceChanceEvent> enhanceChanceSubscriber,
            ISubscriber<EnhanceButtonPositionEvent> positionSubscriber)
        {
            _weaponDatabase = weaponDatabase;
            _view = view;

            enhanceChanceSubscriber.Subscribe(e =>
            {
                _currentChance = e.Chance;
                _currentCost = e.Cost;
                _view.SetChance(_currentChance, _currentCost);
            }).AddTo(_disposables);

            positionSubscriber.Subscribe(e =>
            {
                if (e.positionY <= -1000f)
                {
                    _view.HidingChanceAnimation().Forget();
                }
                else if (Mathf.Approximately(e.positionY, 0f))
                {
                    _view.ShowingChanceAnimation().Forget();
                }
                else
                {
                    _view.SyncYPosition(429.1f + e.positionY);
                }
            }).AddTo(_disposables);
        }

        public void Start()
        {
            var weapon = _weaponDatabase.Get(10001);
            _currentCost = weapon?.basePrice ?? 0;
            _currentChance = -1f;
            _view.SetChance(_currentChance, _currentCost);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}