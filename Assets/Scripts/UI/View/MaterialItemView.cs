using UnityEngine;
using UnityEngine.UI;
using R3;
using TMPro;
using Cysharp.Text;

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

        public void SetData(uint id, uint count)
        {
            MaterialId = id;
            MaterialCount = count;
            countText?.SetText(ZString.Format("x{0:N0}", count));
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
            checkedImage?.gameObject.SetActive(true);
            IsSelected = true;
        }
        public void UnCheck()
        {
            checkedImage?.gameObject.SetActive(false);
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
            countText?.SetText(ZString.Format("x{0:N0}", amount));
        }
    }
}
