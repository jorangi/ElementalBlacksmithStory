using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ObservableCollections;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;

namespace ElementalBlacksmithStory.UI
{
    public class BidPresenter : IDisposable
    {
        private const uint TEMP_DIALOGUE_ID = 600708;
        private SO_NPCData _npcData;
        private readonly Dictionary<uint, SO_DialogueData> _dialogueCache = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly PlayerData _playerData;
        private readonly BidView _view;
        private readonly TradingPostService _service;
        private readonly CompositeDisposable _disposables = new();
        private ulong _settedPrice;
        private readonly WeaponSpriteLoader _weaponSpriteLoader;
        private readonly MaterialSpriteLoader _materialSpriteLoader;
        private readonly RuneSpriteLoader _runeSpriteLoader;
        private readonly UIBackPanelPresenter _uiBackPanelPresenter;
        private uint _currentTargetId;
        [Inject]
        public BidPresenter(BidView view,
                            UIBackPanelPresenter uiBackPanelPresenter,
                            PlayerData playerData,
                            TradingPostService service,
                            WeaponSpriteLoader weaponSpriteLoader,
                            MaterialSpriteLoader materialSpriteLoader,
                            RuneSpriteLoader runeSpriteLoader,
                            SO_WeaponDatabase _weaponDatabase,
                            SO_MaterialDatabase _materialDatabase,
                            SO_RuneDatabase _runeDatabase
                            )
        {
            _view = view;
            _uiBackPanelPresenter = uiBackPanelPresenter;
            _service = service;
            _playerData = playerData;
            _weaponSpriteLoader = weaponSpriteLoader;
            _materialSpriteLoader = materialSpriteLoader;
            _runeSpriteLoader = runeSpriteLoader;
            _view.OnBidButtonAsObservable()
                .Subscribe(_ =>
                {
                    _service.Bid(_currentTargetId, new BidContext("player", _settedPrice));
                    _playerData.TrySpend(_settedPrice);
                    Hide();
                }).AddTo(_disposables);

            _view.OnCancelButtonAsObservable()
                .Subscribe(_ => { Hide(); }).AddTo(_disposables);

            _view.OnChangedBidPriceAsObservable()
                .Subscribe(e =>
                {
                    ulong money = _playerData.Money.CurrentValue;
                    if (e > money)
                    {
                        _settedPrice = money;
                        _view.SetBidPrice(_settedPrice);
                    }
                    else
                    {
                        _settedPrice = e;
                    }
                    _service.HighestBids.TryGetValue(_currentTargetId, out var bid);
                    _view.SetInteract(IsBidValid(bid));
                }).AddTo(_disposables);

            _view.OnIncreaseButtonAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(_ =>
                {
                    _settedPrice = Math.Clamp(_settedPrice + GetPriceStep(_settedPrice), 0, _playerData.Money.CurrentValue);
                    _view.SetBidPrice(_settedPrice);
                    _service.HighestBids.TryGetValue(_currentTargetId, out var bid);
                    _view.SetInteract(IsBidValid(bid));
                }).AddTo(_disposables);

            _view.OnDecreaseButtonAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(20))
                .Subscribe(_ =>
                {
                    _settedPrice = Math.Clamp(_settedPrice - GetPriceStep(_settedPrice), 0, _playerData.Money.CurrentValue);
                    _view.SetBidPrice(_settedPrice);
                    _service.HighestBids.TryGetValue(_currentTargetId, out var bid);
                    _view.SetInteract(IsBidValid(bid));
                }).AddTo(_disposables);

            _service.HighestBids.CollectionChanged += OnChangedHighestBids;
        }
        public void OnChangedHighestBids(in NotifyCollectionChangedEventArgs<KeyValuePair<uint, BidContext>> e)
        {
            if (e.NewItem.Key == _currentTargetId && (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace))
            {
                SetHighestBid(e.NewItem.Value);
            }
            else if (e.OldItem.Key == _currentTargetId && (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset))
            {
                _currentTargetId = 0;
                Hide();
            }
        }
        public async UniTask SetItem(IShopItem item)
        {
            if (item == null) return;

            _currentTargetId = item.Id;
            if (_service.HighestBids.TryGetValue(_currentTargetId, out var bid))
            {
                SetHighestBid(bid);
            }

            Sprite sprite = null;
            switch (ItemIdHelper.GetCategory(item.ItemId))
            {
                case ItemCategory.Weapon:
                    sprite = await _weaponSpriteLoader.GetSprite(item.ItemId);
                    break;
                case ItemCategory.Material:
                    sprite = await _materialSpriteLoader.GetSprite(item.ItemId);
                    break;
                case ItemCategory.Rune:
                    sprite = await _runeSpriteLoader.GetSprite(item.ItemId);
                    break;
                default:
                    break;
            }
            _view.SetItem(item.Name, sprite, item.Count);
        }
        public void SetHighestBid(BidContext bid)
        {
            _view.SetHighestBid(bid.Bidder, bid.Price, bid.Bidder == "player");
        }
        public ulong GetPriceStep(ulong price)
        {
            if (price < 10) return 1;

            int power = (int)Math.Log10(price);
            return (ulong)Math.Pow(10, Math.Max(0, power - 1));
        }
        public void Show()
        {
            _uiBackPanelPresenter.Show();
            _view.Show().Forget();
            _service.HighestBids.TryGetValue(_currentTargetId, out var bid);

            _settedPrice = bid.Price;
            _view.SetBidPrice(_settedPrice);
            _view.SetInteract(IsBidValid(bid));

            SetDialogueByIdAsync(TEMP_DIALOGUE_ID, ct: _cts.Token).Forget();
        }

        private bool IsBidValid(BidContext bid) => string.IsNullOrEmpty(bid.Bidder) ? _settedPrice >= bid.Price : _settedPrice > bid.Price;
        public void Hide()
        {
            _uiBackPanelPresenter.Hide();
            _view.Hide().Forget();
        }
        private async UniTask<SO_NPCData> GetNPCDataAsync(uint speakerId, CancellationToken ct = default)
        {
            if (speakerId == 0) return null;
            if (_npcData != null) return _npcData;

            try
            {
                var handle = Addressables.LoadAssetAsync<SO_NPCData>(speakerId.ToString());
                _npcData = await handle.ToUniTask(cancellationToken: ct);
                return _npcData;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BidPresenter] NPC 데이터({speakerId}) 로드 실패: {ex.Message}");
            }
            return null;
        }
        /// <summary>
        /// 대사 ID로 SO_DialogueData를 로드하고 조건문, 플레이스홀더, 한국어 조사를 파싱하여 출력합니다.
        /// </summary>
        public async UniTask SetDialogueByIdAsync(uint dialogueId, IReadOnlyDictionary<string, object> parameters = null, int lineIndex = 0, CancellationToken ct = default)
        {
            try
            {
                if (!_dialogueCache.TryGetValue(dialogueId, out var dialogueData) || dialogueData == null)
                {
                    var handle = Addressables.LoadAssetAsync<SO_DialogueData>(dialogueId.ToString());
                    dialogueData = await handle.ToUniTask(cancellationToken: ct);
                    if (dialogueData != null)
                    {
                        _dialogueCache[dialogueId] = dialogueData;
                    }
                }

                if (dialogueData != null && dialogueData.contents != null && dialogueData.contents.Count > 0)
                {
                    if (lineIndex < 0 || lineIndex >= dialogueData.contents.Count)
                    {
                        lineIndex = 0;
                    }

                    var line = dialogueData.contents[lineIndex];
                    if (line.speakerId != 0)
                    {
                        var npcData = await GetNPCDataAsync(line.speakerId, ct);
                        if (npcData != null)
                        {
                            _view.SetNPCName(npcData.NpcName);
                        }
                    }
                    string formattedText = line.GetFormattedText(parameters);
                    _view.SetDialogue(formattedText);
                }
                else
                {
                    Debug.LogWarning($"[BidPresenter] 대사 데이터({dialogueId})를 찾을 수 없습니다.");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BidPresenter] 대사 데이터({dialogueId}) 로드 실패: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _service.HighestBids.CollectionChanged -= OnChangedHighestBids;
            _disposables.Dispose();
        }
    }
}