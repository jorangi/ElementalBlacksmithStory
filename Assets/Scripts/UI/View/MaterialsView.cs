using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using R3.Triggers;
using UnityEngine.EventSystems;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Core;

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

        private void Awake()
        {
            if (_bagButton != null && !_bagButton.TryGetComponent<UIButtonSound>(out _))
            {
                _bagButton.gameObject.AddComponent<UIButtonSound>();
            }
        }

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
        private const float ANIM_DURATION = 0.25f;

        /// <summary>
        /// 재료창 보이게 하는 함수
        /// </summary>
        public async UniTaskVoid ShowingMaterialsAnimation()
        {
            if (materialPanel == null) return;

            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new();
            var token = _cts.Token;

            float startY = materialPanel.anchoredPosition.y;
            float targetY = 0f;
            float elapsed = 0f;

            while (elapsed < ANIM_DURATION)
            {
                if (token.IsCancellationRequested) return;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ANIM_DURATION);
                float ease = 1f - Mathf.Pow(1f - t, 3);
                float currentY = Mathf.LerpUnclamped(startY, targetY, ease);
                materialPanel.anchoredPosition = new Vector2(materialPanel.anchoredPosition.x, currentY);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            materialPanel.anchoredPosition = new Vector2(materialPanel.anchoredPosition.x, targetY);
            if (_handleButton != null) _handleButton.enabled = true;
        }

        /// <summary>
        /// 재료창 숨기는 함수
        /// </summary>
        public async UniTaskVoid HidingMaterialsAnimation()
        {
            if (materialPanel == null) return;

            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new();
            var token = _cts.Token;

            float startY = materialPanel.anchoredPosition.y;
            float targetY = -1011f;
            float elapsed = 0f;

            while (elapsed < ANIM_DURATION)
            {
                if (token.IsCancellationRequested) return;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ANIM_DURATION);
                float ease = 1f - Mathf.Pow(1f - t, 3);
                float currentY = Mathf.LerpUnclamped(startY, targetY, ease);
                materialPanel.anchoredPosition = new Vector2(materialPanel.anchoredPosition.x, currentY);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            materialPanel.anchoredPosition = new Vector2(materialPanel.anchoredPosition.x, targetY);
            if (_handleButton != null) _handleButton.enabled = true;
        }
        public void SyncYPosition(float y)
        {
            materialPanel.anchoredPosition = new(materialPanel.anchoredPosition.x, y);
        }
        public void DisableHandle()
        {
            _handleButton.enabled = false;
        }
        private readonly Dictionary<uint, MaterialItemView> _itemViews = new();
        public MaterialItemView GetItemView(uint materialId) => _itemViews.TryGetValue(materialId, out var view) ? view : null;
        public IEnumerable<MaterialItemView> GetAllItemViews() => _itemViews.Values;

        public void ClearMaterials()
        {
            _itemDisposables.Dispose();
            _itemDisposables = new CompositeDisposable();
            _itemViews.Clear();

            if (materialParent == null) return;

            foreach (Transform child in materialParent)
            {
                Destroy(child.gameObject);
            }
        }
        public async UniTaskVoid DisplayMaterials(IReadOnlyDictionary<SO_MaterialData, uint> materials, MaterialSpriteLoader loader)
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
                SO_MaterialData material = kvp.Key;
                uint count = kvp.Value;

                GameObject instance = Instantiate(materialItemPrefab, materialParent);
                instance.name = kvp.Key.Id.ToString();
                if (instance.TryGetComponent<MaterialItemView>(out var itemView))
                {
                    _itemViews[material.Id] = itemView;
                    itemView.SetData(material.Id, count);
                    Sprite sprite = await loader.GetSprite(material.Id, ct);
                    if(sprite == null)
                    {
                        Debug.LogError($"[MaterialsView] 재료 스프라이트를 불러오지 못했습니다 - ID: {material}");
                    }
                    else
                        itemView.SetIcon(sprite);
                    itemView.OnClickAsObservable()
                        .Subscribe(_ => _onMaterialClickSubject.OnNext((material.Id, count, itemView)))
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