using System;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class GameExitPresenter :IDisposable
    {
        private readonly GameExitView _view;
        private readonly CompositeDisposable _disposables = new();
        [Inject]
        public GameExitPresenter(
            GameExitView view,
            IPublisher<GameExitEvent> exitPub
        )
        {
            view.OnExitButtonAsObservable().Subscribe(_ =>
            {
                exitPub.Publish(new GameExitEvent{});
            }).AddTo(_disposables);
            _view = view;
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void ShowPanel() => _view.gameExitPanel.SetActive(!_view.gameExitPanel.activeInHierarchy);
    }
}