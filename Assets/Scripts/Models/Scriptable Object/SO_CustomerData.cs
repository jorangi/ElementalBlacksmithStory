using System.Collections.Generic;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{

    [CreateAssetMenu(fileName = "SO_CustomerGroup", menuName = "Scriptable Objects/SO_CustomerGroup")]
    public class SO_CustomerGroup : ScriptableObject
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
    }
}