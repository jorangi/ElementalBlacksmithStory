using UnityEngine;
using TMPro;

namespace ElementalBlacksmithStory.UI
{
    public class EnhanceChanceView : MonoBehaviour
    {
        [SerializeField]private TextMeshProUGUI chanceText;

        public void SetChance(float chance)
        {
            chanceText.text = (chance * 100f).ToString("F2") + "%" ;
        }
    }
}