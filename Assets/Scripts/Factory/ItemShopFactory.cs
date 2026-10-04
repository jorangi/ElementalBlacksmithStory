using ElementalBlacksmithStory.Data;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    /// <summary>
    /// ID를 분석하여 적절한 도메인 객체(Weapon, MaterialShopItem, Rune 등)를 IShopItem으로 생성하는 팩토리 클래스
    /// </summary>
    public sealed class ItemShopFactory : IItemShopFactory
    {
        private readonly SO_MaterialDatabase _materialDatabase;
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly SO_RuneDatabase _runeDatabase;

        [Inject]
        public ItemShopFactory(
            SO_MaterialDatabase materialDatabase,
            SO_WeaponDatabase weaponDatabase,
            SO_RuneDatabase runeDatabase = null)
        {
            _materialDatabase = materialDatabase;
            _weaponDatabase = weaponDatabase;
            _runeDatabase = runeDatabase;
        }

        public IShopItem Create(uint id, uint stock = 1, float margin = 1.0f)
        {
            ItemCategory category = ItemIdHelper.GetCategory(id);

            switch (category)
            {
                case ItemCategory.Material:
                    if (_materialDatabase != null)
                    {
                        var mat = _materialDatabase.Get(id);
                        if (mat != null)
                        {
                            return new MaterialShopItem(mat, stock);
                        }
                    }
                    break;

                case ItemCategory.Weapon:
                    if (_weaponDatabase != null)
                    {
                        var wep = _weaponDatabase.Get(id);
                        if (wep != null)
                        {
                            var weapon = new Weapon(wep);
                            if (margin > 0)
                            {
                                weapon.SetMargin(margin);
                            }
                            return weapon;
                        }
                    }
                    break;

                case ItemCategory.Rune:
                    if (_runeDatabase != null)
                    {
                        var runeData = _runeDatabase.Get(id);
                        if (runeData != null)
                        {
                            return new Rune(runeData);
                        }
                    }
                    break;
            }

            return null;
        }
    }
}
