using System;
using ElementalBlacksmithStory.Data;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    /// <summary>
    /// 아이템 카테고리별 최대 가치 대비 상대적 로그 티어(0.0 ~ 1.0)를 산출하는 서비스
    /// </summary>
    public class ItemTierService
    {
        private ulong _maxWeaponValue;
        private ulong _maxMaterialValue;
        private ulong _maxRuneValue;

        public ulong MaxWeaponValue => _maxWeaponValue;
        public ulong MaxMaterialValue => _maxMaterialValue;
        public ulong MaxRuneValue => _maxRuneValue;

        [Inject]
        public ItemTierService(
            SO_WeaponDatabase weaponDatabase = null,
            SO_MaterialDatabase materialDatabase = null,
            SO_RuneDatabase runeDatabase = null)
        {
            CacheMaxValues(weaponDatabase, materialDatabase, runeDatabase);
        }

        private void CacheMaxValues(
            SO_WeaponDatabase weaponDatabase,
            SO_MaterialDatabase materialDatabase,
            SO_RuneDatabase runeDatabase)
        {
            if (weaponDatabase != null && weaponDatabase.Items != null)
            {
                var list = weaponDatabase.Items;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && list[i].basePrice > _maxWeaponValue)
                    {
                        _maxWeaponValue = list[i].basePrice;
                    }
                }
            }

            if (materialDatabase != null && materialDatabase.Items != null)
            {
                var list = materialDatabase.Items;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && list[i].Value > _maxMaterialValue)
                    {
                        _maxMaterialValue = list[i].Value;
                    }
                }
            }

            if (runeDatabase != null && runeDatabase.Items != null)
            {
                var list = runeDatabase.Items;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && list[i].Value > _maxRuneValue)
                    {
                        _maxRuneValue = list[i].Value;
                    }
                }
            }
        }

        /// <summary>
        /// 아이템의 카테고리 내 상대적 로그 티어(0.0 ~ 1.0)를 반환합니다.
        /// </summary>
        public float GetNormalizedTier(IShopItem item)
        {
            if (item == null) return 0f;

            ulong maxVal = item.Category switch
            {
                ShopItemCategory.Sword or ShopItemCategory.Bow => _maxWeaponValue,
                ShopItemCategory.Material => _maxMaterialValue,
                ShopItemCategory.Rune => _maxRuneValue,
                _ => 1
            };

            return CalculateNormalizedLogTier(item.Value, maxVal);
        }

        /// <summary>
        /// 값과 카테고리를 직접 전달하여 상대적 로그 티어(0.0 ~ 1.0)를 반환합니다.
        /// </summary>
        public float GetNormalizedTier(ulong value, ShopItemCategory category)
        {
            ulong maxVal = category switch
            {
                ShopItemCategory.Sword or ShopItemCategory.Bow => _maxWeaponValue,
                ShopItemCategory.Material => _maxMaterialValue,
                ShopItemCategory.Rune => _maxRuneValue,
                _ => 1
            };

            return CalculateNormalizedLogTier(value, maxVal);
        }

        /// <summary>
        /// 값과 카테고리 최대값을 기반으로 0.0 ~ 1.0 정규화 로그 티어를 계산합니다.
        /// </summary>
        public static float CalculateNormalizedLogTier(ulong value, ulong maxValue)
        {
            if (value <= 1 || maxValue <= 1) return 0f;
            double logVal = Math.Log10(value);
            double logMax = Math.Log10(maxValue);
            if (logMax <= 0.0001) return 0f;
            return Mathf.Clamp01((float)(logVal / logMax));
        }
    }
}

