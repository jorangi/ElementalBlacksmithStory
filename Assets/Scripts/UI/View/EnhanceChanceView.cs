using UnityEngine;
using TMPro;
using Cysharp.Text;

namespace ElementalBlacksmithStory.UI
{
    public class EnhanceChanceView : MonoBehaviour
    {
        [SerializeField]private TextMeshProUGUI chanceText;

        public void SetChance(float chance, ulong cost)
        {
            chanceText.SetText(
                ZString.Format("{0:F2}% <size=\"40%\">(-{1:N0} <sprite name=\"CoinSack\">)</size>",
                chance * 100.0f,
                cost));
        }
    }
}