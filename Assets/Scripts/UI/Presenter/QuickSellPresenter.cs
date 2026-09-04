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
        [Inject]
        public QuickSellPresenter(
            SO_WeaponDatabase weaponDataBase,
            QuickSellView view,
            ISubscriber<ChangeWeaponEvent> weaponChangeSubscriber,
            IPublisher<SellEvent> sellEventPublisher
            )
        {
            this._view = view;
            _sellEventPublisher = sellEventPublisher;
            weaponChangeSubscriber.Subscribe(e =>
            {
                sellPrice = e.Weapon.Price;
               _view.SetPrice(sellPrice, e.TotalCost).Forget();
            }).AddTo(_disposables);

            view.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(300))
                .Subscribe(_=>Sell())
                .AddTo(_disposables);
        }
        public void Sell()
        {
            _sellEventPublisher.Publish(new SellEvent(sellPrice));
        }
        public void Start(){}
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}