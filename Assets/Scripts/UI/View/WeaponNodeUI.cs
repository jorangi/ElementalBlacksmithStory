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
        [SerializeField] private GameObject highlight_Opened;
        [SerializeField] private GameObject highlight_Destination;
        [SerializeField] private GameObject highlight_Route;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Button button;        
        public SO_WeaponData WeaponData { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;
        public CompositeDisposable _disposables = new();
        private bool _isOpened = false;
        public Observable<Unit> OnClickAsObservable() => button.OnClickAsObservable();
        public void SetOpen(bool isOpened)
        {
            _isOpened = isOpened;
            nameText.SetText(isOpened ? WeaponData.weaponName : "?????");
            iconImage.color = isOpened ? Color.white : Color.black;
            if(_isOpened)
            {
                highlight_Opened.SetActive(true);
                highlight_Destination.SetActive(false);
                highlight_Route.SetActive(false);
            }
            else
            {
                highlight_Opened.SetActive(false);
                highlight_Destination.SetActive(false);
                highlight_Route.SetActive(false);
            }
        }
        public void SetDestinationNode()
        {
            highlight_Opened.SetActive(false);
            highlight_Destination.SetActive(true);
            highlight_Route.SetActive(false);
        }
        public void SetRouteNode()
        {
            highlight_Opened.SetActive(false);
            highlight_Destination.SetActive(false);
            highlight_Route.SetActive(true);
        }
        public void SetNormalNode()
        {
            SetOpen(true);
        }
        public void SetLockedNode()
        {
            SetOpen(false);
        }
        public async UniTask Setup(SO_WeaponData data, WeaponSpriteLoader spriteLoader, CancellationToken cancellationToken = default)
        {
            WeaponData = data;
            if (nameText != null) 
                nameText.SetText(data.weaponName);
            
            if (iconImage != null && spriteLoader != null && data != null)
            {
                Sprite weaponSprite = await spriteLoader.GetWeaponSprite(data.Id.ToString(), cancellationToken);
                if (weaponSprite != null)
                {
                    iconImage.sprite = weaponSprite;
                }
            }
            
            SetOpen(true);
        }
    }
}