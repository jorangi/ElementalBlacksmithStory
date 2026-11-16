using System;
using R3;
using VContainer;

namespace ElementalBlacksmithStory.UI
{
    public class SessionMenusPresenter : IDisposable
    {
        private readonly SessionMenusView _view;
        private readonly CompositeDisposable _disposables = new();
        [Inject]
        public SessionMenusPresenter(
            SessionMenusView view
        )
        {
            _view = view;
            _view.OnChangeButtonIndexAsObservable()
                .Subscribe(this, static (i, self) =>
                {
                    self._view.ChangeHighLight(i);
                }).AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _disposables.Clear();
        }
    }
}