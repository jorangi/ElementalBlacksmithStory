using System;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class ForgePresenter : IStartable, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly ForgeView _view;
        private readonly SO_WeaponDatabase _weaponDB;
        private readonly EnhancementService _service;
        private readonly ForgeManager _forgeManager;
        private readonly PlayerData _playerData;

        public ForgePresenter(
            ForgeView view,
            EnhancementService service,
            ForgeManager forgeManager,
            PlayerData playerData,
            SO_WeaponDatabase weaponDB,
            ISubscriber<ChangeWeaponEvent> weaponChangeSubscriber
        )
        {
            _view = view;
            _service = service;
            _forgeManager = forgeManager;
            _playerData = playerData;
            _weaponDB = weaponDB;

            Debug.Log("ForgePresenter 초기화");
            _service.EnhanceInfo.Subscribe(
                this, static (_, self) =>
                {
                    ulong totalCost = self._forgeManager.CurrentWeapon?.TotalCost.CurrentValue ?? 0UL;
                    self.SetEnhanceInfo(self._service.EnhanceInfo.CurrentValue, totalCost);
                }).AddTo(_disposables);

            weaponChangeSubscriber.Subscribe(e =>
            {
                if (e.Weapon == null) return;
                ulong price = _playerData.TryPeekLatestPrice(e.WeaponId, out var latestPrice)
                    ? latestPrice
                    : e.Weapon.BasePrice;
                _view.SetQuickSellPriceAsync(price).Forget();
            }).AddTo(_disposables);

            _view.OnEnhanceButtonAsObservable()
                .Subscribe(this, static (_, self) =>
                {
                }).AddTo(_disposables);
            _view.OnQuickSellButtonAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(300))
                .Subscribe(this, static (_, self) =>
                {
                    self._service.Sell();
                }).AddTo(_disposables);
            _view.OnInventoryButtonAsObservable()
                .Subscribe(this, static (_, self) =>
                {
                }).AddTo(_disposables);
            _view.OnRecipeTreeButtonAsObservable()
                .Subscribe(this, static (_, self) =>
                {
                }).AddTo(_disposables);
        }
        public void Start()
        {
            _playerData.OnTradePriceChanged
                .Prepend(_forgeManager.CurrentWeapon != null ? _forgeManager.CurrentWeapon.WeaponId : 0u)
                .Subscribe(this, static (itemId, self) =>
                {
                    if (self._forgeManager.CurrentWeapon == null || self._forgeManager.CurrentWeapon.WeaponId != itemId) return;
                    ulong price = self._playerData.TryPeekLatestPrice(itemId, out var latestPrice) ?
                        latestPrice :
                        self._forgeManager.CurrentWeapon.BasePrice;
                    self._view.SetQuickSellPriceAsync(price).Forget();
                }).AddTo(_disposables);
        }
        public void SetEnhanceInfo(EnhanceInfoPayload info, ulong accumulatedCost)
        {
            _view.SetEnhanceChanceText(info.Chance == -1 ? "??%" : ZString.Format("{0}%", info.Chance * 100));
            _view.SetEnhanceCostText(ZString.Format("<size=60%><sprite name=\"CoinSack\"></size>{0} G <br><size=60%>누적 소요: {1} G</size>", info.Cost, accumulatedCost));
        }
        public void Dispose()
        {
            _disposables.Dispose();
            _disposables.Clear();
        }
    }
}