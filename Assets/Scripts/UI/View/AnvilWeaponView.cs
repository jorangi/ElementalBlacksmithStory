using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ElementalBlacksmithStory
{
    public class AnvilWeaponView : MonoBehaviour
    {
        [SerializeField] private Image weaponImage;
        [SerializeField] private TextMeshProUGUI weaponName;
        public void ChangeSprite(Sprite sprite, bool hideMat = false)
        {
            weaponImage.sprite = sprite;
            if(hideMat)
            {
                weaponImage.material = null;
            }
        }
        public void ChangeWeaponName(string weaponName)
        {
            this.weaponName.text = weaponName;
        }
    }
}