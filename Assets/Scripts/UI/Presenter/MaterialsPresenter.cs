using System;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using R3;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using System.Threading;

namespace ElementalBlacksmithStory.UI
{
    public class MaterialsPresenter : IStartable, IDisposable
    {
        private const int MAX_AMOUNT = 99;
        private readonly MaterialsView _view;
        private readonly SelectMaterialAmountView _selectMaterialAmountView;
        private readonly CompositeDisposable _disposables = new();
        private readonly MaterialSpriteLoader _materialSpriteLoader;
        private readonly SO_MaterialDatabase _materialDatabase;
        public MaterialItemView _selectedItemView = null;
        public ReactiveProperty<uint> _selectedAmount = new();
        public uint _selectedMaterialId;
        private readonly Dictionary<SO_MaterialData, uint> _temporaryInventory = new(); // 다른데로 옮겨야댐
        private readonly IPublisher<SubmitMaterialEvent> _submitMaterialPublisher;
        [Inject]
        public MaterialsPresenter(
            MaterialsView view,
            SelectMaterialAmountView selectMaterialAmountView,
            SO_MaterialDatabase materialDatabase,
            MaterialSpriteLoader materialSpriteLoader,
            IPublisher<SubmitMaterialEvent> submitMaterialPublisher
            )
        {
            _materialSpriteLoader = materialSpriteLoader;
            _materialDatabase = materialDatabase;
            _view = view;
            _selectMaterialAmountView = selectMaterialAmountView;
            _submitMaterialPublisher = submitMaterialPublisher;

        }
        float dragY = 0;
        bool isHidingTriggered = false;
        public void Start()
        {
            _temporaryInventory.TryAdd(_materialDatabase.GetMaterial(30001), (uint)UnityEngine.Random.Range(1, 100));
            _temporaryInventory.TryAdd(_materialDatabase.GetMaterial(30002), (uint)UnityEngine.Random.Range(1, 100));
            _temporaryInventory.TryAdd(_materialDatabase.GetMaterial(30003), (uint)UnityEngine.Random.Range(1, 100));
            _temporaryInventory.TryAdd(_materialDatabase.GetMaterial(30004), (uint)UnityEngine.Random.Range(1, 100));
            _temporaryInventory.TryAdd(_materialDatabase.GetMaterial(30005), (uint)UnityEngine.Random.Range(1, 100));
            _temporaryInventory.TryAdd(_materialDatabase.GetMaterial(30006), (uint)UnityEngine.Random.Range(1, 100));
            _temporaryInventory.TryAdd(_materialDatabase.GetMaterial(30007), 1);

            RefreshMaterialsView();

            _view.OnMaterialClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(item =>
                {
                    OnMaterialItemClicked(item.id, item.count, item.itemView).Forget();
                })
                .AddTo(_disposables);

            _view.OnBagClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    _view.ShowingMaterialsAnimation().Forget();
                })
                .AddTo(_disposables);

            _view.OnHandleDragAsObservable()
                .Subscribe(e =>
                {
                    if (isHidingTriggered) return;

                    dragY = Mathf.Min(0f, dragY + e.delta.y);
                    if (dragY < -400f)
                    {
                        isHidingTriggered = true;
                        _view.DisableHandle();
                        _view.HidingMaterialsAnimation().Forget();
                        dragY = 0f;
                        return;
                    }
                    _view.SyncYPosition(dragY);
                })
                .AddTo(_disposables);
            
            _view.OnHandleEndDragAsObservable()
                .Subscribe(e =>
                {
                    dragY = 0;
                    if (isHidingTriggered)
                    {
                        isHidingTriggered = false;
                        return;
                    }
                    _view.ShowingMaterialsAnimation().Forget();
                })
                .AddTo(_disposables);
            
            _selectMaterialAmountView.OnChangedAmountAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(e =>
                {
                    _selectedAmount.Value = Math.Clamp(e, 0, Math.Min(MAX_AMOUNT, _selectedItemView.MaterialCount));
                    _selectMaterialAmountView.SetAmount(_selectedAmount.Value);
                })
                .AddTo(_disposables);
            _selectMaterialAmountView.OnIncreaseAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    _selectedAmount.Value = Math.Clamp(_selectedAmount.Value + 1, 0, Math.Min(MAX_AMOUNT, _selectedItemView.MaterialCount));
                    _selectMaterialAmountView.SetAmount(_selectedAmount.Value);
                })
                .AddTo(_disposables);
            
            _selectMaterialAmountView.OnDecreaseAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    _selectedAmount.Value = Math.Clamp(_selectedAmount.Value - 1, 0, Math.Min(MAX_AMOUNT, _selectedItemView.MaterialCount));
                    _selectMaterialAmountView.SetAmount(_selectedAmount.Value);
                })
                .AddTo(_disposables);
            
            _selectMaterialAmountView.OnSubmitAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    if(_selectedAmount.Value == 0) return;
                    Debug.Log($"[MaterialPresenter] 선택한 재료: {_selectedAmount.Value}개");
                    _selectedItemView.Select();
                    _selectedItemView.SetAmount(_selectedAmount.Value);
                    _selectedItemView.Check();
                    _submitMaterialPublisher.Publish(new SubmitMaterialEvent(_selectedMaterialId, _selectedAmount.Value));
                    _selectMaterialAmountView.Hide();
                })
                .AddTo(_disposables);
            _selectMaterialAmountView.OnCancelAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ =>
                {
                    _selectMaterialAmountView.Hide();
                })
                .AddTo(_disposables);
        }


        public void RefreshMaterialsView()
        {
            _view.DisplayMaterials(_temporaryInventory,_materialSpriteLoader).Forget();
        }
        
        private CancellationTokenSource _cts = new();
        private async UniTask OnMaterialItemClicked(uint materialId, uint count, MaterialItemView itemView)
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            _selectedMaterialId = 0;
            _selectedAmount.Value = 0;
            if(itemView.IsSelected)
            {
                itemView.UnCheck();
                itemView.SetData(materialId, _temporaryInventory[_materialDatabase.GetMaterial(materialId)]);
                return;
            }
            Debug.Log($"[MaterialsPresenter] 재료 클릭 수신 - ID: {materialId}, 보유 수량: {count}");
            if (count == 1)
            {
                itemView.SelectOnce();
                _submitMaterialPublisher.Publish(new SubmitMaterialEvent(_selectedMaterialId, 1));
            }
            else if (count > 1)
            {
                _selectedMaterialId = itemView.Select();
                Sprite sprite = await _materialSpriteLoader.GetMaterialSprite(_selectedMaterialId.ToString(), ct);
                SO_MaterialData materialData = _materialDatabase.GetMaterial(_selectedMaterialId);
                _selectedItemView = itemView;
                _selectMaterialAmountView.SetData(sprite, materialData.materialName, _temporaryInventory[materialData]);
            }
        }        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}