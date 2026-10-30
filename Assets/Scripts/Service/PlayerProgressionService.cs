using System;
using ElementalBlacksmithStory.Data;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    /// <summary>
    /// SO_ProgressionSettings에 정의된 항목별 배점(최소 단위 1점)과 구간 기준점을 바탕으로
    /// 플레이어의 종합 진행도 점수를 ulong으로 산출하는 서비스 클래스
    /// </summary>
    public sealed class PlayerProgressionService : IPlayerProgressionService
    {
        private readonly PlayerData _playerData;
        private readonly SO_ProgressionSettings _settings;
        private readonly ForgeManager _forgeManager;
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly SO_MaterialDatabase _materialDatabase;

        private ulong _cachedMaxMaterialValue;
        private ulong _cachedMaxWeaponValue;

        public ulong MidGameScoreThreshold => _settings != null ? _settings.MidGameScoreThreshold : 500UL;
        public ulong EndGameScoreThreshold => _settings != null ? _settings.EndGameScoreThreshold : 3000UL;

        [Inject]
        public PlayerProgressionService(
            PlayerData playerData,
            SO_ProgressionSettings settings = null,
            ForgeManager forgeManager = null,
            SO_WeaponDatabase weaponDatabase = null,
            SO_MaterialDatabase materialDatabase = null)
        {
            _playerData = playerData;
            _settings = settings;
            _forgeManager = forgeManager;
            _weaponDatabase = weaponDatabase;
            _materialDatabase = materialDatabase;
        }

        public ulong GetTotalProgressionScore()
        {
            if (_playerData == null) return 0UL;

            EnsureMaxValuesCached();

            ulong wealthScore = CalculateWealthScore();
            ulong runeScore = CalculateRuneScore();
            ulong weaponScore = CalculateWeaponScore();
            ulong materialScore = CalculateMaterialScore();

            return wealthScore + runeScore + weaponScore + materialScore;
        }

        /// <summary>
        /// ulong 총점을 중반부 기준점(0.5) 및 극후반부 기준점(1.0)을 거치는 구간 비율(0.0 ~ 1.0)로 변환합니다.
        /// </summary>
        public float GetProgressionRatio()
        {
            ulong score = GetTotalProgressionScore();
            ulong midThreshold = MidGameScoreThreshold;
            ulong endThreshold = EndGameScoreThreshold;

            if (score == 0) return 0f;
            if (score >= endThreshold) return 1.0f;

            if (score <= midThreshold)
            {
                return 0.5f * ((float)score / midThreshold);
            }

            ulong span = endThreshold - midThreshold;
            return 0.5f + 0.5f * ((float)(score - midThreshold) / span);
        }

        private ulong CalculateWealthScore()
        {
            ulong money = _playerData.Money.CurrentValue;
            ulong startGold = _settings != null ? _settings.WealthStartGold : 1000UL;
            ulong endgameGold = _settings != null ? _settings.WealthEndgameGold : 1000000000UL;
            ulong maxWealthScore = _settings != null ? _settings.MaxWealthScore : 3000UL;

            if (money < startGold) return 0UL;

            // 거상 기준 골드(예: 10억) 초과 시: 소수의 괴물 유저로 인해 점수가 무한 폭주하지 않도록 로그 Soft Cap 적용
            if (money >= endgameGold)
            {
                double excessRatio = (double)money / endgameGold;
                ulong softCapBonus = (ulong)Math.Round(Math.Log10(excessRatio) * (maxWealthScore * 0.1));
                return maxWealthScore + softCapBonus;
            }

            double logMin = Math.Log10(startGold);
            double logMax = Math.Log10(endgameGold);
            double logCur = Math.Log10(money);

            double t = Math.Clamp((logCur - logMin) / (logMax - logMin), 0.0, 1.0);
            ulong calculated = (ulong)Math.Round(maxWealthScore * (t * t));
            return calculated > 0 ? calculated : 1UL;
        }

        private ulong CalculateRuneScore()
        {
            if (_playerData.RuneInventory == null) return 0UL;

            ulong totalScore = 0UL;
            float stackLogFactor = _settings != null ? _settings.RuneStackLogFactor : 0.5f;

            foreach (var pair in _playerData.RuneInventory.GetAll())
            {
                if (pair.Key == null || pair.Value == 0) continue;

                ulong baseGradeScore = GetRuneScore(pair.Key.grade);
                double multiplier = 1.0 + Math.Log10(pair.Value) * stackLogFactor;
                totalScore += (ulong)Math.Max(1.0, Math.Round(baseGradeScore * multiplier));
            }

            return totalScore;
        }

        private ulong CalculateWeaponScore()
        {
            ulong totalScore = 0UL;
            ulong maxWeaponVal = _cachedMaxWeaponValue > 0 ? _cachedMaxWeaponValue : 50000UL;

            if (_playerData.EquipmentInventory != null)
            {
                foreach (var weapon in _playerData.EquipmentInventory.GetAllActive())
                {
                    if (weapon == null) continue;
                    totalScore += EvaluateSingleWeaponScore(weapon, maxWeaponVal);
                }
            }

            if (_forgeManager != null && _forgeManager.CurrentWeapon != null)
            {
                totalScore += EvaluateSingleWeaponScore(_forgeManager.CurrentWeapon, maxWeaponVal);
            }

            return totalScore;
        }

        private ulong EvaluateSingleWeaponScore(Weapon weapon, ulong maxWeaponVal)
        {
            if (weapon == null || weapon.Data == null) return 0UL;

            ulong minWepScore = _settings != null ? _settings.MinWeaponScore : 5UL;
            ulong maxWepScore = _settings != null ? _settings.MaxWeaponScore : 300UL;
            ulong stepBonus = _settings != null ? _settings.WeaponEnhanceStepBonus : 15UL;

            ulong refPrice = weapon.Value > 0 ? weapon.Value : weapon.Data.basePrice;
            double ratio = Math.Clamp((double)refPrice / maxWeaponVal, 0.0, 1.0);

            ulong singleScore = minWepScore + (ulong)Math.Round((maxWepScore - minWepScore) * ratio);

            if (weapon.EnhanceHistory != null)
            {
                singleScore += (ulong)weapon.EnhanceHistory.Count * stepBonus;
            }

            var runes = weapon.Runes;
            if (runes != null)
            {
                for (int i = 0; i < runes.Count; i++)
                {
                    var equippedRune = runes[i];
                    if (equippedRune != null && equippedRune.Data != null)
                    {
                        singleScore += GetRuneScore(equippedRune.Data.grade);
                    }
                }
            }

            return singleScore;
        }

        private ulong CalculateMaterialScore()
        {
            if (_playerData.MaterialInventory == null) return 0UL;

            ulong totalScore = 0UL;
            ulong maxMatVal = _cachedMaxMaterialValue > 0 ? _cachedMaxMaterialValue : 4000UL;
            ulong minMatScore = _settings != null ? _settings.MinMaterialScore : 1UL;
            ulong maxMatScore = _settings != null ? _settings.MaxMaterialScore : 15UL;
            float stackLogFactor = _settings != null ? _settings.MaterialStackLogFactor : 1.0f;

            foreach (var pair in _playerData.MaterialInventory.GetAll())
            {
                if (pair.Key == null || pair.Value == 0) continue;

                double valueRatio = Math.Clamp((double)pair.Key.Value / maxMatVal, 0.0, 1.0);
                double unitScore = minMatScore + (maxMatScore - minMatScore) * valueRatio;
                double stackMultiplier = 1.0 + Math.Log10(pair.Value) * stackLogFactor;

                // 최소 단위 1점 보장
                totalScore += (ulong)Math.Max(1.0, Math.Round(unitScore * stackMultiplier));
            }

            return totalScore;
        }

        private ulong GetRuneScore(RuneGrade grade)
        {
            if (_settings != null)
            {
                return _settings.GetRuneGradeScore(grade);
            }

            switch (grade)
            {
                case RuneGrade.Common: return 5UL;
                case RuneGrade.Uncommon: return 15UL;
                case RuneGrade.Rare: return 50UL;
                case RuneGrade.Epic: return 200UL;
                case RuneGrade.Legendary: return 600UL;
                case RuneGrade.Mythical: return 1500UL;
                default: return 5UL;
            }
        }

        private void EnsureMaxValuesCached()
        {
            if (_cachedMaxMaterialValue == 0 && _materialDatabase != null && _materialDatabase.Items != null)
            {
                var items = _materialDatabase.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].Value > _cachedMaxMaterialValue)
                    {
                        _cachedMaxMaterialValue = items[i].Value;
                    }
                }
            }

            if (_cachedMaxWeaponValue == 0 && _weaponDatabase != null && _weaponDatabase.Items != null)
            {
                var items = _weaponDatabase.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].basePrice > _cachedMaxWeaponValue)
                    {
                        _cachedMaxWeaponValue = items[i].basePrice;
                    }
                }
            }
        }
    }
}
