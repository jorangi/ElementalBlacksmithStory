using ElementalBlacksmithStory.Data;
using UnityEngine;

namespace ElementalBlacksmithStory.Core
{
    /// <summary>
    /// 강화 실패 시 무기 복원(롤백) 및 강등 처리를 전담하는 비즈니스 서비스
    /// </summary>
    public class WeaponRollbackService
    {
        private readonly SO_WeaponDatabase _weaponDatabase;

        public WeaponRollbackService(SO_WeaponDatabase weaponDatabase)
        {
            _weaponDatabase = weaponDatabase;
        }

        /// <summary>
        /// 하드모드 등에서 무기를 이전 단계로 1단계 강등 복원합니다.
        /// 기본 무기(0강)인 경우 false를 반환하고 아무 작업도 하지 않습니다.
        /// </summary>
        public bool TryRollbackOneStep(Weapon weapon)
        {
            if (weapon == null || weapon.EnhanceHistory.Count == 0)
            {
                return false;
            }

            var popped = weapon.EnhanceHistory.Pop();
            ulong newBasePrice = weapon.BasePrice >= popped.AddedBasePrice

                ? weapon.BasePrice - popped.AddedBasePrice

                : 0;
            weapon.SetBasePrice(newBasePrice);

            uint prevWeaponId;
            ulong prevCost;
            if (weapon.EnhanceHistory.Count > 0)
            {
                var top = weapon.EnhanceHistory.Peek();
                prevWeaponId = top.WeaponId;
                prevCost = top.Cost;
            }
            else
            {
                prevWeaponId = weapon.BaseWeaponData != null ? weapon.BaseWeaponData.Id : weapon.WeaponId;
                prevCost = weapon.BaseWeaponData != null ? weapon.BaseWeaponData.cost : 0;
            }

            weapon.SetCost(prevCost);
            ApplyWeaponData(weapon, prevWeaponId);
            return true;
        }

        /// <summary>
        /// 레시피 실패 시 지정된 실패 결과 무기(defaultFailWeapon)로 롤백합니다.
        /// </summary>
        public void RollbackTo(Weapon weapon, SO_WeaponData targetFailWeapon)
        {
            if (weapon == null || targetFailWeapon == null) return;

            while (weapon.EnhanceHistory.Count > 0 && weapon.EnhanceHistory.Peek().WeaponId != targetFailWeapon.Id)
            {
                var popped = weapon.EnhanceHistory.Pop();
                ulong newBasePrice = weapon.BasePrice >= popped.AddedBasePrice
                    ? weapon.BasePrice - popped.AddedBasePrice
                    : 0;
                weapon.SetBasePrice(newBasePrice);
            }

            weapon.SetCost(targetFailWeapon.cost);
            ApplyWeaponData(weapon, targetFailWeapon.Id, targetFailWeapon);
        }

        private void ApplyWeaponData(Weapon weapon, uint weaponId, SO_WeaponData cachedData = null)
        {
            SO_WeaponData weaponData = cachedData ?? _weaponDatabase.Get(weaponId);
            if (weaponData != null)
            {
                weapon.SetData(weaponData);
            }

            float margin = weapon.Data != null && weapon.Data.margin > 1f
                ? Random.Range(1.0f, weapon.Data.margin)
                : (weapon.Data != null ? weapon.Data.margin : 1f);

            weapon.SetMargin(margin);
        }
    }
}
