using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ElementalBlacksmithStory.Data;
using Cysharp.Threading.Tasks;
using System.Threading;
using R3;
using MessagePipe;
using ElementalBlacksmithStory.Events;

namespace ElementalBlacksmithStory.UI
{
    public class WeaponNodeUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Button button;        
        public SO_WeaponData WeaponData { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;
        public CompositeDisposable _disposables = new();
        private bool _isFlag = false;
        public Observable<Unit> OnClickAsObservable() => button.OnClickAsObservable();
        public async UniTask Setup(SO_WeaponData data, WeaponSpriteLoader spriteLoader, CancellationToken cancellationToken = default)
        {
            WeaponData = data;
            if (nameText != null) 
                nameText.text = data.weaponName;
            
            if (iconImage != null && spriteLoader != null && data != null)
            {
                Sprite weaponSprite = await spriteLoader.GetWeaponSprite(data.Id.ToString(), cancellationToken);
                if (weaponSprite != null)
                {
                    iconImage.sprite = weaponSprite;
                }
            }
        }
    }
}