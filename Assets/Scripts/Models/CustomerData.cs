using System;
using System.Threading;
using ElementalBlacksmithStory.Data;
using R3;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    public class CustomerData : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly CancellationTokenSource _cts = new();
        private bool _isDisposed = false;
        public CancellationToken CancellationToken => _cts.Token;

        public readonly SO_CustomerGroup Group;
        private readonly IShopItem _item;
        private readonly ulong _estimatePrice;
        private readonly ulong _firstOfferPrice;
        private ulong _currentPrice;
        private readonly float _interpolation;
        private int _patience;
        private readonly int _lifetime; // 거래 신청리스트에 얼마동안 잔류할지
        private readonly float _patienceNoise;
        private readonly float _interpolationNoise;
        private string _name;
        private readonly Sprite _sprite;
        private int _bargainCount = 0;

        public ulong FirstOfferPrice { get => _firstOfferPrice; }
        public ulong CurrentPrice { get => _currentPrice; }
        public string Name { get => _name; }
        public Sprite Sprite { get => _sprite; }
        public ulong EstimatePrice { get => _estimatePrice; }
        public float Interpolation { get => _interpolation; }
        public int Patience { get => _patience; }
        public float PatienceNoise { get => _patienceNoise; }
        public float InterpolationNoise { get => _interpolationNoise; }
        public IShopItem Item { get => _item; }
        public int Lifetime { get => _lifetime; }
        public int BargainCount { get => _bargainCount; }
        public CustomerData(SO_CustomerGroup group, IShopItem item)
        {
            Group = group;
            _item = item;
            ulong totalBaseValue = item.Value * (item.Count > 0 ? (ulong)item.Count : 1UL);
            _estimatePrice = (ulong)(totalBaseValue * UnityEngine.Random.Range(group.minEstimateRatio, group.maxEstimateRatio));
            _firstOfferPrice = (ulong)(_estimatePrice * UnityEngine.Random.Range(group.minFirstOfferRatio, group.maxFirstOfferRatio));
            _currentPrice = _firstOfferPrice;
            _interpolation = UnityEngine.Random.Range(group.minInterpolation, group.maxInterpolation);
            _patience = UnityEngine.Random.Range(group.minPatience, group.maxPatience);
            _patienceNoise = UnityEngine.Random.Range(0.85f, 1.15f);
            _interpolationNoise = UnityEngine.Random.Range(0.85f, 1.15f);
            _lifetime = 10 * UnityEngine.Random.Range(group._minTimer, group._maxTimer); // 랜덤 타이머 * 10 = 최소 50초~50분
            _name = (group._customerNames != null && group._customerNames.Count > 0)
                ? group._customerNames[UnityEngine.Random.Range(0, group._customerNames.Count)]
                : group._customerGroupName;
            _sprite = (group._customerSprites != null && group._customerSprites.Count > 0)
                ? group._customerSprites[UnityEngine.Random.Range(0, group._customerSprites.Count)]
                : null;
        }
        public void SetCustomerName(string name)
        {
            _name = name;
        }
        public ulong Bargain(ulong price)
        {
            // 1. 플레이어가 현재 제시가보다 낮거나 같게 부른 경우 (즉시 타결)
            if (price <= _currentPrice)
            {
                _currentPrice = price;
                return _currentPrice;
            }

            // 2. 인내심 감소량 계산 (기본 소모량 + 손님 추측가 초과 시 과욕 페널티)
            const int basePatienceDrain = 10;
            int patienceDrain = basePatienceDrain;

            if (_estimatePrice > 0 && price > _estimatePrice)
            {
                float greedRatio = (float)(price - _estimatePrice) / _estimatePrice;
                int greedPenalty = Mathf.RoundToInt(greedRatio * 20f * Group.greedSensitivity);
                patienceDrain += Mathf.Max(0, greedPenalty);
            }

            // 손님의 기분/상황에 따른 인내심 감소량
            patienceDrain = Mathf.Max(1, Mathf.RoundToInt(patienceDrain * _patienceNoise));

            // 인내심 0: 최종 제안용
            _patience = _patience == 0 ? -1 : Mathf.Max(0, _patience - patienceDrain);
            
            // 3. 인내심이 바닥난 경우 (흥정 결렬 -> 0 반환)
            if (_patience < 0)
            {
                return 0UL;
            }

            // 4. 새로운 제시가 계산
            float effectiveInterpolation = Mathf.Clamp(_interpolation * _interpolationNoise, 0.01f, 1.0f);

            ulong priceDiff = price - _currentPrice;
            ulong increment = (ulong)Mathf.Round(priceDiff * effectiveInterpolation);
            if (increment == 0)
            {
                increment = 1;
            }

            _currentPrice += increment;
            _bargainCount += 1;
            // 플레이어 제시가에 도달하거나 넘어서면 타결
            if (_currentPrice >= price)
            {
                _currentPrice = price;
                return _currentPrice;
            }

            // 손님의 추측가를 초과하지 않도록 캡핑
            if (_estimatePrice > 0 && _currentPrice > _estimatePrice)
            {
                _currentPrice = _estimatePrice;
            }

            return _currentPrice;
        }
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}