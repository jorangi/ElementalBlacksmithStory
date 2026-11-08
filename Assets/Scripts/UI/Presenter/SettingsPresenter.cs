using System;
using ElementalBlacksmithStory.Core;
using R3;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class SettingsPresenter : IStartable, IDisposable
    {
        private readonly SettingsView _view;
        private readonly SettingsService _service;
        public bool IsActivated => _view.IsActivated;
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public SettingsPresenter(SettingsView view, SettingsService service)
        {
            _view = view;
            _service = service;
        }
        public void Start()
        {
            _view.SetInitialValues(
                _service.BGMVolume,
                _service.SFXVolume,
                _service.IsVibrationEnabled,
                _service.IsAutoselectMaterials,
                _service.IsExpeditionNotice,
                _service.IsQuicksellAlert,
                _service.IsScreenShake
            );

            _view.OnBGMValueChanged.Subscribe(this, static (val, self) =>
            {
                self._service.SetBGMVolume(val);
                self._view.UpdateBGMPercentText(val);
            }).AddTo(_disposables);

            _view.OnSFXValueChanged.Subscribe(this, static (val, self) =>
            {
                self._service.SetSFXVolume(val);
                self._view.UpdateSFXPercentText(val);
            }).AddTo(_disposables);

            _view.OnVibrationChanged.Subscribe(this, static (val, self) => self._service.SetVibration(val)).AddTo(_disposables);
            _view.OnEnhancementPresentationClick.Subscribe(this, static (_, self) => self._service.ExecuteEnhancementPresentation()).AddTo(_disposables);
            _view.OnProtectMaterialsClick.Subscribe(this, static (_, self) => self._service.ExecuteProtectMaterials()).AddTo(_disposables);
            _view.OnAutoselectMaterialsChanged.Subscribe(this, static (val, self) => self._service.SetAutoselectMaterials(val)).AddTo(_disposables);
            _view.OnExpeditionNoticeChanged.Subscribe(this, static (val, self) => self._service.SetExpeditionNotice(val)).AddTo(_disposables);
            _view.OnQuicksellAlertChanged.Subscribe(this, static (val, self) => self._service.SetQuicksellAlert(val)).AddTo(_disposables);
            _view.OnScreenShakeChanged.Subscribe(this, static (val, self) => self._service.SetScreenShake(val)).AddTo(_disposables);
            _view.OnAccountLinkClick.Subscribe(this, static (_, self) => self._service.ExecuteAccountLink()).AddTo(_disposables);
            _view.OnTermsOfUseClick.Subscribe(this, static (_, self) => self._service.ExecuteTermsOfUse()).AddTo(_disposables);
        }
        public void Hide()
        {
            _view.Hide();
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
