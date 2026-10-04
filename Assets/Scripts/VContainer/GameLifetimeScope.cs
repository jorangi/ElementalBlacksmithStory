using UnityEngine;
using VContainer;
using VContainer.Unity;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_WeaponDatabase weaponDatabase;
        [SerializeField] private SO_MaterialDatabase materialDatabase;
        [SerializeField] private SO_RuneDatabase runeDatabase;
        [SerializeField] private SO_SpriteSettings spriteSettings;
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterInstance(weaponDatabase);
            builder.RegisterInstance(materialDatabase);
            builder.RegisterInstance(runeDatabase);
            builder.RegisterInstance(spriteSettings);

            new MessagePipeInstaller().Install(builder);
            new GameServiceInstaller().Install(builder);
            new UIInstaller().Install(builder);
        }
    }
}
