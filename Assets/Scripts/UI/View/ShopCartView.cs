using UnityEngine;
using System;
namespace ElementalBlacksmithStory.UI
{
    public class ShopCartView: MonoBehaviour
    {
        [SerializeField] private GameObject _container;
        [SerializeField] private GameObject _cartItemPrefab;
        public ShopCartItemView Create(uint itemId, uint ea, Sprite sprite)
        {
            GameObject item = Instantiate(_cartItemPrefab, _container.transform);
            item.TryGetComponent<ShopCartItemView>(out var view);
            view.SetIcon(sprite);
            view.SetEA(ea);
            return view;
        }
    }
}