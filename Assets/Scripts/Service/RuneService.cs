using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Inventory;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    public enum ApplyRuneResult
    {
        Success,
        NotEnoughMoney,
        InvalidWeapon,
        RuneNotFound,
        NoChanges
    }

    public enum SynthesizeRuneResult
    {
        Success,
        NotEnoughMaterials,
        MythicalNotAllowed,
        NotEnoughMoney
    }

    /// <summary>
    /// 룬 장착, 비용 계산, (향후) 합성 등 룬 관련 도메인 비즈니스 로직을 전담하는 서비스
    /// </summary>
    public class RuneService : IDisposable
    {
        private readonly PlayerData playerData;
        private readonly CompositeDisposable _disposables = new();
        private readonly SO_RuneDatabase _runeDatabase;
        private readonly RuneInventory _runeInventory;
        public RuneInventory RuneInventory => _runeInventory;

        [Inject]
        public RuneService(
            PlayerData playerData,
            SO_RuneDatabase runeDatabase,
            RuneInventory runeInventory
            )
        {
            this.playerData = playerData;
            _runeDatabase = runeDatabase;
            _runeInventory = runeInventory;
        }

        public SO_RuneData GetRuneData(uint id)
        {
            return _runeDatabase != null ? _runeDatabase.Get(id) : null;
        }

        public bool CanAfford(ulong cost) => playerData.CanAfford(cost);

        /// <summary>
        /// 단일 룬에 대한 장착/해제 비용 산정 (룬 가치의 30% + 무기 가치의 5%, 최소 100골드)
        /// </summary>
        public ulong CalculateRuneActionCost(SO_RuneData runeData, Weapon weapon)
        {
            if (runeData == null) return 0;
            ulong runeVal = runeData.Value;
            ulong weaponVal = weapon != null ? weapon.Value : 0;
            ulong cost = (ulong)(runeVal * 0.3f) + (ulong)(weaponVal * 0.05f);
            return cost < 100UL ? 100UL : cost;
        }

        /// <summary>
        /// 무기의 기존 룬 상태와 변경할 룬 목록을 비교하여 총 비용(신규 장착비 + 제거비)을 산정
        /// </summary>
        public ulong CalculateEquipCost(Weapon weapon, IReadOnlyList<uint> runeIds)
        {
            if (weapon == null || runeIds == null) return 0;

            ulong totalCost = 0;
            int maxSlots = Mathf.Min(runeIds.Count, weapon.RuneSlot);
            for (int i = 0; i < maxSlots; i++)
            {
                uint newRuneId = runeIds[i];
                Rune existingRune = weapon.GetRune(i);
                uint existingRuneId = existingRune != null && existingRune.Data != null ? existingRune.Data.Id : 0;

                if (newRuneId == existingRuneId) continue;

                // 룬 제거 비용
                if (existingRuneId != 0 && existingRune.Data != null)
                {
                    totalCost += CalculateRuneActionCost(existingRune.Data, weapon);
                }

                // 룬 장착 비용
                if (newRuneId != 0)
                {
                    var newData = GetRuneData(newRuneId);
                    if (newData != null)
                    {
                        totalCost += CalculateRuneActionCost(newData, weapon);
                    }
                }
            }

            return totalCost;
        }
        /// <summary>
        /// 룬 3개의 합성 비용 산정 (합성 불가 조건 시 0 반환)
        /// </summary>
        public ulong CalculateSynthesizeCost(IReadOnlyList<uint> runeIds)
        {
            if (runeIds == null || runeIds.Count != 3) return 0;

            ulong totalCost = 0;
            for (int i = 0; i < 3; i++)
            {
                if (runeIds[i] == 0) return 0;

                var runeData = _runeDatabase.Get(runeIds[i]);
                if (runeData == null || runeData.grade == RuneGrade.Mythical)
                {
                    // Mythical 등급 룬은 합성 불가
                    return 0;
                }
                totalCost += (ulong)runeData.grade;
            }
            return totalCost;
        }
        /// <summary>
        /// 임시 슬롯 룬 변경사항을 무기에 반영하고 인벤토리 및 골드를 갱신
        /// </summary>
        public bool TryApplyRunes(Weapon weapon, IReadOnlyList<uint> runeIds, out ApplyRuneResult result, out ulong spentCost)
        {
            spentCost = 0;
            if (weapon == null)
            {
                result = ApplyRuneResult.InvalidWeapon;
                return false;
            }

            if (runeIds == null || runeIds.Count == 0)
            {
                result = ApplyRuneResult.NoChanges;
                return false;
            }

            // 변경 사항이 있는지 확인
            bool hasChanges = false;
            int maxSlots = Mathf.Min(runeIds.Count, weapon.RuneSlot);
            for (int i = 0; i < maxSlots; i++)
            {
                uint newRuneId = runeIds[i];
                Rune existingRune = weapon.GetRune(i);
                uint existingRuneId = existingRune != null && existingRune.Data != null ? existingRune.Data.Id : 0;
                if (newRuneId != existingRuneId)
                {
                    hasChanges = true;
                    break;
                }
            }

            if (!hasChanges)
            {
                result = ApplyRuneResult.NoChanges;
                return false;
            }

            ulong cost = CalculateEquipCost(weapon, runeIds);
            spentCost = cost;

            if (cost > 0 && !playerData.TrySpend(cost))
            {
                result = ApplyRuneResult.NotEnoughMoney;
                return false;
            }

            for (int i = 0; i < maxSlots; i++)
            {
                uint newRuneId = runeIds[i];
                Rune existingRune = weapon.GetRune(i);
                uint existingRuneId = existingRune != null && existingRune.Data != null ? existingRune.Data.Id : 0;

                if (newRuneId == existingRuneId) continue;

                if (newRuneId == 0)
                {
                    weapon.UnequipRune(i, out Rune unequippedRune);
                    if (unequippedRune != null && unequippedRune.Data != null)
                    {
                        _runeInventory?.Add(unequippedRune.Data, 1);
                    }
                }
                else
                {
                    SO_RuneData runeData = GetRuneData(newRuneId);
                    if (runeData != null)
                    {
                        Rune rune = new(runeData);
                        weapon.EquipRune(rune, i, out Rune unequippedRune);

                        _runeInventory?.Get(runeData, 1);

                        if (unequippedRune != null && unequippedRune.Data != null)
                        {
                            _runeInventory?.Add(unequippedRune.Data, 1);
                        }
                    }
                }
            }

            result = ApplyRuneResult.Success;
            return true;
        }
        public SynthesizeRuneResult TrySynthesizeRunes(IReadOnlyList<uint> runeIds, out SO_RuneData result, out ulong spentCost)
        {
            spentCost = 0;
            result = null;

            if (runeIds == null || runeIds.Count != 3)
            {
                return SynthesizeRuneResult.NotEnoughMaterials;
            }

            for (int i = 0; i < 3; i++)
            {
                if (runeIds[i] == 0)
                {
                    return SynthesizeRuneResult.NotEnoughMaterials;
                }

                var runeData = _runeDatabase.Get(runeIds[i]);
                if (runeData == null)
                {
                    return SynthesizeRuneResult.NotEnoughMaterials;
                }

                if (runeData.grade == RuneGrade.Mythical)
                {
                    Debug.LogError("Mythical 등급의 룬은 합성 재료로 사용할 수 없습니다.");
                    return SynthesizeRuneResult.MythicalNotAllowed;
                }
            }

            ulong cost = CalculateSynthesizeCost(runeIds);
            spentCost = cost;

            if (cost > 0 && !playerData.TrySpend(cost))
            {
                Debug.LogWarning($"{cost - playerData.Money.CurrentValue}골드가 부족합니다.");
                return SynthesizeRuneResult.NotEnoughMoney;
            }

            List<SO_RuneData> runeTable = new();
            ulong costAverage = cost / 3;
            (RuneGrade, RuneGrade) gradeRange;
            if (costAverage < (ulong)RuneGrade.Uncommon) gradeRange = (RuneGrade.Common, RuneGrade.Uncommon);
            else if (costAverage < (ulong)RuneGrade.Rare) gradeRange = (RuneGrade.Uncommon, RuneGrade.Rare);
            else if (costAverage < (ulong)RuneGrade.Epic) gradeRange = (RuneGrade.Rare, RuneGrade.Epic);
            else if (costAverage < (ulong)RuneGrade.Legendary) gradeRange = (RuneGrade.Epic, RuneGrade.Legendary);
            else gradeRange = (RuneGrade.Legendary, RuneGrade.Mythical);

            // 기본 상위 보장 30% + 금액 기반 70% (Legendary 3개는 Mythical 고정 30% 확률 적용)
            float upperPercent = (gradeRange.Item2 == RuneGrade.Mythical)
                ? 0.3f
                : Mathf.Clamp01(0.3f + 0.7f * (costAverage - (ulong)gradeRange.Item1) / ((ulong)gradeRange.Item2 - (ulong)gradeRange.Item1));

            if (UnityEngine.Random.Range(0f, 1f) < upperPercent)
            {
                // 상위 등급
                runeTable.AddRange(_runeDatabase.GetRunesByGrade(gradeRange.Item2));
            }
            else
            {
                // 하위 등급
                runeTable.AddRange(_runeDatabase.GetRunesByGrade(gradeRange.Item1));
            }

            SO_RuneData randomRune = runeTable[UnityEngine.Random.Range(0, runeTable.Count)];

            // 합성 재료 룬 3개 소모
            for (int i = 0; i < 3; i++)
            {
                var mat = _runeDatabase.Get(runeIds[i]);
                if (mat != null)
                {
                    _runeInventory?.Get(mat, 1);
                }
            }

            // 합성 결과 룬 인벤토리에 추가
            _runeInventory?.Add(randomRune, 1);

            result = randomRune;
            return SynthesizeRuneResult.Success;
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
