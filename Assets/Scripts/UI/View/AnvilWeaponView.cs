using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ElementalBlacksmithStory
{
    public class AnvilWeaponView : MonoBehaviour
    {
        [SerializeField] private Image weaponImage;
        [SerializeField] private TextMeshProUGUI weaponName;
        public void ChangeSprite(Sprite sprite)
        {
            weaponImage.sprite = sprite;
        }
        public void ChangeWeaponName(string weaponName)
        {
            this.weaponName.text = weaponName;
        }
    }
}