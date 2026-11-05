using Cysharp.Text;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class MaterialItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image checkedImage;
        public bool IsSelected { get; private set; } = false;
        public uint MaterialId { get; private set; }
        public uint MaterialCount { get; private set; }

        private void Awake()
        {
            if (button != null && !button.TryGetComponent<UIButtonSound>(out _))
            {
                button.gameObject.AddComponent<UIButtonSound>();
            }
        }

        public void SetData(uint id, uint count)
        {
            MaterialId = id;
            MaterialCount = count;
            if (countText != null)
                countText.SetText(ZString.Format("x{0:N0}", count));
        }

        public void SetIcon(Sprite icon)
        {
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
        }
        public void SelectOnce()
        {
            Check();
            Debug.Log($"[MaterialItemView] {MaterialId} 재료 1개 선택됨 (체크 표시)");
        }
        public void Check()
        {
            if (checkedImage != null)
                checkedImage.gameObject.SetActive(true);
            IsSelected = true;
        }
        public void UnCheck()
        {
            if (checkedImage != null)
                checkedImage.gameObject.SetActive(false);
            IsSelected = false;
        }
        public uint Select()
        {
            return MaterialId;
        }
        public Observable<Unit> OnClickAsObservable()
        {
            if (button == null)
            {
                Debug.LogError("[MaterialItemView] button이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return button.OnClickAsObservable();
        }
        public void SetAmount(uint amount)
        {
            if (countText != null)
                countText.SetText(ZString.Format("x{0:N0}", amount));
        }
    }
}
