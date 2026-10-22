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
        private readonly List<uint> _affectedRuneIds = new(8);

        /// <summary>
        /// 현재 임시 편집 슬롯과 무기 원래 슬롯을 비교하여 인벤토리 UI 가상 수량을 동기화
        /// </summary>
        public void SyncDraftRunes(IReadOnlyList<uint> currentRunes, IReadOnlyList<uint> originalRunes)
        {
            _affectedRuneIds.Clear();

            foreach (var kvp in _tempWithinRuneDic)
            {
                if (!_affectedRuneIds.Contains(kvp.Key))
                {
                    _affectedRuneIds.Add(kvp.Key);
                }
            }

            _tempWithinRuneDic.Clear();

            if (currentRunes != null)
            {
                for (int i = 0; i < currentRunes.Count; i++)
                {
                    uint id = currentRunes[i];
                    if (id == 0) continue;

                    if (_tempWithinRuneDic.TryGetValue(id, out var count))
                    {
                        _tempWithinRuneDic[id] = count + 1;
                    }
                    else
                    {
                        _tempWithinRuneDic[id] = 1;
                    }

                    if (!_affectedRuneIds.Contains(id))
                    {
                        _affectedRuneIds.Add(id);
                    }
                }
            }

            if (originalRunes != null)
            {
                for (int i = 0; i < originalRunes.Count; i++)
                {
                    uint id = originalRunes[i];
                    if (id == 0) continue;

                    if (_tempWithinRuneDic.TryGetValue(id, out var count))
                    {
                        _tempWithinRuneDic[id] = count - 1;
                    }
                    else
                    {
                        _tempWithinRuneDic[id] = -1;
                    }

                    if (!_affectedRuneIds.Contains(id))
                    {
                        _affectedRuneIds.Add(id);
                    }
                }
            }

            for (int i = 0; i < _affectedRuneIds.Count; i++)
            {
                uint runeId = _affectedRuneIds[i];
                int used = _tempWithinRuneDic.TryGetValue(runeId, out var count) ? count : 0;
                int baseCount = _runeInventory.GetCount(runeId);
                int displayCount = Mathf.Max(0, baseCount - used);
                UpdateRuneDisplayAmountAsync(runeId, (uint)displayCount).Forget();
            }
        }

        /// <summary>
        /// 특정 룬의 UI 표시 수량을 갱신하며, 필요 시 카드를 생성
        /// </summary>
        public async UniTask UpdateRuneDisplayAmountAsync(uint runeId, uint amount)
        {
            if (runeId == 0) return;

            if (_view.HasCard(runeId))
            {
                _view.SetRuneAmount(runeId, amount);
                _view.Sort();
            }
            else if (amount > 0 && _runeDatabase != null)
            {
                SO_RuneData runeData = _runeDatabase.Get(runeId);
                if (runeData != null)
                {
                    Sprite sprite = _runeSpriteLoader != null
                        ? await _runeSpriteLoader.GetSprite(runeId, _cts.Token)
                        : null;
                    bool isFavorite = _runeInventory.IsFavorite(runeId);
                    _view.Create(runeData, sprite, amount, isFavorite);
                    _view.Sort();
                }
            }
        }

        /// <summary>
        /// 장착 완료 또는 초기화 시 임시 슬롯 딕셔너리 비우고 인벤토리 원래 수량으로 복원
        /// </summary>
        public void ClearDraft()
        {
            if (_tempWithinRuneDic.Count == 0) return;

            _affectedRuneIds.Clear();
            foreach (var kvp in _tempWithinRuneDic)
            {
                _affectedRuneIds.Add(kvp.Key);
            }
            _tempWithinRuneDic.Clear();

            for (int i = 0; i < _affectedRuneIds.Count; i++)
            {
                uint runeId = _affectedRuneIds[i];
                int baseCount = _runeInventory.GetCount(runeId);
                UpdateRuneDisplayAmountAsync(runeId, (uint)baseCount).Forget();
            }
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
