using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;
using LitMotion;
using LitMotion.Extensions;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class CustomerListView : MonoBehaviour
    {
        private const float ANIM_DURATION = 0.2f;
        /// <summary>
        /// 말풍선 패널
        /// </summary>
        [SerializeField] RectTransform Bubble;
        /// <summary>
        /// 말풍선 텍스트
        /// </summary>
        [SerializeField] TextMeshProUGUI bubbleText;
        [SerializeField] TextMeshProUGUI npcName;
        [SerializeField] private Image _itemImage;
        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private TextMeshProUGUI _itemAmount;
        private CancellationTokenSource _cts;
        private MotionHandle motionHandle;
        [SerializeField] private Color rootPanelColor;
        [SerializeField] private RectTransform _auctionUI;
        [SerializeField] private RectTransform _customerListUI;
        [SerializeField] private RectTransform _customerListRect;
        [SerializeField] private GameObject _noListAlert;
        [SerializeField] private GameObject _listItemPrefab;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _withdrawButton;
        private readonly CompositeDisposable _disposables = new();
        private IObjectPool<CustomerListItemView> _itemPools;
        private readonly Dictionary<CustomerData, CustomerListItemView> _customerListItems = new();
        private readonly Subject<CustomerData> _onItemClickedSubject = new();
        public Observable<CustomerData> OnItemClicked => _onItemClickedSubject.AsObservable();

        private void Awake()
        {
            _itemPools = new ObjectPool<CustomerListItemView>(
                createFunc: () =>
                {
                    var instance = Instantiate(_listItemPrefab, _customerListRect);
                    instance.SetActive(false);
                    var itemView = instance.GetComponent<CustomerListItemView>();
                    itemView.OnButtonAsObservable()
                        .Subscribe((self: this, itemView), static (_, state) =>
                        {
                            if (state.itemView.Data != null)
                            {
                                state.self._onItemClickedSubject.OnNext(state.itemView.Data);
                            }
                        });
                    return itemView;
                },
                actionOnGet: obj =>
                {
                    obj.transform.SetParent(_customerListRect, false);
                    obj.gameObject.SetActive(true);
                },
                actionOnRelease: obj =>
                {
                    obj.gameObject.SetActive(false);
                },
                actionOnDestroy: obj =>
                {
                    if (obj != null) Destroy(obj);
                },
                collectionCheck: false,
                defaultCapacity: 100,
                maxSize: 150
            );

            var prewarmBuffer = new CustomerListItemView[100];
            for (int i = 0; i < 100; i++)
            {
                prewarmBuffer[i] = _itemPools.Get();
            }
            for (int i = 0; i < 100; i++)
            {
                _itemPools.Release(prewarmBuffer[i]);
            }
        }
        public Observable<Unit> OnCloseButtonAsObservable()
        {
            if (_closeButton == null)
            {
                Debug.LogError("[CustomerListView] _closeButton이 null입니다.");
                return Observable.Empty<Unit>();
            }
            return _closeButton.OnClickAsObservable();
        }
        public Observable<Unit> OnWithdrawButtonAsObservable()
        {
            if (_withdrawButton == null)
            {
                Debug.LogError("[CustomerListView] _withdrawButton이 null입니다.");
                return Observable.Empty<Unit>();
            }
            return _withdrawButton.OnClickAsObservable();
        }
        public void SetItem(string itemName, Sprite itemImage, uint amount = 1)
        {
            _itemName.SetText(itemName);
            _itemImage.sprite = itemImage;
            _itemAmount.SetText(amount > 1 ? ZString.Format("x{0}", amount) : string.Empty);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_itemAmount.rectTransform);
        }


        public async UniTask Show(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (motionHandle.IsActive())
            {
                motionHandle.Cancel();
            }
            _customerListUI.gameObject.SetActive(true);
            _auctionUI.gameObject.SetActive(true);
            motionHandle = LMotion.Create(Vector3.zero, Vector3.one, ANIM_DURATION)
                                .WithEase(Ease.InOutBack)
                                .BindToLocalScale(_auctionUI)
                                .AddTo(gameObject);

            await motionHandle.ToUniTask(_cts.Token)
                                .SuppressCancellationThrow();
        }
        public void HideImmediately()
        {
            _customerListUI.gameObject.SetActive(false);
        }
        public async UniTask Hide(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (motionHandle.IsActive())
            {
                motionHandle.Cancel();
            }
            motionHandle = LMotion.Create(Vector3.one, Vector3.zero, ANIM_DURATION)
                                .WithEase(Ease.InOutBack)
                                .BindToLocalScale(_auctionUI)
                                .AddTo(gameObject);

            await motionHandle.ToUniTask(_cts.Token)
                                .SuppressCancellationThrow();

            _customerListUI.gameObject.SetActive(false);
            _auctionUI.gameObject.SetActive(false);
        }
        public void ShowNoListAlert(bool show)
        {
            _noListAlert.SetActive(show);
        }
        public void Create(CustomerData data)
        {
            if (data == null || _customerListItems.ContainsKey(data)) return;
            CustomerListItemView item = _itemPools.Get();
            item.SetData(data);
            _customerListItems.Add(data, item);
            ShowNoListAlert(false);
        }
        public void Remove(CustomerData data)
        {
            if (data == null || !_customerListItems.TryGetValue(data, out var entry))
            {
                return;
            }
            _customerListItems.Remove(data);
            _itemPools.Release(entry);
            if (_customerListItems.Count == 0)
            {
                ShowNoListAlert(true);
            }
        }
        public void Clear()
        {
            foreach (var pair in _customerListItems)
            {
                if (pair.Value.gameObject != null)
                {
                    _itemPools.Release(pair.Value);
                }
            }
            _customerListItems.Clear();
            ShowNoListAlert(true);
        }
        private void OnDestroy()
        {
            _itemPools?.Clear();
        }
        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}