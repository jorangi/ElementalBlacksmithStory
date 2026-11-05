using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.UI
{
    public class BargainRegisterItemDetailPresenter : IDisposable
    {
        private readonly BargainRegisterItemDetailView _view;
        private readonly WeaponSpriteLoader _weaponSpriteLoader;
        private readonly MaterialSpriteLoader _materialSpriteLoader;
        private readonly RuneSpriteLoader _runeSpriteLoade;
        private readonly CompositeDisposable _disposables = new();
        private IShopItem _cachedItem = null;
        private uint _registerAmount = 0;

        [Inject]
        public BargainRegisterItemDetailPresenter(
            BargainRegisterItemDetailView view,
            WeaponSpriteLoader weaponSpriteLoader,
            MaterialSpriteLoader materialSpriteLoader,
            RuneSpriteLoader runeSpriteLoader,
            IPublisher<RegisterBargainItemEvent> registerEventPublisher
            )
        {
            _view = view;
            _weaponSpriteLoader = weaponSpriteLoader;
            _materialSpriteLoader = materialSpriteLoader;
            _runeSpriteLoade = runeSpriteLoader;
            _view.OnRegisterButtonAsObservable()
                .Subscribe(_ =>
                {
                    if (_registerAmount > 0)
                        registerEventPublisher.Publish(new RegisterBargainItemEvent
                        {
                            _itemId = _cachedItem.ItemId,
                            _amount = _registerAmount
                        });
                    Hide();
                }).AddTo(_disposables);
            _view.OnCancelButtonAsObservable()
                .Subscribe(_ =>
                {
                    Hide();
                }).AddTo(_disposables);
            _view.OnIncreaseButtonAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(_ =>
                {
                    if (_registerAmount + 1 > _cachedItem.Count) _registerAmount = 1;
                    else _registerAmount = Math.Clamp(_registerAmount + 1, 0, _cachedItem.Count);
                    _view.SetRegisterAmount(_registerAmount);
                }).AddTo(_disposables);
            _view.OnDecreaseButtonAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(_ =>
                {
                    _registerAmount = Math.Clamp(_registerAmount - 1, 0, _cachedItem.Count);
                    _view.SetRegisterAmount(_registerAmount);
                }).AddTo(_disposables);
            _view.OnAmountChangedAsObservable()
                .Subscribe(amount =>
                {
                    _registerAmount = Math.Clamp(amount, 0, _cachedItem.Count);
                    _view.SetRegisterAmount(_registerAmount);
                }).AddTo(_disposables);
        }
        public async UniTask SetData(IShopItem item, CancellationToken ct = default)
        {
            if (item == null) return;
            _cachedItem = item;
            Sprite sprite = null;
            try
            {
                switch (ItemIdHelper.GetCategory(item.ItemId))
                {
                    case ItemCategory.Weapon:
                        sprite = await _weaponSpriteLoader.GetSprite(item.ItemId);
                        break;
                    case ItemCategory.Material:
                        sprite = await _materialSpriteLoader.GetSprite(item.ItemId);
                        break;
                    case ItemCategory.Rune:
                        sprite = await _runeSpriteLoade.GetSprite(item.ItemId);
                        break;
                    default:
                        Debug.LogWarning($"[BargainRegisterItemDetailPresenter] 해당 카테고리에 대한 스프라이트가 존재하지 않습니다. {ItemIdHelper.GetCategory(item.ItemId)}");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError($"[BargainRegisterItemDetailPresenter] {e.Message}");
            }
            _view.SetData(item.Name, sprite, item.Count);
        }
        public void Show()
        {
            _registerAmount = 0;
            _view.SetRegisterAmount(0);
            _view.Show().Forget();
        }
        public void Hide()
        {
            _view.Hide().Forget();
        }
        public void Dispose()
        {
            _disposables.Dispose();
            _disposables.Clear();
        }
    }
}