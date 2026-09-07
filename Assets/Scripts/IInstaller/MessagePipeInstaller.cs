using VContainer;
using VContainer.Unity;
using MessagePipe;
using ElementalBlacksmithStory.Events;

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
            builder.RegisterMessageBroker<ChangeMoneyEvent>(options);
            builder.RegisterMessageBroker<SubmitMaterialEvent>(options);
            builder.RegisterMessageBroker<EnhanceButtonPositionEvent>(options);
            builder.RegisterMessageBroker<NextRecipeChangedEvent>(options);
            builder.RegisterMessageBroker<GameExitEvent>(options);
            builder.RegisterMessageBroker<PlaySoundEvent>(options);
            builder.RegisterMessageBroker<SellEvent>(options);

            // VFX / Particle 이벤트
            builder.RegisterMessageBroker<CoinParticleEvent>(options);
        }
    }
}
