using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class ShopTabView : MonoBehaviour
    {
        [SerializeField] private Button _buyTabButton;
        [SerializeField] private Button _sellTabButton;

        [Header("대사 ID 설정")]
        [SerializeField] private uint _buyDialogueId = 600307;
        [SerializeField] private uint _sellDialogueId = 600308;

        public uint BuyDialogueId => _buyDialogueId;
        public uint SellDialogueId => _sellDialogueId;

        public Observable<Unit> OnClickBuyTabAsObservable()
        {
            if (_buyTabButton == null) return Observable.Empty<Unit>();
            return _buyTabButton.OnClickAsObservable();
        }

        public Observable<Unit> OnClickSellTabAsObservable()
        {
            if (_sellTabButton == null) return Observable.Empty<Unit>();
            return _sellTabButton.OnClickAsObservable();
        }

        /// <summary>
        /// 탭 선택 비주얼 갱신 (선택된 탭 버튼 interactable/indicator 반영)
        /// </summary>
        public void SetTabVisual(bool isSellMode)
        {
            if (_buyTabButton != null)
            {
                _buyTabButton.interactable = isSellMode;
            }

            if (_sellTabButton != null)
            {
                _sellTabButton.interactable = !isSellMode;
            }

            if (!isSellMode)
            {
                _buyTabButton.image.color = new Color(1.0f, 0.7451f, 0.5922f, 1f);
                _sellTabButton.image.color = Color.white;
            }
            else
            {
                _buyTabButton.image.color = Color.white;
                _sellTabButton.image.color = new Color(1.0f, 0.7451f, 0.5922f, 1f);
            }
        }
    }
}
