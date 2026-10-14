using System;
using Cysharp.Text;
using ElementalBlacksmithStory.Data;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class RuneCardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _runeName;
        [SerializeField] private TextMeshProUGUI _runeDesc;
        [SerializeField] private TextMeshProUGUI _runeEA;
        [SerializeField] private Button _favoriteButton;
        [SerializeField] private Button _cardButton;

        public uint RuneId { get; private set; }
        public RuneGrade Grade { get; private set; }
        public string RuneName { get; private set; } = string.Empty;
        public bool IsFavorite { get; private set; }

        private readonly Subject<Unit> _onLeftClickSubject = new();
        private readonly Subject<Unit> _onRightClickSubject = new();

        /// <summary>
        /// 룬 카드 좌클릭 이벤트
        /// </summary>
        /// <returns></returns>
        public Observable<Unit> OnCardButtonClickAsObservable()
        {
            return _onLeftClickSubject.ThrottleFirst(TimeSpan.FromMilliseconds(100f));
        }

        /// <summary>
        /// 룬 카드 우클릭 이벤트
        /// </summary>
        /// <returns></returns>
        public Observable<Unit> OnCardRightClickAsObservable()
        {
            return _onRightClickSubject.ThrottleFirst(TimeSpan.FromMilliseconds(100f));
        }
        /// <summary>
        /// 즐겨찾기 버튼 클릭 이벤트
        /// </summary>
        /// <returns></returns>
        public Observable<Unit> OnFavoriteButtonClickAsObservable()
        {
            if (_favoriteButton != null)
            {
                return _favoriteButton.OnClickAsObservable().ThrottleFirst(TimeSpan.FromMilliseconds(100f));
            }
            else
            {
                Debug.LogError("[RuneCardView] 즐겨찾기 버튼이 없습니다.");
                return Observable.Empty<Unit>();
            }
        }
        /// <summary>
        /// 카드에 룬 세팅
        /// </summary>
        /// <param name="runeData"></param>
        /// <param name="sprite"></param>
        /// <param name="ea"></param>
        /// <param name="isFavorite"></param>
        public void SetRune(SO_RuneData runeData, Sprite sprite, uint ea, bool isFavorite = false)
        {
            RuneId = runeData.Id;
            Grade = runeData.grade;
            RuneName = runeData.Name;
            SetRuneIcon(sprite);
            SetRuneName(runeData.Name);
            SetRuneDescription(runeData.description);
            SetRuneEA(ea);
            SetFavorite(isFavorite);
        }
        /// <summary>
        /// 룬 아이콘 세팅
        /// </summary>
        /// <param name="sprite"></param>
        public void SetRuneIcon(Sprite sprite)
        {
            _icon.sprite = sprite;
        }
        /// <summary>
        /// 룬 이름 세팅
        /// </summary>
        /// <param name="name"></param>
        public void SetRuneName(string name)
        {
            _runeName.SetText(name);
        }
        /// <summary>
        /// 룬 설명 세팅
        /// </summary>
        /// <param name="description"></param>
        public void SetRuneDescription(string description)
        {
            _runeDesc.SetText(description);
        }
        /// <summary>
        /// 룬 개수 세팅
        /// </summary>
        /// <param name="ea"></param>
        public void SetRuneEA(uint ea)
        {
            _runeEA.SetText(ZString.Format("x{0:N0}", ea));
            gameObject.SetActive(ea > 0);
        }
        /// <summary>
        /// 즐겨찾기 설정
        /// </summary>
        /// <param name="isFavorite"></param>
        public void SetFavorite(bool isFavorite)
        {
            _favoriteButton.image.color = isFavorite ? Color.yellow : Color.gray;
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _onLeftClickSubject.OnNext(Unit.Default);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                _onRightClickSubject.OnNext(Unit.Default);
            }
        }

        private void OnDestroy()
        {
            _onLeftClickSubject.Dispose();
            _onRightClickSubject.Dispose();
        }
    }
}