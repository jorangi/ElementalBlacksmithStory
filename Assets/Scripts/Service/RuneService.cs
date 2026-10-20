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

    /// <summary>
    /// 룬 장착, 비용 계산, (향후) 합성 등 룬 관련 도메인 비즈니스 로직을 전담하는 서비스
    /// </summary>
    public class RuneService : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly SO_RuneDatabase _runeDatabase;
        private readonly RuneInventory _runeInventory;
        private readonly IPublisher<ChangeMoneyEvent> _moneyPublisher;

        private ulong _currentMoney;

        public ulong CurrentMoney => _currentMoney;
        public RuneInventory RuneInventory => _runeInventory;

        [Inject]
        public RuneService(
            SO_RuneDatabase runeDatabase,
            RuneInventory runeInventory,
            ISubscriber<ChangeMoneyEvent> moneySubscriber,
            IPublisher<ChangeMoneyEvent> moneyPublisher)
        {
            _runeDatabase = runeDatabase;
            _runeInventory = runeInventory;
            _moneyPublisher = moneyPublisher;

            moneySubscriber.Subscribe(e =>
            {
                _currentMoney = e.MoneyChange;
            }).AddTo(_disposables);
        }

        public SO_RuneData GetRuneData(uint id)
        {
            return _runeDatabase != null ? _runeDatabase.Get(id) : null;
        }

        public bool CanAfford(ulong cost) => _currentMoney >= cost;

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

            if (cost > 0 && !CanAfford(cost))
            {
                result = ApplyRuneResult.NotEnoughMoney;
                return false;
            }

            if (cost > 0)
            {
                _currentMoney -= cost;
                _moneyPublisher.Publish(new ChangeMoneyEvent(out _, _currentMoney));
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

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
