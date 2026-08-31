using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using VContainer;
using VContainer.Unity;
using MessagePipe;
using R3;
using System;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

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
            ISubscriber<ChangeWeaponEvent> changeWeaponSubscriber,
            ISubscriber<NextRecipeChangedEvent> nextRecipeSubscriber,
            ISubscriber<EnhanceButtonPositionEvent> positionSubscriber)
        {
            _weaponDatabase = weaponDatabase;
            _view = view;

            changeWeaponSubscriber.Subscribe(e =>
            {
                _currentCost = e.Weapon.Cost;
                _view.SetChance(_currentChance, _currentCost);
            }).AddTo(_disposables);

            nextRecipeSubscriber.Subscribe(e =>
            {
                _currentChance = (e.Recipe != null) ? e.Recipe.recipeOutcome.chance : 0f;
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
            var weapon = _weaponDatabase.GetWeapon(10001);
            var recipe = weapon?.recipes?.FirstOrDefault();
            _currentCost = weapon?.cost ?? 0;
            _currentChance = (recipe != null) ? recipe.recipeOutcome.chance : 0f;
            _view.SetChance(_currentChance, _currentCost);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}