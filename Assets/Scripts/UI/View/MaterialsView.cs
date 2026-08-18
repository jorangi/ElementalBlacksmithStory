using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using R3.Triggers;
using UnityEngine.EventSystems;

namespace ElementalBlacksmithStory.UI
{
    public class MaterialsView : MonoBehaviour
    {
        [Header("재료창")]
        [SerializeField] private RectTransform materialPanel;
        [Header("재료 아이템 부모")]
        [SerializeField] private Transform materialParent;

        [Header("재료 아이템 프리팹")]
        [SerializeField] private GameObject materialItemPrefab;
        [Header("자루 버튼")]
        [SerializeField] private Button _bagButton;
        [Header("재료창 핸들")]
        [SerializeField] private Image _handleButton;


        private readonly Subject<(uint id, uint count, MaterialItemView itemView)> _onMaterialClickSubject = new();
        private CompositeDisposable _itemDisposables = new();

        public Observable<(uint id, uint count, MaterialItemView itemView)> OnMaterialClickAsObservable() => _onMaterialClickSubject;
        public Observable<Unit> OnBagClickAsObservable()
        {
            if(_bagButton == null)
            {
                Debug.LogWarning("[MaterialsView] _bagButton이 인스펙터에 설정되지 않았습니다.");
                return Observable.Empty<Unit>();
            }
            return _bagButton.OnClickAsObservable();
        }
        public Observable<PointerEventData> OnHandleDragAsObservable()
        {
            if(_handleButton == null)
            {
                Debug.LogWarning("[MaterialsView] _handleButton이 인스펙터에 설정되지 않았습니다.");
                return Observable.Empty<PointerEventData>();
            }
            return _handleButton.OnDragAsObservable();
        }
        public Observable<PointerEventData> OnHandleEndDragAsObservable()
        {
            if(_handleButton == null)
            {
                Debug.LogWarning("[MaterialsView] _handleButton이 인스펙터에 설정되지 않았습니다.");
                return Observable.Empty<PointerEventData>();
            }
            return _handleButton.OnEndDragAsObservable();
        }
        private CancellationTokenSource _cts;
        /// <summary>
        /// 재료창 보이게 하는 함수
        /// </summary>
        /// <returns></returns>
        public async UniTaskVoid ShowingMaterialsAnimation()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new();
            var token = _cts.Token;
            while(materialPanel.anchoredPosition.y < -10)
            {
                if(token.IsCancellationRequested)
                {
                    break;
                }
                materialPanel.anchoredPosition = Vector2.Lerp(materialPanel.anchoredPosition, new(materialPanel.anchoredPosition.x, 0), 0.1f);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            materialPanel.anchoredPosition = new(materialPanel.anchoredPosition.x, 0);
            _handleButton.enabled = true;
        }
        /// <summary>
        /// 재료창 숨기는 함수
        /// </summary>
        /// <returns></returns>
        public async UniTaskVoid HidingMaterialsAnimation()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new();
            var token = _cts.Token;
            while(materialPanel.anchoredPosition.y > -1011)
            {
                if(token.IsCancellationRequested)
                {
                    break;
                }
                materialPanel.anchoredPosition = Vector2.Lerp(materialPanel.anchoredPosition, new(materialPanel.anchoredPosition.x, -1011), 0.1f);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            materialPanel.anchoredPosition = new(materialPanel.anchoredPosition.x, -1011);
            _handleButton.enabled = true;
        }
        public void SyncYPosition(float y)
        {
            materialPanel.anchoredPosition = new(materialPanel.anchoredPosition.x, y);
        }
        public void DisableHandle()
        {
            _handleButton.enabled = false;
        }
        public void ClearMaterials()
        {
            _itemDisposables.Dispose();
            _itemDisposables = new CompositeDisposable();

            if (materialParent == null) return;

            foreach (Transform child in materialParent)
            {
                Destroy(child.gameObject);
            }
        }
        public async UniTaskVoid DisplayMaterials(IReadOnlyDictionary<uint, uint> materials, MaterialSpriteLoader loader)
        {
            ClearMaterials();

            if (materialParent == null || materialItemPrefab == null)
            {
                Debug.LogWarning("[MaterialsView] materialParent 또는 materialItemPrefab이 인스펙터에 설정되지 않았습니다.");
                return;
            }
            var ct = this.GetCancellationTokenOnDestroy();

            foreach (var kvp in materials)
            {
                uint materialId = kvp.Key;
                uint count = kvp.Value;

                GameObject instance = Instantiate(materialItemPrefab, materialParent);
                instance.name = kvp.Key.ToString();
                if (instance.TryGetComponent<MaterialItemView>(out var itemView))
                {
                    itemView.SetData(materialId, count);
                    Sprite sprite = await loader.GetMaterialSprite(materialId.ToString(), ct);
                    if(sprite == null)
                    {
                        Debug.LogError($"[MaterialsView] 재료 스프라이트를 불러오지 못했습니다 - ID: {materialId}");
                    }
                    else
                        itemView.SetIcon(sprite);
                    itemView.OnClickAsObservable()
                        .Subscribe(_ => _onMaterialClickSubject.OnNext((materialId, count, itemView)))
                        .AddTo(_itemDisposables);
                }
            }
        }

        private void OnDestroy()
        {
            _itemDisposables.Dispose();
            _onMaterialClickSubject.Dispose();
        }
    }
}