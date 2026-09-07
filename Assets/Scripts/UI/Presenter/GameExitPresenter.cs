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
        private readonly SettingsPresenter _settingsPresenter;
        private readonly WeaponTreePresenter _weaponTreePresenter;
        private readonly SelectMaterialAmountView _selectMaterialAmountView;
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public GameExitPresenter(
            GameExitView view,
            SettingsPresenter settingsPresenter,
            WeaponTreePresenter weaponTreePresenter,
            SelectMaterialAmountView selectMaterialAmountView,
            IPublisher<GameExitEvent> exitPub
        )
        {
            view.OnExitButtonAsObservable().Subscribe(_ =>
            {
                exitPub.Publish(new GameExitEvent{});
            }).AddTo(_disposables);
            _settingsPresenter = settingsPresenter;
            _weaponTreePresenter = weaponTreePresenter;
            _selectMaterialAmountView = selectMaterialAmountView;
            _view = view;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
        /// <summary>
        /// 게임 종료 확인 패널 띄우기, 다른 패널이 열려있다면 해당 패널 닫기
        /// 1순위 설정창, 2순위 무기트리, 3순위 재료수량조정창
        /// </summary>
        public void ShowPanel()
        {
            if (_settingsPresenter.IsActivated)
            {
                _settingsPresenter.Hide();
            }
            else if (_weaponTreePresenter.IsActivated)
            {
                _weaponTreePresenter.Hide();
            }
            else if (_selectMaterialAmountView.IsActivated)
            {
                _selectMaterialAmountView.Hide();
            }
            else
            {
                _view.gameExitPanel.SetActive(!_view.gameExitPanel.activeInHierarchy);
            }
        }
    }
}