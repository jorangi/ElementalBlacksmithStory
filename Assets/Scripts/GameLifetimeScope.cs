using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.UI;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Inventory;

namespace ElementalBlacksmithStory.Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_WeaponDatabase weaponDatabase;
        [SerializeField] private SO_MaterialDatabase materialDatabase;
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            var options = builder.RegisterMessagePipe();
            weaponDatabase.Init();
            materialDatabase.Init();
            builder.RegisterInstance(weaponDatabase);
            builder.RegisterInstance(materialDatabase);
            builder.Register<EquipmentInventory>(Lifetime.Singleton);
            builder.Register<MaterialInventory>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameInitializer>();
            builder.RegisterComponentInHierarchy<WeaponSpriteLoader>();
            builder.RegisterComponentInHierarchy<AudioClipLoader>();
            builder.Register<ForgeManager>(Lifetime.Singleton);
            builder.RegisterEntryPoint<MoneyPresenter>();
            builder.RegisterEntryPoint<QuickSellPresenter>();
            builder.RegisterComponentInHierarchy<MaterialSpriteLoader>();
            builder.RegisterComponentInHierarchy<WeaponTreeBuilder>();
            builder.RegisterComponentInHierarchy<MoneyView>();
            builder.RegisterComponentInHierarchy<QuickSellView>();
            builder.RegisterComponentInHierarchy<AnvilWeaponView>();
            builder.RegisterComponentInHierarchy<EnhanceButtonView>();
            builder.RegisterComponentInHierarchy<EnhanceChanceView>();
            builder.RegisterComponentInHierarchy<MaterialsView>();
            builder.RegisterComponentInHierarchy<SelectMaterialAmountView>();
            builder.RegisterComponentInHierarchy<GameExitView>();
            builder.RegisterComponentInHierarchy<SettingsView>();
            builder.RegisterEntryPoint<AnvilWeaponPresenter>();
            builder.RegisterEntryPoint<EnhanceButtonPresenter>();
            builder.RegisterEntryPoint<EnhanceChancePresenter>();
            builder.RegisterEntryPoint<MaterialsPresenter>();
            builder.RegisterEntryPoint<WeaponTreePresenter>();
            builder.RegisterEntryPoint<GameExitPresenter>().AsSelf();
            builder.RegisterEntryPoint<SettingsPresenter>();
            builder.RegisterEntryPoint<EnhancementService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SettingsService>(Lifetime.Singleton).AsSelf();
            builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));
            builder.RegisterMessageBroker<EnhanceRequestEvent>(options);
            builder.RegisterMessageBroker<EnhanceResultEvent>(options);
            builder.RegisterMessageBroker<ChangeWeaponEvent>(options);
            builder.RegisterMessageBroker<ChangeMoneyEvent>(options);
            builder.RegisterMessageBroker<SubmitMaterialEvent>(options);
            builder.RegisterMessageBroker<EnhanceButtonPositionEvent>(options);
            builder.RegisterMessageBroker<NextRecipeChangedEvent>(options);
            builder.RegisterMessageBroker<GameExitEvent>(options);
            builder.RegisterMessageBroker<PlaySoundEvent>(options);
            builder.RegisterMessageBroker<SellEvent>(options);
        }
    }
}
