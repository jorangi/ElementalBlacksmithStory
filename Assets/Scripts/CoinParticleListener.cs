using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;
using R3;

public class CoinParticleListener : MonoBehaviour
{
    private readonly ParticleSystem _coinParticle;
    private readonly CompositeDisposable _disposables = new();
    [Inject]
    public void Construct(
        
    )
    {
        
    }
    private void Start()
    {
        
    }
}
