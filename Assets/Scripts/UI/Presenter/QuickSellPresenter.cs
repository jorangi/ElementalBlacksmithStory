using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class QuickSellPresenter : IStartable, IDisposable
    {
        private readonly QuickSellView _view;
        private readonly CompositeDisposable _disposables = new();
        private ulong sellPrice;
        private ulong _currentTotalCost;
        private readonly IPublisher<QuickSellEvent> _sellEventPublisher;
        private readonly IPublisher<CoinParticleEvent> _coinParticlePublisher;
        private readonly PlayerData _playerData;
        private Weapon _currentWeapon;

        [Inject]
        public QuickSellPresenter(
            SO_WeaponDatabase weaponDataBase,
            QuickSellView view,
            ISubscriber<ChangeWeaponEvent> weaponChangeSubscriber,
            IPublisher<QuickSellEvent> sellEventPublisher,
            PlayerData playerData,
            IPublisher<CoinParticleEvent> coinParticlePublisher)
        {
            this._view = view;
            _sellEventPublisher = sellEventPublisher;
            _coinParticlePublisher = coinParticlePublisher;
            _playerData = playerData;

            view.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(300))
                .Subscribe(this, static (_, self) => self.Sell())
                .AddTo(_disposables);

            weaponChangeSubscriber.Subscribe(e =>
            {
                _currentWeapon = e.Weapon;
                _currentTotalCost = e.TotalCost;
                RefreshPrice();
            }).AddTo(_disposables);

            _playerData.OnTradePriceChanged
                .Subscribe(this, static (itemId, self) =>
                {
                    if (self._currentWeapon != null && self._currentWeapon.WeaponId == itemId)
                    {
                        self.RefreshPrice();
                    }
                })
                .AddTo(_disposables);
        }

        private void RefreshPrice()
        {
            if (_currentWeapon == null)
            {
                sellPrice = 0;
                return;
            }

            if (_playerData.TryPeekLatestPrice(_currentWeapon.WeaponId, out ulong latestPrice))
            {
                sellPrice = latestPrice;
            }
            else
            {
                sellPrice = _currentWeapon.Price;
            }

            _view.SetPrice(sellPrice, _currentTotalCost).Forget();
        }

        public void Sell()
        {
            if (_currentWeapon == null) return;
            if (_playerData.TryConsumeLatestPrice(_currentWeapon.WeaponId, out ulong latestPrice))
            {
                sellPrice = latestPrice;
            }
            else
            {
                sellPrice = _currentWeapon.Price;
            }

            ulong currentSellPrice = sellPrice;
            if (currentSellPrice == 0) return;
            _sellEventPublisher.Publish(new QuickSellEvent(currentSellPrice));
        }

        public void Start() { }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
