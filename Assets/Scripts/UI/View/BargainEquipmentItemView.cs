using Cysharp.Text;

namespace ElementalBlacksmithStory.UI
{
    public class BargainEquipmentItemView : EquipmentShopItemView
    {
        public override void SetPrice(ulong price)
        {
            if (_priceText != null)
            {
                _priceText.SetText(ZString.Format("<color=grey>거래 신청인:</color> {0:N0}", price));
            }
        }
    }
}