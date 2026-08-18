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
    public class MoneyPresenter : IStartable, IDisposable
    {
        private readonly MoneyView _view;
        private readonly CompositeDisposable _disposables = new();
        [Inject]
        public MoneyPresenter(
            MoneyView view,
            ISubscriber<ChangeMoneyEvent> subscriber)
        {
            this._view = view;
            subscriber.Subscribe(e =>
            {
               _view.SetMoney(e.MoneyChange).Forget();
            }).AddTo(_disposables);
        }
        public void Start(){}
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}