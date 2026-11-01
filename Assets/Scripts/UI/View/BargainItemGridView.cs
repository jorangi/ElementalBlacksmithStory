using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using LitMotion;
using LitMotion.Extensions;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class BargainItemGridView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private RectTransform _registerUI;
        [SerializeField] private GameObject _itemCardPrefab;
        [SerializeField] private Transform _container;
        [SerializeField] private TextMeshProUGUI _categoryName;
        [SerializeField] private Button _prevCategoryButton;
        [SerializeField] private Button _nextCategoryButton;
        [SerializeField] private Button _cancleButton;
        [SerializeField] private Button _registerButton;
        private readonly List<BargainRegisterItemView> _itemCards = new();
        private IObjectPool<BargainRegisterItemView> _itemCardPool;
        private readonly Subject<BargainRegisterItemView> _onCardClick = new();
        public Observable<BargainRegisterItemView> OnCardClick => _onCardClick;
        private CancellationTokenSource _cts = new();
        private MotionHandle _motionHandle = new();
        private const float ANIM_DURATION = 0.2f;
        [SerializeField] RectTransform Bubble;
        [SerializeField] TextMeshProUGUI bubbleText;
        [SerializeField] TextMeshProUGUI npcName;

        private void Awake()
        {
            _itemCardPool = new ObjectPool<BargainRegisterItemView>(
                createFunc: () =>
                {
                    var card = Instantiate(_itemCardPrefab, _container).GetComponent<BargainRegisterItemView>();

                    card.OnButtonAsObservable()
                        .Subscribe(_ => _onCardClick.OnNext(card))
                        .AddTo(card);
                    return card;
                },
                actionOnGet: card => card.gameObject.SetActive(true),
                actionOnRelease: card => card.gameObject.SetActive(false),
                actionOnDestroy: card => Destroy(card.gameObject),
                defaultCapacity: 20,
                maxSize: 100
            );
        }
        public Observable<Unit> OnRegisterButtonAsObservable()
        {
            if (_registerButton == null)
            {
                Debug.LogWarning("[BargainItemGridView] Register Button이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _registerButton.OnClickAsObservable();
        }
        public Observable<Unit> OnCancleButtonAsObservable()
        {
            if (_cancleButton == null)
            {
                Debug.LogWarning("[BargainItemGridView] Cancel Button이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _cancleButton.OnClickAsObservable();
        }
        public Observable<Unit> OnNextCategoryButtonAsObservable()
        {
            if (_nextCategoryButton == null)
            {
                Debug.LogWarning("[BargainItemGridView] Next Category Button이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _nextCategoryButton.OnClickAsObservable();
        }
        public Observable<Unit> OnPrevCategoryButtonAsObservable()
        {
            if (_prevCategoryButton == null)
            {
                Debug.LogWarning("[BargainItemGridView] Prev Category Button이 없습니다.");
                return Observable.Empty<Unit>();
            }
            return _prevCategoryButton.OnClickAsObservable();
        }
        public void SetCategoryName(string name)
        {
            if (_categoryName == null)
            {
                Debug.LogWarning("[BargainItemGridView] Category Name이 없습니다.");
                return;
            }
            _categoryName.text = name;
        }
        public BargainRegisterItemView CreatItem(uint id, Sprite sprite, uint amount = 1)
        {
            var card = _itemCardPool.Get();
            card.SetData(id, sprite, amount);
            _itemCards.Add(card);
            return card;
        }
        public void UnCheckAll()
        {
            for (int i = 0; i < _itemCards.Count; i++)
            {
                _itemCards[i].Check(false);
            }
        }
        public void Clear()
        {
            for (int i = 0; i < _itemCards.Count; i++)
            {
                _itemCardPool.Release(_itemCards[i]);
            }
            _itemCards.Clear();
        }
        public async UniTask Show(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            _rect.gameObject.SetActive(true);
            _registerUI.gameObject.SetActive(true);
            if (_motionHandle.IsActive())
            {
                _motionHandle.Cancel();
            }
            _motionHandle = LMotion.Create(Vector3.zero, Vector3.one, ANIM_DURATION)
                                .WithEase(Ease.InOutBack)
                                .BindToLocalScale(_rect)
                                .AddTo(gameObject);
            await _motionHandle.ToUniTask(_cts.Token);
        }
        public async UniTask Hide(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            if (_motionHandle.IsActive())
            {
                _motionHandle.Cancel();
            }
            _motionHandle = LMotion.Create(Vector3.one, Vector3.zero, ANIM_DURATION)
                                .WithEase(Ease.InOutBack)
                                .BindToLocalScale(_rect)
                                .AddTo(gameObject);
            await _motionHandle.ToUniTask(_cts.Token);
            _rect.gameObject.SetActive(false);
            _registerUI.gameObject.SetActive(false);
        }
    }
}