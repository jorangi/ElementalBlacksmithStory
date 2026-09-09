using VContainer;
using VContainer.Unity;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Inventory;
using ElementalBlacksmithStory.UI;

namespace ElementalBlacksmithStory.Core
{
    public class GameServiceInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameInitializer>();

            builder.Register<EquipmentInventory>(Lifetime.Singleton);
            builder.Register<MaterialInventory>(Lifetime.Singleton);
            builder.Register<ForgeManager>(Lifetime.Singleton);

            builder.RegisterEntryPoint<EnhancementService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SettingsService>(Lifetime.Singleton).AsSelf();

            builder.RegisterEntryPoint<WeaponSpriteLoader>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<MaterialSpriteLoader>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<NPCStandingSpriteLoader>(Lifetime.Singleton).AsSelf();
            builder.RegisterComponentInHierarchy<AudioClipLoader>();
            builder.RegisterComponentInHierarchy<WeaponTreeBuilder>();
            builder.RegisterComponentInHierarchy<CoinParticleListener>();
        }
    }
}
