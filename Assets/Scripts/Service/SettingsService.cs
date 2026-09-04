using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Core
{
    public class SettingsService : IStartable
    {
        private const string KEY_BGM_VOLUME = "Settings_BGM_Volume";
        private const string KEY_SFX_VOLUME = "Settings_SFX_Volume";
        private const string KEY_VIBRATION = "Settings_Vibration";
        private const string KEY_AUTOSELECT_MATERIALS = "Settings_Autoselect_Materials";
        private const string KEY_EXPEDITION_NOTICE = "Settings_Expedition_Notice";
        private const string KEY_QUICKSELL_ALERT = "Settings_Quicksell_Alert";
        private const string KEY_SCREEN_SHAKE = "Settings_Screen_Shake";

        private readonly AudioClipLoader _audioClipLoader;

        public float BGMVolume { get; private set; }
        public float SFXVolume { get; private set; }
        public bool IsVibrationEnabled { get; private set; }
        public bool IsAutoselectMaterials { get; private set; }
        public bool IsExpeditionNotice { get; private set; }
        public bool IsQuicksellAlert { get; private set; }
        public bool IsScreenShake { get; private set; }

        [Inject]
        public SettingsService(AudioClipLoader audioClipLoader)
        {
            _audioClipLoader = audioClipLoader;
            LoadSettings();
        }

        public void Start()
        {
            ApplyAudioSettings();
        }

        private void LoadSettings()
        {
            BGMVolume = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, 1.0f);
            SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 1.0f);
            IsVibrationEnabled = PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1;
            IsAutoselectMaterials = PlayerPrefs.GetInt(KEY_AUTOSELECT_MATERIALS, 1) == 1;
            IsExpeditionNotice = PlayerPrefs.GetInt(KEY_EXPEDITION_NOTICE, 1) == 1;
            IsQuicksellAlert = PlayerPrefs.GetInt(KEY_QUICKSELL_ALERT, 1) == 1;
            IsScreenShake = PlayerPrefs.GetInt(KEY_SCREEN_SHAKE, 1) == 1;
        }

        private void ApplyAudioSettings()
        {
            if (_audioClipLoader != null)
            {
                _audioClipLoader.SetBGMVolume(BGMVolume);
                _audioClipLoader.SetSFXVolume(SFXVolume);
            }
        }

        public void SetBGMVolume(float volume)
        {
            BGMVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(KEY_BGM_VOLUME, BGMVolume);
            PlayerPrefs.Save();
            if (_audioClipLoader != null)
            {
                _audioClipLoader.SetBGMVolume(BGMVolume);
            }
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, SFXVolume);
            PlayerPrefs.Save();
            if (_audioClipLoader != null)
            {
                _audioClipLoader.SetSFXVolume(SFXVolume);
            }
        }

        public void SetVibration(bool enabled)
        {
            IsVibrationEnabled = enabled;
            PlayerPrefs.SetInt(KEY_VIBRATION, enabled ? 1 : 0);
            PlayerPrefs.Save();
            // TODO: 모바일 진동 API 연동 (예: Handheld.Vibrate() 등 진동 피드백 매니저 연결)
            Debug.Log($"[SettingsService] 진동 설정 변경: {enabled}");
        }

        public void SetAutoselectMaterials(bool enabled)
        {
            IsAutoselectMaterials = enabled;
            PlayerPrefs.SetInt(KEY_AUTOSELECT_MATERIALS, enabled ? 1 : 0);
            PlayerPrefs.Save();
            // TODO: 대장간 재료 자동 선택 로직 연동
            Debug.Log($"[SettingsService] 재료 자동 선택 설정 변경: {enabled}");
        }

        public void SetExpeditionNotice(bool enabled)
        {
            IsExpeditionNotice = enabled;
            PlayerPrefs.SetInt(KEY_EXPEDITION_NOTICE, enabled ? 1 : 0);
            PlayerPrefs.Save();
            // TODO: 탐험 완료 알림 시스템 연동
            Debug.Log($"[SettingsService] 탐험 알림 설정 변경: {enabled}");
        }

        public void SetQuicksellAlert(bool enabled)
        {
            IsQuicksellAlert = enabled;
            PlayerPrefs.SetInt(KEY_QUICKSELL_ALERT, enabled ? 1 : 0);
            PlayerPrefs.Save();
            // TODO: 빠른 판매 시 확인 팝업 표시 여부 연동
            Debug.Log($"[SettingsService] 빠른 판매 알림 설정 변경: {enabled}");
        }

        public void SetScreenShake(bool enabled)
        {
            IsScreenShake = enabled;
            PlayerPrefs.SetInt(KEY_SCREEN_SHAKE, enabled ? 1 : 0);
            PlayerPrefs.Save();
            // TODO: 카메라 흔들림(Screen Shake) 효과 활성/비활성 연동
            Debug.Log($"[SettingsService] 화면 흔들림 설정 변경: {enabled}");
        }

        public void ExecuteEnhancementPresentation()
        {
            // TODO: 강화 연출 상세 설정 팝업 또는 토글 동작 구현
            Debug.Log("[SettingsService] 강화 연출 설정 클릭 (TODO)");
        }

        public void ExecuteProtectMaterials()
        {
            // TODO: 재료 보호 설정 팝업 또는 로직 구현
            Debug.Log("[SettingsService] 재료 보호 설정 클릭 (TODO)");
        }

        public void ExecuteAccountLink()
        {
            // TODO: 계정 연동 (구글 플레이 게임즈, 애플 게임센터 등) 로직 구현
            Debug.Log("[SettingsService] 계정 연동 클릭 (TODO)");
        }

        public void ExecuteTermsOfUse()
        {
            // TODO: 이용약관 웹페이지 열기 또는 인게임 약관 뷰 표시
            Debug.Log("[SettingsService] 이용약관 클릭 (TODO)");
            // Application.OpenURL("https://example.com/terms");
        }
    }
}
