using UnityEngine;
using UnityEngine.UI;
using System;
using R3;

namespace ElementalBlacksmithStory.UI
{
    public class ShopCartView: MonoBehaviour
    {
        [SerializeField] private GameObject _container;
        [SerializeField] private GameObject _cartItemPrefab;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _cancelButton;
        public ShopCartItemView Create(uint itemId, uint ea, Sprite sprite)
        {
            GameObject item = Instantiate(_cartItemPrefab, _container.transform);
            item.TryGetComponent<ShopCartItemView>(out var view);
            view.SetIcon(sprite);
            view.SetEA(ea);
            return view;
        }
        public Observable<Unit> OnClickAcceptAsObservable()
        {
            if(_acceptButton == null)
            {
                return Observable.Empty<Unit>();
            }
            return _acceptButton.OnClickAsObservable();
        }
        public Observable<Unit> OnClickCancelAsObservable()
        {
            if(_cancelButton == null)
            {
                return Observable.Empty<Unit>();
            }
            return _cancelButton.OnClickAsObservable();
        }
    }
}