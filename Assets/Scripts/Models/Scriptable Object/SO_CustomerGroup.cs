using System.Collections.Generic;
using ElementalBlacksmithStory.Core;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [System.Serializable]
    public struct ItemPreferenceData
    {
        [Header("허용 아이템 카테고리 (비어있으면 전체 허용)")]
        [Tooltip("예: 보급관은 Material, Rune만 등록. 비어있으면 무기 포함 모든 카테고리 입찰")]
        public List<ShopItemCategory> allowedCategories;

        [Tooltip("선호 카테고리 (설정된 카테고리는 입찰 욕구 및 가치평가 보너스)")]
        public List<ShopItemCategory> preferredCategories;

        [Header("선호 원소 속성 (선호 시 Desire/상한가 보너스)")]
        [Tooltip("예: 루미너스 순례자는 Holy, 마법학자는 속성이 있는 걸 추구하는 등")]
        public List<ElementType> preferredElements;

        [Header("기피 원소 속성 (기피 시 입찰 제외 또는 패널티)")]
        [Tooltip("예: 순례자가 Dark를 기피하거나 배제할 때")]
        public List<ElementType> dislikedElements;

        /// <summary>
        /// 해당 아이템이 거래/입찰 자격을 갖추었는지 판별합니다. (null/Empty면 전체 허용)
        /// </summary>
        public readonly bool IsEligible(ShopItemCategory category, List<ElementType> elementTypes = null)
        {
            if (allowedCategories != null && allowedCategories.Count > 0)
            {
                bool isAllowed = false;
                for (int i = 0; i < allowedCategories.Count; i++)
                {
                    if (allowedCategories[i] == category)
                    {
                        isAllowed = true;
                        break;
                    }
                }

                if (!isAllowed)
                {
                    return false;
                }
            }

            if (dislikedElements != null && dislikedElements.Count > 0 && elementTypes != null && elementTypes.Count > 0)
            {
                for (int i = 0; i < dislikedElements.Count; i++)
                {
                    for (int j = 0; j < elementTypes.Count; j++)
                    {
                        if (dislikedElements[i] == elementTypes[j])
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 해당 아이템이 선호 대상(카테고리 또는 속성)인지 판별합니다.
        /// </summary>
        public readonly bool IsPreferred(ShopItemCategory category, List<ElementType> elementTypes = null)
        {
            if (preferredCategories != null && preferredCategories.Count > 0)
            {
                for (int i = 0; i < preferredCategories.Count; i++)
                {
                    if (preferredCategories[i] == category)
                    {
                        return true;
                    }
                }
            }

            if (preferredElements != null && preferredElements.Count > 0 && elementTypes != null && elementTypes.Count > 0)
            {
                for (int i = 0; i < preferredElements.Count; i++)
                {
                    for (int j = 0; j < elementTypes.Count; j++)
                    {
                        if (preferredElements[i] == elementTypes[j])
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    [CreateAssetMenu(fileName = "SO_CustomerGroup", menuName = "Scriptable Objects/SO_CustomerGroup")]
    public class SO_CustomerGroup : ScriptableObject, IIdentifiable
    {
        [Header("그룹 정보")]
        public string _customerGroupTitle;
        public string _customerGroupName;
        public string _description;

        [Header("가치 평가 배율 (물품 원가 대비 추측가 범위)")]
        [Tooltip("물품 원가에 곱해질 최소/최대 배율. 예: 0.8 ~ 1.5")]
        [Range(0.2f, 5.0f)]
        public float minEstimateRatio = 0.7f;
        [Range(0.2f, 5.0f)]
        public float maxEstimateRatio = 1.3f;

        [Header("첫 제시가 비율 (추측가 대비)")]
        [Tooltip("손님이 처음 던지는 제안가의 비율. 예: 0.5면 추측가의 50%부터 후려침")]
        [Range(0.3f, 1.0f)]
        public float minFirstOfferRatio = 0.5f;
        [Range(0.3f, 1.0f)]
        public float maxFirstOfferRatio = 0.8f;

        [Header("턴당 양보 보간율 (Interpolation)")]
        [Tooltip("매 턴 플레이어 제시가 쪽으로 얼마나 다가갈지 결정하는 비율")]
        [Range(0.05f, 1.0f)]
        public float minInterpolation = 0.15f;
        [Range(0.05f, 1.0f)]
        public float maxInterpolation = 0.35f;

        [Header("인내심 (Patience)")]
        [Tooltip("손님의 최대 인내심 수치")]
        [Range(10, 200)]
        public int minPatience = 60;
        [Range(10, 200)]
        public int maxPatience = 100;

        [Header("과욕 민감도 (Greed Sensitivity)")]
        [Tooltip("손님 추측가보다 비싸게 불렀을 때 인내심이 깎이는 가중치 (1.0 = 표준, 높을수록 예민함)")]
        [Range(0.5f, 3.0f)]
        public float greedSensitivity = 1.0f;

        [Header("아이템 선호 관련")]
        public ItemPreferenceData itemPreferenceData;

        [Header("손님 정보")]
        public List<string> _customerNames;
        public List<Sprite> _customerSprites;

        [Header("첫 가격 제시 대사")]
        public List<DialogueLine> _firstOffer;

        [Header("흥정 제시 대사")]
        public List<DialogueLine> _negotiation;

        [Header("마지막 흥정 대사")]
        public List<DialogueLine> _finalOffer;

        [Header("승낙 대사")]
        public List<DialogueLine> _acception;

        [Header("거절 대사")]
        public List<DialogueLine> _rejection;

        [Header("경매 정보 (입찰 대기 타이머, 초 단위)")]
        [Tooltip("경매 입찰 대기 최소 시간 (초)")]
        [Range(5, 300)]
        public int _minTimer = 45;

        [Tooltip("경매 입찰 대기 최대 시간 (초)")]
        [Range(10, 300)]
        public int _maxTimer = 120;
        public uint Id => uint.Parse(this.name);

        /// <summary>
        /// 해당 아이템이 손님의 거래/입찰 대상인지 판별합니다. (Zero-Allocation)
        /// </summary>
        public bool IsItemEligible(ShopItemCategory category, List<ElementType> elementTypes = null)
            => itemPreferenceData.IsEligible(category, elementTypes);

        /// <summary>
        /// 해당 아이템이 손님의 선호 대상(카테고리 또는 속성)인지 판별합니다. (Zero-Allocation)
        /// </summary>
        public bool IsItemPreferred(ShopItemCategory category, List<ElementType> elementTypes = null)
            => itemPreferenceData.IsPreferred(category, elementTypes);
    }
}