using ElementalBlacksmithStory.Data;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class MarketView : MonoBehaviour
    {
        [Header("상점 데이터")]
        [SerializeField] private SO_ShopData generalShopData;
        [SerializeField] private SO_ShopData equipmentShopData;

        [Header("상점 세션 오브젝트")]
        [SerializeField] private GameObject bubble;
        [SerializeField] private GameObject shopKeeper;
        [SerializeField] private GameObject shopBox;

        [Header("버튼")]
        [SerializeField] private Button generalShopBtn;
        [SerializeField] private Button equipmentShopBtn;
        [SerializeField] private Button auctionBtn;
        [SerializeField] private Button closeBtn;

        public Observable<Unit> OnGeneralShopBtnClickAsObservable => generalShopBtn.OnClickAsObservable();
        public Observable<Unit> OnEquipmentShopBtnClickAsObservable => equipmentShopBtn.OnClickAsObservable();
        public Observable<Unit> OnAuctionBtnClickAsObservable => auctionBtn.OnClickAsObservable();
        public Observable<Unit> OnCloseBtnClickAsObservable => closeBtn.OnClickAsObservable();

        public SO_ShopData GetShopData(MarketType type) => type switch
        {
            MarketType.GENERAL => generalShopData,
            MarketType.WEAPON => equipmentShopData,
            _ => null
        };

        public void Show(MarketType type)
        {
            if (bubble != null) bubble.SetActive(true);
            if (shopKeeper != null) shopKeeper.SetActive(true);
            if (shopBox != null) shopBox.SetActive(true);

            if (generalShopBtn != null) generalShopBtn.gameObject.SetActive(false);
            if (equipmentShopBtn != null) equipmentShopBtn.gameObject.SetActive(false);
            if (auctionBtn != null) auctionBtn.gameObject.SetActive(false);
        }

        public void Hide()
        {
            if (bubble != null) bubble.SetActive(false);
            if (shopKeeper != null) shopKeeper.SetActive(false);
            if (shopBox != null) shopBox.SetActive(false);

            if (generalShopBtn != null) generalShopBtn.gameObject.SetActive(true);
            if (equipmentShopBtn != null) equipmentShopBtn.gameObject.SetActive(true);
            if (auctionBtn != null) auctionBtn.gameObject.SetActive(true);
        }
    }
}