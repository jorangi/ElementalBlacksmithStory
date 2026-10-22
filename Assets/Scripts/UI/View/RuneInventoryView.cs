using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;
using LitMotion;
using LitMotion.Extensions;
using R3;
using TMPro;
using UnityEngine;

namespace ElementalBlacksmithStory.UI
{
    public class RuneInventoryView : MonoBehaviour, IDisposable
    {
        [SerializeField] private GameObject _runeInventory;
        [SerializeField] private Transform _runeContainer;
        [SerializeField] private TMP_InputField _searchField;
        [SerializeField] private RuneCardView _runeCardPrefab;
        private const float SHOW_DURATION = 0.2f;

        private readonly Dictionary<uint, RuneCardView> _runeCardDic = new(32);
        private readonly List<RuneCardView> _runeCards = new(32);
        private readonly CompositeDisposable _disposables = new();

        private readonly Subject<uint> _onFavoriteClickedSubject = new();
        private readonly Subject<uint> _onCardClickedSubject = new();
        private readonly Subject<uint> _onCardRightClickedSubject = new();

        public Observable<uint> OnFavoriteClicked => _onFavoriteClickedSubject;
        public Observable<uint> OnCardClicked => _onCardClickedSubject;
        public Observable<uint> OnCardRightClicked => _onCardRightClickedSubject;
        public Observable<string> OnSearchInputChanged =>

            _searchField != null ? _searchField.onValueChanged.AsObservable() : Observable.Empty<string>();

        private static readonly Comparison<RuneCardView> RuneSortComparer = CompareRuneCards;

        private static int CompareRuneCards(RuneCardView a, RuneCardView b)
        {
            if (a.IsFavorite != b.IsFavorite)
            {
                return a.IsFavorite ? -1 : 1;
            }
            if (a.Grade != b.Grade)
            {
                return b.Grade.CompareTo(a.Grade);
            }
            return a.RuneId.CompareTo(b.RuneId);
        }
        /// <summary>
        /// 룬 카드를 생성
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sprite"></param>
        /// <param name="ea"></param>
        /// <param name="isFavorite"></param>
        public void Create(SO_RuneData data, Sprite sprite, uint ea, bool isFavorite = false)
        {
            if (_runeCardDic.TryGetValue(data.Id, out var existing))
            {
                existing.SetRuneEA(ea);
                existing.SetFavorite(isFavorite);
                return;
            }

            var newCard = Instantiate(_runeCardPrefab, _runeContainer);
            newCard.SetRune(data, sprite, ea, isFavorite);

            newCard.OnFavoriteButtonClickAsObservable()
                .Subscribe(_ => _onFavoriteClickedSubject.OnNext(data.Id))
                .AddTo(_disposables);

            newCard.OnCardButtonClickAsObservable()
                .Subscribe(_ => _onCardClickedSubject.OnNext(data.Id))
                .AddTo(_disposables);

            newCard.OnCardRightClickAsObservable()
                .Subscribe(_ => _onCardRightClickedSubject.OnNext(data.Id))
                .AddTo(_disposables);

            _runeCardDic.Add(data.Id, newCard);
            _runeCards.Add(newCard);
        }
        /// <summary>
        /// 룬 카드가 존재하는지 확인
        /// </summary>
        public bool HasCard(uint runeId) => _runeCardDic.ContainsKey(runeId);

        /// <summary>
        /// 룬 카드의 수량을 설정
        /// </summary>
        /// <param name="runeId"></param>
        /// <param name="ea"></param>
        public void SetRuneAmount(uint runeId, uint ea)
        {
            if (_runeCardDic.TryGetValue(runeId, out var instance))
            {
                instance.SetRuneEA(ea);
            }
            else
            {
                Debug.LogWarning($"[RuneInventoryView] {runeId}의 룬이 인벤토리에 없습니다.");
            }
        }

        /// <summary>
        /// 룬 카드의 즐겨찾기 상태를 설정
        /// </summary>
        /// <param name="runeId"></param>
        /// <param name="isFavorite"></param>
        public void SetFavorite(uint runeId, bool isFavorite)
        {
            if (_runeCardDic.TryGetValue(runeId, out var instance))
            {
                instance.SetFavorite(isFavorite);
            }
        }

        /// <summary>
        /// 룬 카드 보이기/숨기기
        /// </summary>
        /// <param name="runeId"></param>
        /// <param name="isVisible"></param>
        public void SetCardVisible(uint runeId, bool isVisible)
        {
            if (_runeCardDic.TryGetValue(runeId, out var instance))
            {
                instance.gameObject.SetActive(isVisible);
            }
        }

        /// <summary>
        /// 룬 카드의 정렬
        /// </summary>
        public void Sort()
        {
            _runeCards.Sort(RuneSortComparer);

            for (int i = 0; i < _runeCards.Count; i++)
            {
                _runeCards[i].transform.SetSiblingIndex(i);
            }
        }
        /// <summary>
        /// 키워드로 룬 카드 필터링
        /// </summary>
        /// <param name="keyword"></param>
        public void FilterByKeyword(string keyword)
        {
            bool isKeywordEmpty = string.IsNullOrWhiteSpace(keyword);
            for (int i = 0; i < _runeCards.Count; i++)
            {
                var card = _runeCards[i];
                if (isKeywordEmpty)
                {
                    card.gameObject.SetActive(true);
                }
                else
                {
                    bool isMatch = card.RuneName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
                    card.gameObject.SetActive(isMatch);
                }
            }
        }
        public void Clear()
        {
            for (int i = 0; i < _runeCards.Count; i++)
            {
                if (_runeCards[i] != null)
                {
                    Destroy(_runeCards[i].gameObject);
                }
            }
            _runeCards.Clear();
            _runeCardDic.Clear();
        }
        // public async UniTask ShowAsync(CancellationToken ct = default)
        // {
        // }
        // public async UniTask Hide(CancellationToken ct = default)
        // {
        // }
        private void OnDestroy()
        {
            Dispose();
        }
        public void Dispose()
        {
            _disposables.Dispose();
            _onFavoriteClickedSubject.Dispose();
            _onCardClickedSubject.Dispose();
            Clear();
            GC.SuppressFinalize(this);
        }
    }
}