using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 상점에서 장비(무기) 아이템을 전용으로 표시하는 뷰 컴포넌트입니다.
    /// ShopItemViewBase를 상속하며, 수량(Amount) 대신 가로 2슬롯 크기에 맞춘 룬 소켓(최대 3개) 표시 기능을 제공합니다.
    /// </summary>
    [AddComponentMenu("UI/Equipment Shop Item View")]
    public class EquipmentShopItemView : ShopItemViewBase
    {
        [Header("Rune Slots")]
        [Tooltip("3개 룬 소켓의 루트 오브젝트 (해금 여부에 따라 활성/비활성화)")]
        [SerializeField] private GameObject[] _runeSlotRoots;

        [Tooltip("각 소켓에 장착된 룬의 아이콘을 표시하는 Image")]
        [SerializeField] private Image[] _runeIconImages;

        [Tooltip("빈 소켓일 때 표시할 기본 배경/아이콘 스프라이트 (선택 사항)")]
        [SerializeField] private Sprite _emptySlotSprite;

        /// <summary>
        /// 해금된 룬 소켓 수(0~3)에 따라 소켓 활성화 상태를 갱신합니다.
        /// </summary>
        public void SetRuneSlotCount(int unlockedCount)
        {
            if (_runeSlotRoots == null) return;

            for (int i = 0; i < _runeSlotRoots.Length; i++)
            {
                if (_runeSlotRoots[i] != null)
                {
                    _runeSlotRoots[i].SetActive(i < unlockedCount);
                }
            }
        }

        /// <summary>
        /// 특정 룬 소켓에 장착된 룬 아이콘을 적용합니다. (null이면 비표시 또는 빈 슬롯)
        /// </summary>
        public void SetRuneIcon(int socketIndex, Sprite runeIcon)
        {
            if (_runeIconImages == null || socketIndex < 0 || socketIndex >= _runeIconImages.Length)
            {
                return;
            }

            var iconImage = _runeIconImages[socketIndex];
            if (iconImage == null) return;

            if (runeIcon != null)
            {
                iconImage.sprite = runeIcon;
                iconImage.enabled = true;
            }
            else if (_emptySlotSprite != null)
            {
                iconImage.sprite = _emptySlotSprite;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        /// <summary>
        /// 해금된 소켓 수 및 각 소켓의 룬 아이콘들을 일괄 적용합니다.
        /// </summary>
        public void SetRuneSlots(int unlockedCount, Sprite rune0 = null, Sprite rune1 = null, Sprite rune2 = null)
        {
            SetRuneSlotCount(unlockedCount);
            SetRuneIcon(0, rune0);
            SetRuneIcon(1, rune1);
            SetRuneIcon(2, rune2);
        }
    }
}
