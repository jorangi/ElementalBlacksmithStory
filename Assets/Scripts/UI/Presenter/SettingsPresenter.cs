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

            _view.OnBGMValueChanged.Subscribe(val =>
            {
                _service.SetBGMVolume(val);
                _view.UpdateBGMPercentText(val);
            }).AddTo(_disposables);

            _view.OnSFXValueChanged.Subscribe(val =>
            {
                _service.SetSFXVolume(val);
                _view.UpdateSFXPercentText(val);
            }).AddTo(_disposables);

            _view.OnVibrationChanged.Subscribe(val => _service.SetVibration(val)).AddTo(_disposables);
            _view.OnEnhancementPresentationClick.Subscribe(_ => _service.ExecuteEnhancementPresentation()).AddTo(_disposables);
            _view.OnProtectMaterialsClick.Subscribe(_ => _service.ExecuteProtectMaterials()).AddTo(_disposables);
            _view.OnAutoselectMaterialsChanged.Subscribe(val => _service.SetAutoselectMaterials(val)).AddTo(_disposables);
            _view.OnExpeditionNoticeChanged.Subscribe(val => _service.SetExpeditionNotice(val)).AddTo(_disposables);
            _view.OnQuicksellAlertChanged.Subscribe(val => _service.SetQuicksellAlert(val)).AddTo(_disposables);
            _view.OnScreenShakeChanged.Subscribe(val => _service.SetScreenShake(val)).AddTo(_disposables);
            _view.OnAccountLinkClick.Subscribe(_ => _service.ExecuteAccountLink()).AddTo(_disposables);
            _view.OnTermsOfUseClick.Subscribe(_ => _service.ExecuteTermsOfUse()).AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
