using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 상점 카테고리 제어 Presenter (데이터 기반 다중 상점 범용 지원)
    /// </summary>
    public class ShopCategoriesPresenter : IStartable, IDisposable
    {
        private readonly ShopCategoriesView _view;
        private readonly ShopItemGridPresenter _gridPresenter;
        private readonly MaterialSpriteLoader _materialSpriteLoader;
        private readonly IPublisher<PlaySoundEvent> _soundPublisher;

        private readonly List<ShopCategoryData> _categories = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly CompositeDisposable _buttonDisposables = new();
        private string _selectedCategoryId = "all";

        [Inject]
        public ShopCategoriesPresenter(
            ShopCategoriesView view,
            ShopItemGridPresenter gridPresenter,
            MaterialSpriteLoader materialSpriteLoader,
            IPublisher<PlaySoundEvent> soundPublisher)
        {
            _view = view;
            _gridPresenter = gridPresenter;
            _materialSpriteLoader = materialSpriteLoader;
            _soundPublisher = soundPublisher;
        }

        public void Start()
        {
            SetupDefaultGeneralShopCategories().Forget();
        }

        /// <summary>
        /// 잡화점 기본 카테고리 설정 (전체, 재료)
        /// </summary>
        public async UniTaskVoid SetupDefaultGeneralShopCategories()
        {
            Sprite materialIcon = null;
            if (_materialSpriteLoader != null)
            {
                materialIcon = await _materialSpriteLoader.GetSprite(30001);
            }

            var defaultCategories = new List<ShopCategoryData>
            {
                new ShopCategoryData("all", "전체", null, _ => true),
                new ShopCategoryData("material", "재료", materialIcon, item => item is MaterialShopItem || item.SpriteId.ToString()[0] == '3')
            };

            SetCategories(defaultCategories);
        }

        /// <summary>
        /// 상점 카테고리 목록을 데이터 기반으로 교체
        /// (장비점 등 다른 상점 진입 시 이 메서드를 통해 카테고리 데이터를 교체)
        /// </summary>
        public void SetCategories(IEnumerable<ShopCategoryData> categories)
        {
            _buttonDisposables.Clear();
            _categories.Clear();

            if (_view == null) return;

            foreach (var cat in categories)
            {
                _categories.Add(cat);
                var btnView = _view.CreateCategoryButton(cat.Id, cat.DisplayName, cat.Icon);
                if (btnView != null)
                {
                    string catId = cat.Id;
                    btnView.OnClickAsObservable()
                        .ThrottleFirst(TimeSpan.FromMilliseconds(150))
                        .Subscribe(_ => SelectCategory(catId))
                        .AddTo(_buttonDisposables);
                }
            }

            // 기본 첫번째 카테고리(전체) 선택
            SelectCategory(_categories.Count > 0 ? _categories[0].Id : "all");
        }

        /// <summary>
        /// 특정 카테고리 선택 처리
        /// </summary>
        public void SelectCategory(string categoryId)
        {
            _selectedCategoryId = categoryId;
            _view?.SetSelectedVisual(categoryId);
            _soundPublisher?.Publish(new PlaySoundEvent(40106));

            var selectedCat = _categories.Find(c => c.Id == categoryId);
            Func<IShopItem, bool> filter = selectedCat != null ? selectedCat.FilterPredicate : (_ => true);
            _gridPresenter?.SetCategoryFilter(filter);
        }

        public void Dispose()
        {
            _buttonDisposables.Dispose();
            _disposables.Dispose();
        }
    }
}
