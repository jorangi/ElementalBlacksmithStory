using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using VContainer;
using VContainer.Unity;
using MessagePipe;
using R3;
using System;
using System.Linq;

namespace ElementalBlacksmithStory.UI
{
    public class EnhanceChancePresenter : IStartable, IDisposable
    {
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly EnhanceChanceView _view;
        private readonly CompositeDisposable _disposables = new();
        
        [Inject]
        public EnhanceChancePresenter(SO_WeaponDatabase weaponDatabase, EnhanceChanceView view, ISubscriber<ChangeWeaponEvent> changeWeaponSubscriber)
        {
            _weaponDatabase = weaponDatabase;
            _view = view;
            changeWeaponSubscriber.Subscribe(e =>
            {
                var weapon = weaponDatabase.GetWeapon(e.WeaponId);
                var recipe = weapon?.recipes?.FirstOrDefault();
                float chance = (recipe != null) ? recipe.recipeOutcome.chance : 0f;
                _view.SetChance(chance);
            }).AddTo(_disposables);
        }
        public void Start()
        {
            var weapon = _weaponDatabase.GetWeapon(10001);
            var recipe = weapon?.recipes?.FirstOrDefault();
            float chance = (recipe != null) ? recipe.recipeOutcome.chance : 0f;
            _view.SetChance(chance);
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}