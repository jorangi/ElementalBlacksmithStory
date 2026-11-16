using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class MercenaryInfoView : MonoBehaviour
    {
        [SerializeField] private Color _panelColor;
        [SerializeField] private Image _panel;
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Button _closeButton;
        private const float ANIM_DURATION = 0.3f;

        [Header("장비 슬롯")]
        [SerializeField] private Button _weaponButton;
        [SerializeField] private Image _weaponIcon;

        [SerializeField] private Button _accessory1Button;
        [SerializeField] private Image _accessoryIcon1;

        [SerializeField] private Button _accessory2Button;
        [SerializeField] private Image _accessoryIcon2;

        [SerializeField] private Button _accessory3Button;
        [SerializeField] private Image _accessory3Icon;

        [SerializeField] private Button _accessory4Button;
        [SerializeField] private Image _accessory4Icon;

        [Header("유닛 정보")]
        [SerializeField] private Image _standingImage;

        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;

        [SerializeField] private Image _expBar;

        [Header("유닛 스탯")]
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _defText;
        [SerializeField] private TextMeshProUGUI _atkText;
        [SerializeField] private TextMeshProUGUI _magText;
        [SerializeField] private TextMeshProUGUI _atsText;
        [SerializeField] private TextMeshProUGUI _spdText;
        [SerializeField] private TextMeshProUGUI _goldBonusText;

        [Header("애니메이션")]
        private MotionHandle _handle;
        private readonly CancellationTokenSource _cts = new();

        public void SetStandingImage(Sprite image)
        {
            _standingImage.sprite = image;
        }
        public void SetWeaponIcon(Sprite icon)
        {
            _weaponIcon.sprite = icon;
        }
        public void SetAccessory1Icon(Sprite icon)
        {
            _accessoryIcon1.sprite = icon;
        }
        public void SetAccessory2Icon(Sprite icon)
        {
            _accessoryIcon2.sprite = icon;
        }
        public void SetAccessory3Icon(Sprite icon)
        {
            _accessory3Icon.sprite = icon;
        }
        public void SetAccessory4Icon(Sprite icon)
        {
            _accessory4Icon.sprite = icon;
        }
        public void SetNameText(string name)
        {
            _nameText.text = name;
        }
        public void SetLevelText(string text)
        {
            _levelText.SetText(text);
        }
        public void SetExpBar(float exp)
        {
            _expBar.fillAmount = exp;
        }
        public void SetHpText(string text)
        {
            _hpText.SetText(text);
        }
        public void SetDefText(string text)
        {
            _defText.SetText(text);
        }
        public void SetAtkText(string text)
        {
            _atkText.SetText(text);
        }
        public void SetMagText(string text)
        {
            _magText.SetText(text);
        }
        public void SetAtsText(string text)
        {
            _atsText.SetText(text);
        }
        public void SetSpdText(string text)
        {
            _spdText.SetText(text);
        }
        public void SetGoldBonusText(string text)
        {
            _goldBonusText.SetText(text);
        }

        public Observable<Unit> OnCloseButtonClick()
        {
            if (_closeButton == null)
            {
                Debug.LogError("[MercenaryInfoView]_closeButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _closeButton.OnClickAsObservable();
        }
        public Observable<Unit> OnWeaponButtonClick()
        {
            if (_weaponButton == null)
            {
                Debug.LogError("[MercenaryInfoView]_weaponButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _weaponButton.OnClickAsObservable();
        }
        public Observable<Unit> OnAccessory1ButtonClick()
        {
            if (_accessory1Button == null)
            {
                Debug.LogError("[MercenaryInfoView]_accessory1Button 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _accessory1Button.OnClickAsObservable();
        }
        public Observable<Unit> OnAccessory2ButtonClick()
        {
            if (_accessory2Button == null)
            {
                Debug.LogError("[MercenaryInfoView]_accessory2Button 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _accessory2Button.OnClickAsObservable();
        }
        public Observable<Unit> OnAccessory3ButtonClick()
        {
            if (_accessory3Button == null)
            {
                Debug.LogError("[MercenaryInfoView]_accessory3Button이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _accessory3Button.OnClickAsObservable();
        }
        public Observable<Unit> OnAccessory4ButtonClick()
        {
            if (_accessory4Button == null)
            {
                Debug.LogError("[MercenaryInfoView]_accessory4Button이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _accessory4Button.OnClickAsObservable();
        }

        public async UniTask Show()
        {
            _panel.gameObject.SetActive(true);
            if (_handle.IsActive())
            {
                _handle.Cancel();
            }
            _handle = LSequence.Create()
                .Append(LMotion.Create(Color.clear, _panelColor, ANIM_DURATION)
                                .WithEase(Ease.InOutBack)
                                .BindToColor(_panel))
                .Append(LMotion.Create(Vector3.zero, Vector3.one, ANIM_DURATION)
                                .WithEase(Ease.InOutBack)
                                .BindToLocalScale(_rect))
                .Run()
                .AddTo(_panel.gameObject);

            await _handle.ToUniTask(_cts.Token)
                        .SuppressCancellationThrow();
        }
        public async UniTask Hide()
        {
            if (_handle.IsActive())
            {
                _handle.Cancel();
            }
            _handle = LSequence.Create()
                .Append(LMotion.Create(Vector3.one, Vector3.zero, ANIM_DURATION)
                                .WithEase(Ease.InOutBack)
                                .BindToLocalScale(_rect))
                .Append(LMotion.Create(_panelColor, Color.clear, ANIM_DURATION)
                                .WithEase(Ease.InOutBack)
                                .BindToColor(_panel))
                .Run()
                .AddTo(_panel.gameObject);

            await _handle.ToUniTask(_cts.Token)
                        .SuppressCancellationThrow();

            _panel.gameObject.SetActive(false);
        }
    }
}