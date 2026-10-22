using UnityEngine;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// FlowLayoutGroup 그리드 상에서 아이템이 차지할 열(슬롯) 수를 명시적으로 지정하는 컴포넌트입니다.
    /// 기본값 1 (룬, 재료 등 일반 카드), 2 (무기 등 와이드 장비 카드)
    /// </summary>
    [AddComponentMenu("Layout/Shop Layout Item")]
    [DisallowMultipleComponent]
    public class ShopLayoutItem : MonoBehaviour
    {
        [Tooltip("그리드에서 차지할 열(슬롯) 수. 일반 아이템: 1, 장비/무기: 2")]
        [SerializeField] private int _colSpan = 1;

        public int ColSpan
        {
            get => _colSpan;
            set => _colSpan = Mathf.Max(1, value);
        }
    }
}
