using Cysharp.Text;
using ElementalBlacksmithStory.Data;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class CustomerListItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _textField;
        public CustomerData Data { get; private set; }

        public void SetData(CustomerData data)
        {
            Data = data;
            string name = $"{data.Group._customerGroupTitle} {data.Name}";
            ulong price = data.FirstOfferPrice;
            _textField.SetText(ZString.Format(price == 0 ? "{0} | ???" : "{0} | {1:N0}G", name, price));
        }
        public Observable<Unit> OnButtonAsObservable()
        {
            if (_button == null)
            {
                Debug.LogWarning("[CustomerListItemView] _button이 존재하지 않습니다.");
                return Observable.Empty<Unit>();
            }
            return _button.OnClickAsObservable();
        }
    }
}