using System;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Data;
using System.Threading;
using R3;

namespace ElementalBlacksmithStory.UI
{
    public class QuickSellPresenter : IStartable, IDisposable
    {
        private readonly QuickSellView _view;
        private readonly CompositeDisposable _disposables = new();
        private ulong sellPrice;
        private readonly IPublisher<SellEvent> _sellEventPublisher;
        private readonly IPublisher<CoinParticleEvent> _coinParticlePublisher;
        [Inject]
        public QuickSellPresenter(
            SO_WeaponDatabase weaponDataBase,
            QuickSellView view,
            ISubscriber<ChangeWeaponEvent> weaponChangeSubscriber,
            IPublisher<SellEvent> sellEventPublisher,
            IPublisher<CoinParticleEvent> coinParticlePublisher)
        {
            this._view = view;
            _sellEventPublisher = sellEventPublisher;
            _coinParticlePublisher = coinParticlePublisher;

            
            view.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(300))
                .Subscribe(_=>Sell())
                .AddTo(_disposables);

            weaponChangeSubscriber.Subscribe(e =>
            {
                sellPrice = e.Weapon.Price;
               _view.SetPrice(sellPrice, e.TotalCost).Forget();
            }).AddTo(_disposables);

        }
        public void Sell()
        {
            ulong currentSellPrice = sellPrice;
            if (currentSellPrice == 0) return;

            int amount = currentSellPrice switch
            {
                >= 100000 => 300,
                >= 80000  => 250,
                >= 50000  => 200,
                >= 20000  => 150,
                >= 10000  => 100,
                >= 8000   => 90,
                >= 5000   => 60,
                >= 3000   => 40,
                >= 1000   => 20,
                > 0       => 10,
                _         => 0
            };

            _coinParticlePublisher.Publish(new CoinParticleEvent(amount));
            _sellEventPublisher.Publish(new SellEvent(currentSellPrice));
        }
        public void Start(){}
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}