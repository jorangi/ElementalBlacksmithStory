using UnityEngine;

namespace ElementalBlacksmithStory.Events
{
    public readonly struct CoinParticleEvent
    {
        public readonly int Amount;
        public readonly (UnityEngine.Color, UnityEngine.Color) Colors { get; }
        public CoinParticleEvent(int amount, (UnityEngine.Color, UnityEngine.Color) colors)
        {
            Amount = amount;
            Colors = colors;
        }
        public CoinParticleEvent(int amount)
        {
            Amount = amount;
            Colors = (UnityEngine.Color.white, UnityEngine.Color.yellow);
        }
    }
}