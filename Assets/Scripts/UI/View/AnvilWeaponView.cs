using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ElementalBlacksmithStory
{
    public class AnvilWeaponView : MonoBehaviour
    {
        [SerializeField] private Image weaponImage;
        [SerializeField] private TextMeshProUGUI weaponName;
        private Material _defaultMaterial;

        private void Awake()
        {
            if (weaponImage != null)
            {
                _defaultMaterial = weaponImage.material;
                weaponImage.enabled = weaponImage.sprite != null;
            }
        }

        public void ChangeSprite(Sprite sprite, bool hideMat = false)
        {
            if (weaponImage == null) return;

            weaponImage.sprite = sprite;
            weaponImage.enabled = sprite != null;

            if (hideMat)
            {
                weaponImage.material = null;
            }
            else if (_defaultMaterial != null)
            {
                weaponImage.material = _defaultMaterial;
            }
        }

        public void ChangeWeaponName(string name)
        {
            if (weaponName != null)
            {
                weaponName.text = name;
            }
        }
    }
}