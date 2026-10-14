using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Inventory;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class RuneInventoryPresenter : IAsyncStartable, IDisposable
    {
        private readonly RuneInventoryView _view;
        private readonly RuneInventory _runeInventory;
        private readonly RuneSpriteLoader _runeSpriteLoader;
        private readonly SO_RuneDatabase _runeDatabase;
        private readonly CompositeDisposable _disposables = new();
        private readonly CancellationTokenSource _cts = new();
        private IRuneUIState _uiState;
        /// <summary>
        /// Apply이전 임시 슬롯(UI 차감용)
        /// </summary>
        private Dictionary<uint, int> _tempWithinRuneDic = new();
        [Inject]
        public RuneInventoryPresenter(
            RuneInventoryView view,
            RuneInventory runeInventory,
            RuneSpriteLoader runeSpriteLoader,
            SO_RuneDatabase runeDatabase = null)
        {
            _view = view;
            _runeInventory = runeInventory;
            _runeSpriteLoader = runeSpriteLoader;
            _runeDatabase = runeDatabase;
        }

        public async UniTask StartAsync(CancellationToken ct = default)
        {
            BindEvents();
            await RefreshViewAsync(ct);
        }
        public void SetRuneUIState(IRuneUIState uiState) => _uiState = uiState;
        private void BindEvents()
        {
            _view.OnCardClicked
                .Subscribe(id =>
                {
                    _uiState.ChangeRune(id);
                })
                .AddTo(_disposables);

            _view.OnFavoriteClicked
                .Subscribe(runeId => OnToggleFavorite(runeId))
                .AddTo(_disposables);

            _view.OnSearchInputChanged
                .ThrottleLast(TimeSpan.FromMilliseconds(50))
                .Subscribe(keyword => _view.FilterByKeyword(keyword))
                .AddTo(_disposables);

            _runeInventory.OnItemCountChangedAsObservable
                .Subscribe(entry =>
                {
                    OnRuneCountChangedAsync(entry.item, entry.count).Forget();
                })
                .AddTo(_disposables);

        }
        public async UniTask RefreshViewAsync(CancellationToken ct = default)
        {
            _view.Clear();

            // 임시 슬롯 비우기
            _tempWithinRuneDic.Clear();

            var allRunes = _runeInventory.GetAll();
            foreach (var kvp in allRunes)
            {
                var runeData = kvp.Key;
                uint count = kvp.Value;
                if (runeData == null || count == 0) continue;

                Sprite sprite = _runeSpriteLoader != null
                    ? await _runeSpriteLoader.GetSprite(runeData.Id, ct)
                    : null;

                bool isFavorite = _runeInventory.IsFavorite(runeData.Id);
                _view.Create(runeData, sprite, count, isFavorite);
            }

            _view.Sort();
        }
        /// <summary>
        /// 임시 슬롯에 룬을 배정
        /// </summary>
        /// <param name="runeId"></param>
        public void SelectRune(uint runeId)
        {
            if (runeId == 0) return;

            if (_tempWithinRuneDic.TryGetValue(runeId, out var count))
            {
                _tempWithinRuneDic[runeId] = count + 1;
            }
            else
            {
                _tempWithinRuneDic.Add(runeId, 1);
            }

            int runeEA = _runeInventory.GetCount(runeId);
            int remaining = Mathf.Max(0, runeEA - _tempWithinRuneDic[runeId]);
            _view.SetRuneAmount(runeId, (uint)remaining);
        }
        /// <summary>
        /// 임시 슬롯에서 룬을 해제
        /// </summary>
        /// <param name="runeId"></param>
        public void UnselectRune(uint runeId)
        {
            if (runeId == 0) return;

            if (_tempWithinRuneDic.TryGetValue(runeId, out var count))
            {
                if (count <= 1)
                {
                    _tempWithinRuneDic.Remove(runeId);
                }
                else
                {
                    _tempWithinRuneDic[runeId] = count - 1;
                }
            }

            int runeEA = _runeInventory.GetCount(runeId);
            int used = _tempWithinRuneDic.TryGetValue(runeId, out var currentUsed) ? currentUsed : 0;
            int remaining = Mathf.Max(0, runeEA - used);
            _view.SetRuneAmount(runeId, (uint)remaining);
        }
        /// <summary>
        /// 장착 완료 또는 초기화 시 임시 슬롯 딕셔너리 비우기
        /// </summary>
        public void ClearDraft()
        {
            _tempWithinRuneDic.Clear();
        }
        private void OnToggleFavorite(uint runeId)
        {
            bool isFavorite = _runeInventory.ToggleFavorite(runeId);
            _view.SetFavorite(runeId, isFavorite);
            _view.Sort();
        }
        private async UniTaskVoid OnRuneCountChangedAsync(SO_RuneData runeData, uint count)
        {
            if (runeData == null) return;

            if (count > 0)
            {
                Sprite sprite = _runeSpriteLoader != null
                    ? await _runeSpriteLoader.GetSprite(runeData.Id, _cts.Token)
                    : null;

                bool isFavorite = _runeInventory.IsFavorite(runeData.Id);
                _view.Create(runeData, sprite, count, isFavorite);
                _view.Sort();
            }
            else
            {
                _view.SetRuneAmount(runeData.Id, 0);
            }
        }
        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
        }
    }
}
