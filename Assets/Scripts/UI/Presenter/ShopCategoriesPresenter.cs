using System;
using System.Collections.Generic;
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
        private readonly IPublisher<PlaySoundEvent> _soundPublisher;
        private readonly List<SO_ShopCategory> _categories = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly CompositeDisposable _buttonDisposables = new();
        private string _selectedCategoryId = "all";

        [Inject]
        public ShopCategoriesPresenter(
            ShopCategoriesView view,
            ShopItemGridPresenter gridPresenter,
            IPublisher<PlaySoundEvent> soundPublisher)
        {
            _view = view;
            _gridPresenter = gridPresenter;
            _soundPublisher = soundPublisher;
        }

        public void Start() { }

        /// <summary>
        /// 상점 카테고리 목록을 데이터 기반으로 교체 (맨 앞에 '전체' 탭 자동 생성)
        /// </summary>
        public void SetCategories(IEnumerable<SO_ShopCategory> categories)
        {
            _buttonDisposables.Clear();
            _categories.Clear();


            if (_view == null) return;

            _view.HideAllButtons();

            // 1. "전체" 탭 자동 생성 (상점 내 모든 카테고리 물품 포괄)
            var allBtn = _view.CreateCategoryButton("all", "전체", null);
            if (allBtn != null)
            {
                allBtn.OnClickAsObservable()
                    .ThrottleFirst(TimeSpan.FromMilliseconds(150))
                    .Subscribe(_ => SelectCategory("all"))
                    .AddTo(_buttonDisposables);
            }

            // 2. 상점에 등록된 하위 카테고리 버튼 생성
            if (categories != null)
            {
                foreach (var cat in categories)
                {
                    if (cat == null) continue;
                    _categories.Add(cat);
                    var btnView = _view.CreateCategoryButton(cat.CategoryId, cat.DisplayName, cat.Icon);
                    if (btnView != null)
                    {
                        string catId = cat.CategoryId;
                        btnView.OnClickAsObservable()
                            .ThrottleFirst(TimeSpan.FromMilliseconds(150))
                            .Subscribe(_ => SelectCategory(catId))
                            .AddTo(_buttonDisposables);
                    }
                }
            }

            // 기본 첫 번째로 '전체' 선택
            SelectCategory("all");
        }

        /// <summary>
        /// 특정 카테고리 선택 처리
        /// </summary>
        public void SelectCategory(string categoryId)
        {
            _selectedCategoryId = categoryId;
            if (_view != null)
                _view.SetSelectedVisual(categoryId);
            _soundPublisher?.Publish(new PlaySoundEvent(40106));

            if (categoryId == "all")
            {
                // 전체: 상점의 모든 물품 표시
                _gridPresenter?.SetCategoryFilter(_ => true);
            }
            else
            {
                // 특정 카테고리: 해당 카테고리가 들고 있는 아이템 목록으로 필터링
                var selectedCat = _categories.Find(c => c.CategoryId == categoryId);
                _gridPresenter?.SetCategoryFilter(item => selectedCat != null && selectedCat.Contains(item));
            }
        }

        public void Dispose()
        {
            _buttonDisposables.Dispose();
            _disposables.Dispose();
        }
    }
}
