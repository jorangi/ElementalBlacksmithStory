using System;
using Cysharp.Text;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class BargainRegisterItemView : MonoBehaviour
    {
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _amountCount;
        [SerializeField] private GameObject _checkObject;
        [SerializeField] private Button _button;

        public uint ItemId { get; private set; }

        public Observable<Unit> OnButtonAsObservable()
        {
            if (_button == null)
            {
                Debug.LogWarning("[BargainRegisterItemView] 버튼이 등록되지 않았음.");
                return Observable.Empty<Unit>();
            }
            return _button.OnClickAsObservable();
        }
        public void SetIcon(Sprite sprite)
        {
            _itemIcon.sprite = sprite;
        }
        public void SetAmount(uint amount)
        {
            _amountCount.SetText(ZString.Format("x{0}", amount));
        }
        public void Check(bool isCheck)
        {
            _checkObject.SetActive(isCheck);
        }

        public void SetData(uint id, Sprite sprite, uint amount = 1)
        {
            ItemId = id;
            SetIcon(sprite);
            SetAmount(amount);
            Check(false);
        }
    }
}
