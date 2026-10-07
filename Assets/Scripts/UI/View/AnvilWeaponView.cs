using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory
{
    public class AnvilWeaponView : MonoBehaviour
    {
        [SerializeField] private Image weaponImage;
        [SerializeField] private TextMeshProUGUI weaponName;
        [Header("룬 소켓 슬롯")]
        [SerializeField] private Image[] runeSlotImages = new Image[3];
        [SerializeField] private Sprite emptySlotSprite;

        private Material _defaultMaterial;

        private void Awake()
        {
            if (weaponImage != null)
            {
                _defaultMaterial = weaponImage.material;
                weaponImage.enabled = weaponImage.sprite != null;
            }

            if (runeSlotImages != null)
            {
                for (int i = 0; i < runeSlotImages.Length; i++)
                {
                    if (runeSlotImages[i] != null)
                    {
                        if (emptySlotSprite == null && runeSlotImages[i].sprite != null)
                        {
                            emptySlotSprite = runeSlotImages[i].sprite;
                        }
                        runeSlotImages[i].gameObject.SetActive(false);
                    }
                }
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

        /// <summary>
        /// 해금된 룬 슬롯 수에 따라 슬롯 표시/숨김 설정 (0 ~ 3)
        /// </summary>
        public void SetRuneSlotCount(int slotCount)
        {
            if (runeSlotImages == null) return;
            for (int i = 0; i < runeSlotImages.Length; i++)
            {
                if (runeSlotImages[i] != null)
                {
                    runeSlotImages[i].gameObject.SetActive(i < slotCount);
                }
            }
        }

        /// <summary>
        /// 특정 소켓에 룬 스프라이트 적용 (null이면 빈 소켓 스프라이트 emptySlotSprite 복원)
        /// </summary>
        public void SetRune(int socketIndex, Sprite runeSprite)
        {
            if (runeSlotImages == null || socketIndex < 0 || socketIndex >= runeSlotImages.Length) return;

            var slotImage = runeSlotImages[socketIndex];
            if (slotImage == null) return;

            slotImage.sprite = runeSprite != null ? runeSprite : emptySlotSprite;
        }

        /// <summary>
        /// 3개 슬롯의 룬 스프라이트 상태 갱신 (슬롯 비어있을 시 빈 슬롯 이미지 적용)
        /// </summary>
        public void ChangeRuneStatus(Sprite rune1, Sprite rune2, Sprite rune3)
        {
            SetRune(0, rune1);
            SetRune(1, rune2);
            SetRune(2, rune3);
        }

        /// <summary>
        /// 해금된 슬롯 수 및 3개 룬 스프라이트 일괄 갱신
        /// </summary>
        public void ChangeRuneStatus(int slotCount, Sprite rune1, Sprite rune2, Sprite rune3)
        {
            SetRuneSlotCount(slotCount);
            SetRune(0, rune1);
            SetRune(1, rune2);
            SetRune(2, rune3);
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