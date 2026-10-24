using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class MoneyPresenter : IStartable, IDisposable
    {
        private readonly MoneyView _view;
        private readonly CompositeDisposable _disposables = new();
        [Inject]
        public MoneyPresenter(
            PlayerData playerData,
            MoneyView view)
        {
            this._view = view;
            playerData.Money.Subscribe(e =>
            {
                _view.SetMoney(e).Forget();
            }).AddTo(_disposables);
        }
        public void Start() { }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}