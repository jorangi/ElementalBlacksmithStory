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
using ElementalBlacksmithStory.Inventory;
using ElementalBlacksmithStory.Core;

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
        private readonly IPublisher<SubmitMaterialEvent> _submitMaterialPublisher;
        private readonly IPublisher<EnhanceButtonPositionEvent> _positionPublisher;
        private readonly MaterialInventory _inventory;
        private readonly ISubscriber<NextRecipeChangedEvent> _nextRecipeSubscriber;
        private SO_CraftRecipe _currentNextRecipe;

        [Inject]
        public MaterialsPresenter(
            MaterialsView view,
            SelectMaterialAmountView selectMaterialAmountView,
            SO_MaterialDatabase materialDatabase,
            MaterialSpriteLoader materialSpriteLoader,
            IPublisher<SubmitMaterialEvent> submitMaterialPublisher,
            IPublisher<EnhanceButtonPositionEvent> positionPublisher,
            MaterialInventory inventory,
            ISubscriber<NextRecipeChangedEvent> nextRecipeSubscriber
            )
        {
            _materialSpriteLoader = materialSpriteLoader;
            _materialDatabase = materialDatabase;
            _view = view;
            _selectMaterialAmountView = selectMaterialAmountView;
            _submitMaterialPublisher = submitMaterialPublisher;
            _positionPublisher = positionPublisher;
            _inventory = inventory;
            _nextRecipeSubscriber = nextRecipeSubscriber;
        }
        float dragY = 0;
        bool isHidingTriggered = false;
        public void Start()
        {
            RefreshMaterialsView();

            _nextRecipeSubscriber.Subscribe(OnNextRecipeChanged).AddTo(_disposables);

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
                    _positionPublisher.Publish(new EnhanceButtonPositionEvent(0f));
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
                        _positionPublisher.Publish(new EnhanceButtonPositionEvent(-1011f));
                        dragY = 0f;
                        return;
                    }
                    _view.SyncYPosition(dragY);
                    _positionPublisher.Publish(new EnhanceButtonPositionEvent(dragY));
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
                    _positionPublisher.Publish(new EnhanceButtonPositionEvent(0f));
                })
                .AddTo(_disposables);
            
            _selectMaterialAmountView.OnChangedAmountAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(e =>
                {
                    _selectedAmount.Value = Math.Clamp(e, 0, Math.Min(MAX_AMOUNT, _selectedItemView.MaterialCount));
                    _selectMaterialAmountView.SetAmount(_selectedAmount.Value);
                })
                .AddTo(_disposables);
            _selectMaterialAmountView.OnIncreaseAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(_ =>
                {
                    _selectedAmount.Value = Math.Clamp(_selectedAmount.Value + 1, 0, Math.Min(MAX_AMOUNT, _selectedItemView.MaterialCount));
                    _selectMaterialAmountView.SetAmount(_selectedAmount.Value);
                })
                .AddTo(_disposables);
            
            _selectMaterialAmountView.OnDecreaseAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
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
            _view.DisplayMaterials(_inventory.GetAll() ,_materialSpriteLoader).Forget();
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
                itemView.SetData(materialId, _inventory.GetCount(_materialDatabase.GetMaterial(materialId)));
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
                Sprite sprite = await _materialSpriteLoader.GetSprite(_selectedMaterialId, ct);
                SO_MaterialData materialData = _materialDatabase.GetMaterial(_selectedMaterialId);
                _selectedItemView = itemView;
                _selectMaterialAmountView.SetData(sprite, materialData.materialName, _inventory.GetCount(materialData));
            }
        }
        private void OnNextRecipeChanged(NextRecipeChangedEvent e)
        {
            _currentNextRecipe = e.Recipe;

            foreach (var itemView in _view.GetAllItemViews())
            {
                if (itemView != null)
                {
                    itemView.UnCheck();
                    var matData = _materialDatabase.GetMaterial(itemView.MaterialId);
                    if (matData != null)
                    {
                        itemView.SetData(itemView.MaterialId, _inventory.GetCount(matData));
                    }
                    _submitMaterialPublisher.Publish(new SubmitMaterialEvent(itemView.MaterialId, 0));
                }
            }

            if (_currentNextRecipe == null || _currentNextRecipe.recipeMaterials == null)
            {
                return;
            }

            foreach (var recipeMat in _currentNextRecipe.recipeMaterials)
            {
                if (recipeMat.material == null) continue;

                uint matId = recipeMat.material.Id;
                uint neededCount = recipeMat.count;
                uint ownedCount = _inventory.GetCount(recipeMat.material);

                if (ownedCount > 0)
                {
                    uint selectCount = Math.Min(neededCount, ownedCount);
                    var itemView = _view.GetItemView(matId);
                    if (itemView != null)
                    {
                        itemView.Check();
                        itemView.SetAmount(selectCount);
                    }
                    _submitMaterialPublisher.Publish(new SubmitMaterialEvent(matId, selectCount));
                }
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}