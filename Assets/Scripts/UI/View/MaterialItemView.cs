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
        [SerializeField] private TextMeshProUGUI checkedText;

        public uint MaterialId { get; private set; }
        public uint MaterialCount { get; private set; }

        public void SetData(uint id, uint count)
        {
            MaterialId = id;
            MaterialCount = count;
            
            //SetIcon();
            if (countText != null)
            {
                countText.SetText(ZString.Format("x{0:N0}", count));
            }
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
            if (checkedText != null)
            {
                checkedText.gameObject.SetActive(true);
            }
            Debug.Log($"[MaterialItemView] {MaterialId} 재료 1개 선택됨 (체크 표시)");
        }

        public void Select()
        {
            Debug.Log($"[MaterialItemView] {MaterialId} 재료 다수({MaterialCount}개) 선택됨 (개수 조정 모달)");
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
    }
}
