using System;
using Cysharp.Threading.Tasks;
using R3;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class UIBackPanelPresenter : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly UIBackPanelView _backPanelView;

        [Inject]
        public UIBackPanelPresenter(UIBackPanelView backPanelView)
        {
            _backPanelView = backPanelView;
        }

        public void Show()
        {
            _backPanelView.Show().Forget();
        }

        public void Hide()
        {
            _backPanelView.Hide().Forget();
        }

        public void Dispose()
        {
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}