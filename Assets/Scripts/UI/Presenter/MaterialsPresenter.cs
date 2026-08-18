using System;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using R3;
using MessagePipe;
using UnityEngine;

namespace ElementalBlacksmithStory.UI
{
    public class MaterialsPresenter : IStartable, IDisposable
    {
        private readonly MaterialsView _view;
        private readonly CompositeDisposable _disposables = new();
        private readonly MaterialSpriteLoader _materialSpriteLoader;
        private readonly Dictionary<uint, uint> _temporaryInventory = new()
        {
            { 30001, 1 },
            { 30002, 5 },
            { 30003, 12 },
            { 30004, 1 },
            { 30005, 8 }
        };

        [Inject]
        public MaterialsPresenter(
            MaterialsView view,
            MaterialSpriteLoader materialSpriteLoader
            )
        {
            _materialSpriteLoader = materialSpriteLoader;
            _view = view;
        }
        float dragY = 0;
        bool isHidingTriggered = false;
        public void Start()
        {
            RefreshMaterialsView();

            _view.OnMaterialClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(item =>
                {
                    OnMaterialItemClicked(item.id, item.count, item.itemView);
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
                    if (dragY < -200f)
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
            
        }

        public void RefreshMaterialsView()
        {
            _view.DisplayMaterials(_temporaryInventory,_materialSpriteLoader).Forget();
        }

        private void OnMaterialItemClicked(uint materialId, uint count, MaterialItemView itemView)
        {
            Debug.Log($"[MaterialsPresenter] 재료 클릭 수신 - ID: {materialId}, 보유 수량: {count}");

            if (count == 1)
            {
                itemView.SelectOnce();
            }
            else if (count > 1)
            {
                itemView.Select();
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}