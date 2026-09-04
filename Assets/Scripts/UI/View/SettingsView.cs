using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace ElementalBlacksmithStory.UI
{
    public class SettingsView : MonoBehaviour
    {
        [Header("Audio Controls")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private TextMeshProUGUI _bgmPercentText;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TextMeshProUGUI _sfxPercentText;
        [SerializeField] private Toggle _vibrationToggle;

        [Header("Forge Controls")]
        [SerializeField] private Button _enhancementPresentationButton;
        [SerializeField] private Button _protectMaterialsButton;
        [SerializeField] private Toggle _autoselectMaterialsToggle;

        [Header("Expedition Controls")]
        [SerializeField] private Toggle _expeditionNoticeToggle;

        [Header("Selling Controls")]
        [SerializeField] private Toggle _quicksellAlertToggle;

        [Header("Screen Controls")]
        [SerializeField] private Toggle _screenShakeToggle;

        [Header("Account/Etc Controls")]
        [SerializeField] private Button _accountLinkButton;
        [SerializeField] private Button _termsOfUseButton;
        public void SetInitialValues(
            float bgmVolume,
            float sfxVolume,
            bool vibration,
            bool autoselectMaterials,
            bool expeditionNotice,
            bool quicksellAlert,
            bool screenShake)
        {
            if (_bgmSlider != null)
            {
                _bgmSlider.value = bgmVolume;
                UpdateBGMPercentText(bgmVolume);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.value = sfxVolume;
                UpdateSFXPercentText(sfxVolume);
            }

            if (_vibrationToggle != null) _vibrationToggle.isOn = vibration;
            if (_autoselectMaterialsToggle != null) _autoselectMaterialsToggle.isOn = autoselectMaterials;
            if (_expeditionNoticeToggle != null) _expeditionNoticeToggle.isOn = expeditionNotice;
            if (_quicksellAlertToggle != null) _quicksellAlertToggle.isOn = quicksellAlert;
            if (_screenShakeToggle != null) _screenShakeToggle.isOn = screenShake;
        }

        public void UpdateBGMPercentText(float value)
        {
            if (_bgmPercentText != null)
            {
                _bgmPercentText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        public void UpdateSFXPercentText(float value)
        {
            if (_sfxPercentText != null)
            {
                _sfxPercentText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        public Observable<float> OnBGMValueChanged => _bgmSlider != null ? _bgmSlider.OnValueChangedAsObservable() : Observable.Empty<float>();
        public Observable<float> OnSFXValueChanged => _sfxSlider != null ? _sfxSlider.OnValueChangedAsObservable() : Observable.Empty<float>();
        public Observable<bool> OnVibrationChanged => _vibrationToggle != null ? _vibrationToggle.OnValueChangedAsObservable() : Observable.Empty<bool>();

        public Observable<Unit> OnEnhancementPresentationClick => _enhancementPresentationButton != null ? _enhancementPresentationButton.OnClickAsObservable() : Observable.Empty<Unit>();
        public Observable<Unit> OnProtectMaterialsClick => _protectMaterialsButton != null ? _protectMaterialsButton.OnClickAsObservable() : Observable.Empty<Unit>();
        public Observable<bool> OnAutoselectMaterialsChanged => _autoselectMaterialsToggle != null ? _autoselectMaterialsToggle.OnValueChangedAsObservable() : Observable.Empty<bool>();

        public Observable<bool> OnExpeditionNoticeChanged => _expeditionNoticeToggle != null ? _expeditionNoticeToggle.OnValueChangedAsObservable() : Observable.Empty<bool>();
        public Observable<bool> OnQuicksellAlertChanged => _quicksellAlertToggle != null ? _quicksellAlertToggle.OnValueChangedAsObservable() : Observable.Empty<bool>();
        public Observable<bool> OnScreenShakeChanged => _screenShakeToggle != null ? _screenShakeToggle.OnValueChangedAsObservable() : Observable.Empty<bool>();

        public Observable<Unit> OnAccountLinkClick => _accountLinkButton != null ? _accountLinkButton.OnClickAsObservable() : Observable.Empty<Unit>();
        public Observable<Unit> OnTermsOfUseClick => _termsOfUseButton != null ? _termsOfUseButton.OnClickAsObservable() : Observable.Empty<Unit>();
    }
}