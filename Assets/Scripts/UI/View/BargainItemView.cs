using Cysharp.Text;
using TMPro;

namespace ElementalBlacksmithStory.UI
{
    public class BargainItemView : ShopItemViewBase
    {
        public override void SetPrice(ulong price)
        {
            Price = price;
            if (_priceText != null)
            {
                _priceText.SetText(ZString.Format("<color=grey>거래 신청인:</color> {0:N0}", price));
            }
        }
    }
}