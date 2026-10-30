using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Models;
using LitMotion;
using LitMotion.Extensions;
using R3;
using TMPro;
using UnityEngine;
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
        [SerializeField] private GameObject _listItemPrefab;
        [SerializeField] private Button _closeButton;
        private readonly CompositeDisposable _disposables = new();
        private readonly Dictionary<CustomerData, GameObject> _customerListItems = new();

        public Observable<Unit> OnCloseButtonAsObservable()
        {
            if (_closeButton == null)
            {
                Debug.LogError("[CustomerListView] _closeButton이 null입니다.");
                return Observable.Empty<Unit>();
            }
            return _closeButton.OnClickAsObservable();
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
        public void Create(CustomerData data)
        {
            string name = data.Name;
            ulong price = data.FirstOfferPrice;
            GameObject instance = GameObject.Instantiate(_listItemPrefab, _auctionUI);
            instance.GetComponentInChildren<TextMeshProUGUI>().SetText(ZString.Format(price == 0 ? "{0}  |  ???" : "{0}  |  {1:N0}", name, price));
            _customerListItems.Add(data, instance);
        }
        public void Remove(CustomerData data)
        {
            if (!_customerListItems.ContainsKey(data))
            {
                Debug.LogError($"[CustomerListView] <{data}> 데이터에 오브젝트가 없습니다.");
                return;
            }
            GameObject obj = _customerListItems[data];
            _customerListItems.Remove(data);
            Destroy(obj);
        }
        public void Clear()
        {
            foreach (var customer in _customerListItems)
            {
                Destroy(customer.Value);
            }
            _customerListItems.Clear();
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