using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Inventory;
using ElementalBlacksmithStory.UI;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Core
{
    public class GameServiceInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameInitializer>();
            builder.RegisterEntryPoint<PlayerData>(Lifetime.Singleton).AsSelf();

            builder.RegisterEntryPoint<EquipmentInventory>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<MaterialInventory>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<RuneInventory>(Lifetime.Singleton).AsSelf();
            builder.Register<ForgeManager>(Lifetime.Singleton);
            builder.Register<RecipeKeyHelper>(Lifetime.Singleton);
            builder.Register<RecipeUnlockService>(Lifetime.Singleton);
            builder.Register<WeaponRollbackService>(Lifetime.Singleton);
            builder.Register<WeaponTreePathService>(Lifetime.Singleton);
            builder.Register<ShopService>(Lifetime.Singleton);
            builder.Register<TradingPostService>(Lifetime.Singleton);
            builder.Register<IPlayerProgressionService, PlayerProgressionService>(Lifetime.Singleton);
            builder.Register<RuneService>(Lifetime.Singleton);
            builder.Register<IItemShopFactory, ItemShopFactory>(Lifetime.Singleton);

            builder.RegisterEntryPoint<EnhancementService>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<SettingsService>(Lifetime.Singleton).AsSelf();

            builder.RegisterEntryPoint<WeaponSpriteLoader>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<MaterialSpriteLoader>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<RuneSpriteLoader>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<NPCStandingSpriteLoader>(Lifetime.Singleton).AsSelf();
            builder.RegisterComponentInHierarchy<AudioClipLoader>();
            builder.RegisterComponentInHierarchy<WeaponTreeBuilder>();
            builder.RegisterComponentInHierarchy<CoinParticleListener>();
        }
    }
}
