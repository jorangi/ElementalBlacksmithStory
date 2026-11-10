using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    /// <summary>
    /// 플레이어 진행도 산출을 위한 항목별 배점(최소 단위 1점) 및 구간 기준점 설정 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "SO_ProgressionSettings", menuName = "Scriptable Objects/SO_ProgressionSettings")]
    public class SO_ProgressionSettings : ScriptableObject
    {
        [Header("진행도 구간 기준점 (ulong 점수)")]
        [Tooltip("중반부 진입으로 판정하는 기준 점수")]
        [SerializeField] private ulong _midGameScoreThreshold = 500;
        [Tooltip("극후반부(Endgame) 진입으로 판정하는 기준 점수")]
        [SerializeField] private ulong _endGameScoreThreshold = 3000;

        [Header("1. 재료(Material) 배점 설정 (최소 단위: 1점)")]
        [Tooltip("가장 저렴한 하급 재료의 기본 점수 (최소 단위 1점)")]
        [SerializeField] private ulong _minMaterialScore = 1;
        [Tooltip("DB 최고가 재료의 기본 점수")]
        [SerializeField] private ulong _maxMaterialScore = 15;
        [Tooltip("동일 재료 다량 보유 시 log10 수량 비례 가산 계수")]
        [SerializeField] private float _materialStackLogFactor = 1.0f;

        [Header("2. 무기(Weapon) 배점 설정")]
        [Tooltip("가장 저렴한 기본 무기의 점수")]
        [SerializeField] private ulong _minWeaponScore = 5;
        [Tooltip("DB 최고가 무기의 기본 점수")]
        [SerializeField] private ulong _maxWeaponScore = 300;
        [Tooltip("무기 강화(파생 제작) 1단계당 추가 보너스 점수")]
        [SerializeField] private ulong _weaponEnhanceStepBonus = 15;

        [Header("3. 룬(Rune) 등급별 배점 설정")]
        [SerializeField] private ulong _commonRuneScore = 5;
        [SerializeField] private ulong _uncommonRuneScore = 15;
        [SerializeField] private ulong _rareRuneScore = 50;
        [SerializeField] private ulong _epicRuneScore = 200;
        [SerializeField] private ulong _legendaryRuneScore = 600;
        [SerializeField] private ulong _mythicalRuneScore = 1500;
        [Tooltip("동일 룬 중복 보유 시 log10 수량 비례 가산 계수")]
        [SerializeField] private float _runeStackLogFactor = 0.5f;

        [Header("4. 재력(Money) 배점 설정")]
        [Tooltip("재력 점수(1점)가 부여되기 시작하는 최소 소지 골드")]
        [SerializeField] private ulong _wealthStartGold = 1000;
        [Tooltip("단독으로 극후반 점수를 인정받는 거상 기준 골드 (예: 10억 골드)")]
        [SerializeField] private ulong _wealthEndgameGold = 1000000000;
        [Tooltip("거상 기준 골드 달성 시 부여되는 재력 점수")]
        [SerializeField] private ulong _maxWealthScore = 3000;

        [Header("5. 통계/서버 연동 시 괴물 유저(Outlier) 방어 설정")]
        [Tooltip("유저 점수 분포에서 상·하위 극단치(괴물 유저 등)를 잘라낼 절사 비율 (0.1 = 상위 10%, 하위 10% 제외)")]
        [Range(0.01f, 0.30f)]
        [SerializeField] private float _outlierTrimRatio = 0.10f;

        public ulong MidGameScoreThreshold => _midGameScoreThreshold > 0 ? _midGameScoreThreshold : 500;
        public ulong EndGameScoreThreshold => _endGameScoreThreshold > MidGameScoreThreshold ? _endGameScoreThreshold : MidGameScoreThreshold + 2500;

        public ulong MinMaterialScore => _minMaterialScore > 0 ? _minMaterialScore : 1;
        public ulong MaxMaterialScore => _maxMaterialScore >= MinMaterialScore ? _maxMaterialScore : MinMaterialScore;
        public float MaterialStackLogFactor => _materialStackLogFactor;

        public ulong MinWeaponScore => _minWeaponScore > 0 ? _minWeaponScore : 1;
        public ulong MaxWeaponScore => _maxWeaponScore >= MinWeaponScore ? _maxWeaponScore : MinWeaponScore;
        public ulong WeaponEnhanceStepBonus => _weaponEnhanceStepBonus;

        public float RuneStackLogFactor => _runeStackLogFactor;

        public ulong WealthStartGold => _wealthStartGold > 0 ? _wealthStartGold : 1000;
        public ulong WealthEndgameGold => _wealthEndgameGold > WealthStartGold ? _wealthEndgameGold : 1000000000;
        public ulong MaxWealthScore => _maxWealthScore > 0 ? _maxWealthScore : 3000;
        public float OutlierTrimRatio => _outlierTrimRatio;

        public ulong GetRuneGradeScore(RuneGrade grade)
        {
            switch (grade)
            {
                case RuneGrade.Common: return _commonRuneScore;
                case RuneGrade.Uncommon: return _uncommonRuneScore;
                case RuneGrade.Rare: return _rareRuneScore;
                case RuneGrade.Epic: return _epicRuneScore;
                case RuneGrade.Legendary: return _legendaryRuneScore;
                case RuneGrade.Mythical: return _mythicalRuneScore;
                default: return _commonRuneScore;
            }
        }

        /// <summary>
        /// 외부(서버 등)에서 수집된 정렬된 유저 점수 배열로부터 소수의 괴물 유저(상·하위 Outlier)를 제거하고
        /// 중위수(중반부 기준)와 절사 상위권(극후반부 기준)으로 기준점을 안전하게 갱신합니다.
        /// </summary>
        public void ApplyOutlierSafeUserStatistics(IReadOnlyList<ulong> sortedUserScores)
        {
            if (sortedUserScores == null || sortedUserScores.Count < 4) return;

            int count = sortedUserScores.Count;
            int trimCount = Mathf.Clamp((int)(count * _outlierTrimRatio), 0, (count - 2) / 2);

            int validStart = trimCount;
            int validEnd = count - 1 - trimCount;
            int validLength = validEnd - validStart + 1;
            if (validLength <= 1) return;

            // 중위수(50% 지점)를 중반부 기준으로, 절사 구간 내 상위 85% 지점을 극후반부 기준으로 설정
            int midIndex = validStart + (validLength / 2);
            int endGameIndex = validStart + (int)(validLength * 0.85f);

            ulong calculatedMid = sortedUserScores[midIndex];
            ulong calculatedEnd = sortedUserScores[endGameIndex];

            if (calculatedMid > 0 && calculatedEnd > calculatedMid)
            {
                _midGameScoreThreshold = calculatedMid;
                _endGameScoreThreshold = calculatedEnd;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("현재 배점 기준 중반/극후반 기준점 자동 산출")]
        public void AutoCalculateThresholdsFromSettings()
        {
            // 중반부 기준: 중급 무기 2자루 + Rare 룬 3개 + Epic 룬 1개 + 중급 재료 10종 + 중산층 재력
            ulong estimatedMidWeapon = ((_minWeaponScore + _maxWeaponScore) / 2) * 2 + (_weaponEnhanceStepBonus * 4);
            ulong estimatedMidRune = (_rareRuneScore * 3) + _epicRuneScore;
            ulong estimatedMidMaterial = ((_minMaterialScore + _maxMaterialScore) / 2) * 10;
            _midGameScoreThreshold = Math.Max(50UL, estimatedMidWeapon + estimatedMidRune + estimatedMidMaterial);

            // 극후반부 기준: 최상급 무기 2자루(5강) + Mythical 룬 1개 + Legendary 룬 2개 + 고급 재료 15종
            ulong estimatedEndWeapon = (_maxWeaponScore * 2) + (_weaponEnhanceStepBonus * 10);
            ulong estimatedEndRune = _mythicalRuneScore + (_legendaryRuneScore * 2);
            ulong estimatedEndMaterial = _maxMaterialScore * 15;
            _endGameScoreThreshold = Math.Max(_midGameScoreThreshold + 500UL, estimatedEndWeapon + estimatedEndRune + estimatedEndMaterial);

            // 거상 단독 극후반 달성 점수도 극후반 기준점과 동기화
            _maxWealthScore = _endGameScoreThreshold;

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SO_ProgressionSettings] 기준점 자동 산출 완료 -> 중반부: {_midGameScoreThreshold:N0}점, 극후반부: {_endGameScoreThreshold:N0}점");
        }
#endif
    }
}
