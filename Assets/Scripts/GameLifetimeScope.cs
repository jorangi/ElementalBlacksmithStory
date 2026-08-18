using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.UI;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_WeaponDatabase weaponDatabase;
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            var options = builder.RegisterMessagePipe();
            weaponDatabase.Init();
            builder.RegisterInstance(weaponDatabase);
            builder.RegisterComponentInHierarchy<WeaponSpriteLoader>();
            builder.RegisterComponentInHierarchy<AudioClipLoader>();
            builder.Register<ForgeManager>(Lifetime.Singleton);
            builder.RegisterEntryPoint<MoneyPresenter>();
            builder.RegisterEntryPoint<QuickSellPresenter>();
            builder.RegisterComponentInHierarchy<MaterialSpriteLoader>();
            builder.RegisterComponentInHierarchy<MoneyView>();
            builder.RegisterComponentInHierarchy<QuickSellView>();
            builder.RegisterComponentInHierarchy<AnvilWeaponView>();
            builder.RegisterComponentInHierarchy<EnhanceButtonView>();
            builder.RegisterComponentInHierarchy<EnhanceChanceView>();
            builder.RegisterComponentInHierarchy<MaterialsView>();
            builder.RegisterEntryPoint<AnvilWeaponPresenter>();
            builder.RegisterEntryPoint<EnhanceButtonPresenter>();
            builder.RegisterEntryPoint<EnhanceChancePresenter>();
            builder.RegisterEntryPoint<MaterialsPresenter>();
            builder.RegisterEntryPoint<EnhancementService>(Lifetime.Singleton);
            builder.RegisterMessageBroker<EnhanceRequestEvent>(options);
            builder.RegisterMessageBroker<EnhanceResultEvent>(options);
            builder.RegisterMessageBroker<ChangeWeaponEvent>(options);
            builder.RegisterMessageBroker<ChangeMoneyEvent>(options);
        }
    }
}
