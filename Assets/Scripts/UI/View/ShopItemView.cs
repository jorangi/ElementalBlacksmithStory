using UnityEngine;
using TMPro;
using Cysharp.Text;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 일반 상점 아이템(룬, 재료 등) 카드 뷰입니다.
    /// 보유 수량(_amountInPocketText) 표시를 지원합니다.
    /// </summary>
    public class ShopItemView : ShopItemViewBase
    {
        [Header("Normal Item Specific")]
        [Tooltip("보유 수량 텍스트 (예: '(1,000개 보유)')")]
        [SerializeField] private TextMeshProUGUI _amountInPocketText;

        public override void SetData(uint itemId, string itemName, ulong price, Sprite icon, bool isUniqueItem = false)
        {
            base.SetData(itemId, itemName, price, icon, isUniqueItem);
            if (isUniqueItem && _amountInPocketText != null)
            {
                _amountInPocketText.gameObject.SetActive(false);
            }
        }

        public override void SetAmountInPocket(uint amount)
        {
            if (IsUniqueItem || _amountInPocketText == null) return;
            _amountInPocketText.SetText(ZString.Format("({0:N0} 보유)", amount));
        }
    }
}
