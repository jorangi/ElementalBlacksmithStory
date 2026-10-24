using ElementalBlacksmithStory.Events;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Core
{
    public class MessagePipeInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();

            builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));

            // 도메인 이벤트
            builder.RegisterMessageBroker<EnhanceRequestEvent>(options);
            builder.RegisterMessageBroker<EnhanceResultEvent>(options);
            builder.RegisterMessageBroker<ChangeWeaponEvent>(options);
            builder.RegisterMessageBroker<SubmitMaterialEvent>(options);
            builder.RegisterMessageBroker<EnhanceButtonPositionEvent>(options);
            builder.RegisterMessageBroker<UpdateEnhanceChanceEvent>(options);
            builder.RegisterMessageBroker<ChangeSelectedMaterialsEvent>(options);
            builder.RegisterMessageBroker<GameExitEvent>(options);
            builder.RegisterMessageBroker<PlaySoundEvent>(options);
            builder.RegisterMessageBroker<SellEvent>(options);
            builder.RegisterMessageBroker<StartDialogueEvent>(options);
            builder.RegisterMessageBroker<StartShopDialogueEvent>(options);
            builder.RegisterMessageBroker<AcquireItemEvent>(options);

            // VFX / Particle 이벤트
            builder.RegisterMessageBroker<CoinParticleEvent>(options);
        }
    }
}
