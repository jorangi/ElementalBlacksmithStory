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
        public readonly CompositeDisposable _disposables = new();
        private bool _isOpened = false;

        private void Awake()
        {
            if (button != null && !button.TryGetComponent<UIButtonSound>(out _))
            {
                button.gameObject.AddComponent<UIButtonSound>();
            }
        }

        public Observable<Unit> OnClickAsObservable() => button != null ? button.OnClickAsObservable() : Observable.Empty<Unit>();
        public void SetOpen(bool isOpened)
        {
            _isOpened = isOpened;
            if (nameText != null && WeaponData != null)
            {
                nameText.SetText(isOpened ? WeaponData.weaponName : "?????");
            }
            if (iconImage != null)
            {
                iconImage.color = isOpened ? Color.white : Color.black;
                iconImage.enabled = iconImage.sprite != null;
            }
            if (highlight_Opened != null) highlight_Opened.SetActive(_isOpened);
            if (highlight_Destination != null) highlight_Destination.SetActive(false);
            if (highlight_Route != null) highlight_Route.SetActive(false);
        }
        public void SetDestinationNode()
        {
            if (highlight_Opened != null) highlight_Opened.SetActive(false);
            if (highlight_Destination != null) highlight_Destination.SetActive(true);
            if (highlight_Route != null) highlight_Route.SetActive(false);
        }
        public void SetRouteNode()
        {
            if (highlight_Opened != null) highlight_Opened.SetActive(false);
            if (highlight_Destination != null) highlight_Destination.SetActive(false);
            if (highlight_Route != null) highlight_Route.SetActive(true);
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
            if (nameText != null && data != null) 
                nameText.SetText(data.weaponName);
            
            if (iconImage != null)
            {
                iconImage.enabled = false;
            }

            if (iconImage != null && spriteLoader != null && data != null)
            {
                Sprite weaponSprite = await spriteLoader.GetWeaponSprite(data.Id.ToString(), cancellationToken);
                if (weaponSprite != null)
                {
                    iconImage.sprite = weaponSprite;
                    iconImage.enabled = true;
                }
            }
            
            SetOpen(true);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}