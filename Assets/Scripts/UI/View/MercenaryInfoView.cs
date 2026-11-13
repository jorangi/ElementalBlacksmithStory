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
        private const float ANIM_DURATION = 0.3f;

        [Header("장비 슬롯")]
        [SerializeField] private Button _weaponButton;
        [SerializeField] private Image _weaponIcon;

        [SerializeField] private Button _armorButton;
        [SerializeField] private Image _armorIcon;

        [SerializeField] private Button _accessoryButton1;
        [SerializeField] private Image _accessoryIcon1;

        [SerializeField] private Button _accessoryButton2;
        [SerializeField] private Image _accessoryIcon2;


        [SerializeField] private Button _consumableButton;
        [SerializeField] private Image _consumableIcon;

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
        public void SetArmorIcon(Sprite icon)
        {
            _armorIcon.sprite = icon;
        }
        public void SetAccessory1Icon(Sprite icon)
        {
            _accessoryIcon1.sprite = icon;
        }
        public void SetAccessory2Icon(Sprite icon)
        {
            _accessoryIcon2.sprite = icon;
        }
        public void SetConsumableIcon(Sprite icon)
        {
            _consumableIcon.sprite = icon;
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

        public Observable<Unit> OnWeaponButtonClick()
        {
            if (_weaponButton == null)
            {
                Debug.LogError("[MercenaryInfoView]_weaponButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _weaponButton.OnClickAsObservable();
        }
        public Observable<Unit> OnArmorButtonClick()
        {
            if (_armorButton == null)
            {
                Debug.LogError("[MercenaryInfoView]_armorButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _armorButton.OnClickAsObservable();
        }
        public Observable<Unit> OnAccessory1ButtonClick()
        {
            if (_accessoryButton1 == null)
            {
                Debug.LogError("[MercenaryInfoView]_accessoryButton1이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _accessoryButton1.OnClickAsObservable();
        }
        public Observable<Unit> OnAccessory2ButtonClick()
        {
            if (_accessoryButton2 == null)
            {
                Debug.LogError("[MercenaryInfoView]_accessoryButton2이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _accessoryButton2.OnClickAsObservable();
        }
        public Observable<Unit> OnConsumableButtonClick()
        {
            if (_consumableButton == null)
            {
                Debug.LogError("[MercenaryInfoView]_consumableButton이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _consumableButton.OnClickAsObservable();
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
                .Append(LMotion.Create(_panelColor, Color.clear, ANIM_DURATION)
                                .WithEase(Ease.InOutBack)
                                .BindToColor(_panel))
                .Append(LMotion.Create(Vector3.one, Vector3.zero, ANIM_DURATION)
                                .WithEase(Ease.InOutBack)
                                .BindToLocalScale(_rect))
                .Run()
                .AddTo(_panel.gameObject);

            await _handle.ToUniTask(_cts.Token)
                        .SuppressCancellationThrow();

            _panel.gameObject.SetActive(false);
        }
    }
}