using System.Collections.Generic;
using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Inventory
{
    /// <summary>
    /// 룬 아이템 전용 인벤토리
    /// </summary>
    public class RuneInventory : BaseInventory<SO_RuneData>
    {
        private readonly HashSet<uint> _favoriteRuneIds = new();
        /// <summary>
        /// 이미 장착 중인 룬을 조회하기 위한 Dictionary
        /// </summary>
        private readonly Dictionary<uint, uint> _equipedDic = new();

        public bool IsFavorite(uint runeId) => _favoriteRuneIds.Contains(runeId);

        public bool ToggleFavorite(uint runeId)
        {
            if (_favoriteRuneIds.Contains(runeId))
            {
                _favoriteRuneIds.Remove(runeId);
                return false;
            }
            else
            {
                _favoriteRuneIds.Add(runeId);
                return true;
            }
        }

        public void SetFavorite(uint runeId, bool isFavorite)
        {
            if (isFavorite)
            {
                _favoriteRuneIds.Add(runeId);
            }
            else
            {
                _favoriteRuneIds.Remove(runeId);
            }
        }
        public int GetCount(uint id)
        {
            foreach (var kvp in _inventory)
            {
                if (kvp.Key.Id == id)
                {
                    return (int)kvp.Value;
                }
            }
            return 0;
        }
        public IReadOnlyCollection<uint> FavoriteRuneIds => _favoriteRuneIds;
    }
}
