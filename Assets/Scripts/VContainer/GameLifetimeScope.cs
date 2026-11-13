using ElementalBlacksmithStory.Data;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_WeaponDatabase weaponDatabase;
        [SerializeField] private SO_MaterialDatabase materialDatabase;
        [SerializeField] private SO_RuneDatabase runeDatabase;
        [SerializeField] private SO_CustomerGroupDatabase customerGroupDatabase;
        [SerializeField] private SO_MercenaryDatabase mercenaryDatabase;
        [SerializeField] private SO_SpriteSettings spriteSettings;
        [SerializeField] private SO_ProgressionSettings progressionSettings;
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterInstance(weaponDatabase);
            builder.RegisterInstance(materialDatabase);
            builder.RegisterInstance(runeDatabase);
            builder.RegisterInstance(customerGroupDatabase);
            builder.RegisterInstance(mercenaryDatabase);
            builder.RegisterInstance(spriteSettings);
            builder.RegisterInstance(progressionSettings);
            new MessagePipeInstaller().Install(builder);
            new GameServiceInstaller().Install(builder);
            new UIInstaller().Install(builder);
        }
    }
}
