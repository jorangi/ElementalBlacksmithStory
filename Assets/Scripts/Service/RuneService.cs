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
        /// 무기와 장착할 룬 목록을 바탕으로 장착 비용을 산정
        /// </summary>
        public ulong CalculateEquipCost(Weapon weapon, IReadOnlyList<uint> runeIds)
        {
            if (weapon == null || runeIds == null) return 0;

            ulong totalCost = 0;
            for (int i = 0; i < runeIds.Count; i++)
            {
                uint runeId = runeIds[i];
                if (runeId == 0) continue;

                var runeData = GetRuneData(runeId);
                if (runeData != null)
                {
                    ulong runeCost = runeData.Value > 0 ? (ulong)(runeData.Value * 0.2f) : 50UL;
                    if (runeCost < 50UL) runeCost = 50UL;
                    totalCost += runeCost;
                }
            }

            return totalCost;
        }

        /// <summary>
        /// 임시 슬롯 룬들을 무기에 장착하고 인벤토리 및 골드를 갱신
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

            int maxSlots = Mathf.Min(runeIds.Count, weapon.RuneSlot);
            for (int i = 0; i < maxSlots; i++)
            {
                uint runeId = runeIds[i];
                if (runeId != 0)
                {
                    SO_RuneData runeData = GetRuneData(runeId);
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
