using System;
using UnityEngine;
using VContainer;
using MessagePipe;
using ElementalBlacksmithStory.Events;

namespace ElementalBlacksmithStory.Core
{
    public class CoinParticleListener : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _coinParticle;
        private IDisposable _subscription;

        [Inject]
        public void Construct(ISubscriber<CoinParticleEvent> subscriber)
        {
            _subscription = subscriber.Subscribe(e =>
            {
                var emission = _coinParticle.emission;
                var burst = new ParticleSystem.Burst(0f, (short)e.Amount);
                burst.cycleCount = 1;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    burst
                });

                var customData = _coinParticle.customData;
                customData.enabled = true;
                customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Color);
                customData.SetColor(
                    ParticleSystemCustomData.Custom1,
                    new ParticleSystem.MinMaxGradient(e.Colors.Item1, e.Colors.Item2)
                );
                var shape = _coinParticle.shape;
                shape.radius = 1.2f * (float)Screen.width / 1080f;
                _coinParticle.Play();
            });
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
    }   
}
