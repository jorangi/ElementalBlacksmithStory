using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using ObservableCollections;
using R3;
using UnityEngine;
using VContainer;

namespace ElementalBlacksmithStory.Core
{
    /// <summary>
    /// 교역소 흥정(Bargain) 출품 물품에 손님들이 주기적으로 방문하는 시뮬레이션을 전담하는 서비스
    /// </summary>
    public class BargainSimulatingService : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly SO_CustomerGroupDatabase _customerGroupDatabase;

        /// <summary>
        /// 아이템별 흥정 손님 목록
        /// </summary>
        private readonly Dictionary<uint, List<CustomerData>> _bargainCustomers = new();

        /// <summary>
        /// 아이템별 시뮬레이션 취소 토큰
        /// </summary>
        private readonly Dictionary<uint, CancellationTokenSource> _itemCts = new();

        /// <summary>
        /// 등록된 아이템 캐시
        /// </summary>
        private readonly Dictionary<uint, IShopItem> _registeredItems = new();

        /// <summary>
        /// 아이템별 현재 실행 중인 손님그룹 방문 루틴 목록
        /// </summary>
        private readonly Dictionary<uint, HashSet<uint>> _activeGroupRoutines = new();

        /// <summary>
        /// 새 손님 방문 시 통지
        /// </summary>
        private readonly Subject<(uint itemId, CustomerData customer)> _onCustomerArrived = new();
        public Observable<(uint itemId, CustomerData customer)> OnCustomerArrived => _onCustomerArrived;

        /// <summary>
        /// 손님 퇴장 시 통지
        /// </summary>
        private readonly Subject<(uint itemId, CustomerData customer)> _onCustomerLeft = new();
        public Observable<(uint itemId, CustomerData customer)> OnCustomerLeft => _onCustomerLeft;

        // 이름 추첨 시 가비지 방지용 재사용 버퍼
        private readonly List<string> _nameCandidateBuffer = new(16);
        private readonly Dictionary<string, object> _dialogueParams = new(16);
        private readonly PlayerData _playerData;

        private readonly Subject<IShopItem> _onItemSettledSubject = new();
        public Observable<IShopItem> OnItemSettledAsObservable => _onItemSettledSubject.AsObservable();

        private readonly Subject<IShopItem> _onItemWithdrawSubject = new();
        public Observable<IShopItem> OnItemWithdrawAsObservable => _onItemWithdrawSubject.AsObservable();

        private readonly IPublisher<AcquireItemEvent> _acquireItemEventPublisher;

        [Inject]
        public BargainSimulatingService(
            SO_CustomerGroupDatabase customerGroupDatabase,
            PlayerData playerData,
            IPublisher<AcquireItemEvent> acquireItemEventPublisher
        )
        {
            _playerData = playerData;
            _customerGroupDatabase = customerGroupDatabase;
            _acquireItemEventPublisher = acquireItemEventPublisher;
        }

        public IReadOnlyList<CustomerData> GetCustomers(uint itemId)
        {
            if (_bargainCustomers.TryGetValue(itemId, out var list))
            {
                return list;
            }
            return Array.Empty<CustomerData>();
        }

        public int GetCustomerCount(uint itemId)
        {
            return _bargainCustomers.TryGetValue(itemId, out var list) ? list.Count : 0;
        }

        /// <summary>
        /// 판매 등록된 아이템에 대한 손님 방문 시뮬레이션을 시작
        /// </summary>
        public void RegisterItem(IShopItem item)
        {
            if (item == null || _customerGroupDatabase == null) return;

            // 이미 등록된 경우 기존 루틴 정리
            RemoveItem(item.Id);

            var customerGroups = _customerGroupDatabase.GetAll();
            if (customerGroups == null || customerGroups.Count == 0) return;

            var cts = new CancellationTokenSource();
            _itemCts[item.Id] = cts;
            _registeredItems[item.Id] = item;
            _activeGroupRoutines[item.Id] = new HashSet<uint>();

            if (!_bargainCustomers.ContainsKey(item.Id))
            {
                _bargainCustomers[item.Id] = new List<CustomerData>(8);
            }

            var elements = (item is Weapon weapon && weapon.Data != null) ? weapon.Data.elementTypes : null;

            for (int i = 0; i < customerGroups.Count; i++)
            {
                var group = customerGroups[i];
                if (group == null) continue;

                if (group.IsItemEligible(item.Category, elements))
                {
                    StartRoutineForGroup(item, group, cts.Token);
                }
            }
        }

        /// <summary>
        /// 특정 손님 그룹의 방문 루틴을 시작합니다 (중복 실행 방지).
        /// </summary>
        private void StartRoutineForGroup(IShopItem item, SO_CustomerGroup group, CancellationToken ct)
        {
            if (!_activeGroupRoutines.TryGetValue(item.Id, out var set))
            {
                set = new HashSet<uint>();
                _activeGroupRoutines[item.Id] = set;
            }

            if (set.Add(group.Id))
            {
                SimulateCustomerArrivalRoutine(item, group, ct).Forget();
            }
        }

        /// <summary>
        /// 아이템 판매 완료 또는 등록 취소 시 시뮬레이션을 종료하고 손님 목록을 정리
        /// </summary>
        public void RemoveItem(uint itemId)
        {
            if (_itemCts.TryGetValue(itemId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _itemCts.Remove(itemId);
            }

            _registeredItems.Remove(itemId);
            _activeGroupRoutines.Remove(itemId);

            if (_bargainCustomers.TryGetValue(itemId, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Dispose();
                }
                list.Clear();
                _bargainCustomers.Remove(itemId);
            }
        }

        /// <summary>
        /// 손님 퇴장 함수
        /// 만약 해당 그룹이 만원이라 루틴이 멈춰있던 상태였다면, 빈자리가 생겼으므로 즉시 그 시점부터 해당 그룹의 방문 루틴을 재개
        /// </summary>
        public bool RemoveCustomer(uint itemId, CustomerData customer)
        {
            if (customer == null) return false;

            if (_bargainCustomers.TryGetValue(itemId, out var list))
            {
                if (list.Remove(customer))
                {
                    _onCustomerLeft.OnNext((itemId, customer));
                    customer.Dispose();

                    var group = customer.Group;
                    if (group != null &&
                        _registeredItems.TryGetValue(itemId, out var item) &&
                        _itemCts.TryGetValue(itemId, out var cts) &&
                        !cts.IsCancellationRequested)
                    {
                        bool isRunning = _activeGroupRoutines.TryGetValue(itemId, out var set) && set.Contains(group.Id);
                        if (!isRunning)
                        {
                            // 빈자리가 생겼으므로 즉시 해당 그룹의 방문 루틴을 다시 시작
                            StartRoutineForGroup(item, group, cts.Token);
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        private async UniTaskVoid SimulateCustomerArrivalRoutine(IShopItem item, SO_CustomerGroup group, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int delaySeconds = group.RegisterDelay(item);
                    if (delaySeconds <= 0) delaySeconds = 5;
                    delaySeconds = 1;
                    // Debug.Log($"{group._customerGroupName} {delaySeconds}초 -> {DateTime.Now.AddSeconds(delaySeconds):HH:mm:ss} 도착");
                    bool isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: ct)
                        .SuppressCancellationThrow();

                    if (isCancelled || ct.IsCancellationRequested) return;

                    bool added = TryAddCustomer(item, group);
                    if (!added)
                    {
                        return;
                    }
                }
            }
            finally
            {
                if (_activeGroupRoutines.TryGetValue(item.Id, out var set))
                {
                    set.Remove(group.Id);
                }
            }
        }

        /// <summary>
        /// 해당 그룹에서 아직 방문하지 않은 손님을 추첨하여 추가
        /// </summary>
        private bool TryAddCustomer(IShopItem item, SO_CustomerGroup group)
        {
            if (group == null || item == null) return false;

            if (!_bargainCustomers.TryGetValue(item.Id, out var customers))
            {
                customers = new List<CustomerData>(8);
                _bargainCustomers[item.Id] = customers;
            }

            var groupNames = group._customerNames;
            if (groupNames == null || groupNames.Count == 0)
            {
                for (int i = 0; i < customers.Count; i++)
                {
                    if (customers[i].Group == group) return false;
                }
                var defaultCustomer = new CustomerData(group, item);
                customers.Add(defaultCustomer);
                _onCustomerArrived.OnNext((item.Id, defaultCustomer));
                StartCustomerLifetimeRoutine(item.Id, defaultCustomer);
                return true;
            }

            _nameCandidateBuffer.Clear();
            for (int i = 0; i < groupNames.Count; i++)
            {
                _nameCandidateBuffer.Add(groupNames[i]);
            }

            for (int i = 0; i < customers.Count; i++)
            {
                var existing = customers[i];
                if (existing.Group == group)
                {
                    _nameCandidateBuffer.Remove(existing.Name);
                }
            }

            if (_nameCandidateBuffer.Count == 0)
            {
                Debug.LogWarning($"[BargainSimulatingService] '[{item.Id}] {item.Name}' 아이템에 '{group._customerGroupName}' 그룹의 모든 손님이 이미 등록되었습니다.");
                return false;
            }

            int chosenIdx = UnityEngine.Random.Range(0, _nameCandidateBuffer.Count);
            string chosenName = _nameCandidateBuffer[chosenIdx];
            _nameCandidateBuffer.Clear();

            var newCustomer = new CustomerData(group, item);
            newCustomer.SetCustomerName(chosenName);
            customers.Add(newCustomer);

            _onCustomerArrived.OnNext((item.Id, newCustomer));
            StartCustomerLifetimeRoutine(item.Id, newCustomer);
            return true;
        }

        /// <summary>
        /// 손님의 거래 신청 리스트 잔류 시간 타이머 루틴을 시작
        /// </summary>
        private void StartCustomerLifetimeRoutine(uint itemId, CustomerData customer)
        {
            if (customer == null || customer.Lifetime <= 0) return;
            if (!_itemCts.TryGetValue(itemId, out var itemCts)) return;

            SimulateCustomerLifetimeRoutine(itemId, customer, itemCts.Token).Forget();
        }

        private async UniTaskVoid SimulateCustomerLifetimeRoutine(uint itemId, CustomerData customer, CancellationToken itemCt)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(itemCt, customer.CancellationToken);
            try
            {
                bool isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(customer.Lifetime), cancellationToken: linkedCts.Token)
                    .SuppressCancellationThrow();

                if (isCancelled || linkedCts.IsCancellationRequested) return;

                Debug.Log($"[BargainSimulatingService] 손님 '{customer.Name}'의 잔류 시간({customer.Lifetime}초)이 만료되어 퇴장합니다.");
                RemoveCustomer(itemId, customer);
            }
            catch (OperationCanceledException)
            {
                // 취소 시 정상 종료
            }
        }

        /// <summary>
        /// 대사 템플릿에 사용할 파라미터를 갱신합니다.
        /// </summary>
        private void UpdateDialogueParams(IShopItem item, CustomerData customer, ulong currentPrice, ulong prevPrice, ulong askPrice)
        {
            _dialogueParams.Clear();
            if (item == null || customer == null) return;

            _dialogueParams["itemType"] = item.Category.ToString();
            _dialogueParams["itemCount"] = item.Count;
            _dialogueParams["itemName"] = item.Name;
            _dialogueParams["npcName"] = customer.Name;
            _dialogueParams["price"] = currentPrice;
            _dialogueParams["askPrice"] = askPrice;
            _dialogueParams["priceGap"] = askPrice >= prevPrice ? askPrice - prevPrice : prevPrice - askPrice;
            _dialogueParams["unitPrice"] = item.Count > 0 ? currentPrice / item.Count : currentPrice;
            _dialogueParams["prevPrice"] = prevPrice;
            _dialogueParams["firstPrice"] = customer.FirstOfferPrice;
            _dialogueParams["askRatio"] = prevPrice > 0 ? (float)askPrice / prevPrice : 1.0f;
            _dialogueParams["bargainCount"] = customer.BargainCount;

            string element = "None";
            if (item is Weapon weapon && weapon.Data != null && weapon.Data.elementTypes != null && weapon.Data.elementTypes.Count > 0)
            {
                element = weapon.Data.elementTypes[0].ToString();
            }
            _dialogueParams["itemElement"] = element;
            _dialogueParams["groupName"] = customer.Group != null ? customer.Group._customerGroupName : string.Empty;
            _dialogueParams["baseValue"] = item.Value * (item.Count > 0 ? (ulong)item.Count : 1UL);
        }

        /// <summary>
        /// 손님의 첫 제안 대사를 포맷팅하여 반환합니다.
        /// </summary>
        public string GetFirstOfferDialogue(IShopItem item, CustomerData customer)
        {
            if (item == null || customer == null || customer.Group == null) return string.Empty;
            ulong price = customer.FirstOfferPrice;
            UpdateDialogueParams(item, customer, currentPrice: price, prevPrice: price, askPrice: 0);
            var dialogues = customer.Group._firstOffer;
            return (dialogues != null && dialogues.Count > 0)
                ? dialogues[UnityEngine.Random.Range(0, dialogues.Count)].GetFormattedText(_dialogueParams)
                : string.Empty;
        }

        public void WithDraw(IShopItem item)
        {
            if (item == null) return;
            if (!_registeredItems.ContainsKey(item.Id)) return;

            _onItemWithdrawSubject.OnNext(item);
            RemoveItem(item.Id);
            if (item is Weapon weapon)
            {
                _acquireItemEventPublisher.Publish(new AcquireItemEvent(
                    weapon: weapon,
                    sender: "BargainSimulatingService",
                    target: "player",
                    reason: "Withdraw"
                ));
            }
            else
            {
                _acquireItemEventPublisher.Publish(new AcquireItemEvent(
                    itemId: item.ItemId,
                    amount: item.Count,
                    sender: "BargainSimulatingService",
                    target: "player",
                    reason: "Withdraw"
                ));
            }
        }
        /// <summary>
        /// 플레이어가 가격을 제시하는 함수
        /// </summary>
        /// <param name="item"></param>
        /// <param name="customer"></param>
        /// <param name="playerPrice"></param>
        /// <param name="customerPrice"></param>
        /// <returns></returns>
        public string TryBargain(IShopItem item, CustomerData customer, ulong playerPrice, out ulong customerPrice)
        {
            customerPrice = 0UL;
            if (item == null)
            {
                Debug.LogError("[BargainSimulatingService] item이 존재하지 않습니다.");
                return string.Empty;
            }
            if (customer == null)
            {
                Debug.LogError("[BargainSimulatingService] customer가 존재하지 않습니다.");
                return string.Empty;
            }

            ulong prevPrice = customer.CurrentPrice;
            ulong newPrice = customer.Bargain(playerPrice);
            customerPrice = newPrice;
            UpdateDialogueParams(item, customer, currentPrice: newPrice, prevPrice: prevPrice, askPrice: playerPrice);

            string text = string.Empty;
            if (newPrice == 0)
            {
                // 결렬: 인내심 소모로 손님 퇴장
                text = CancelBargain(item.Id, customer, playerPrice, prevPrice);
            }
            else if (customerPrice == playerPrice)
            {
                text = SettleBargain(item, customer, newPrice, playerPrice, prevPrice);
            }
            else if (customer.Patience == 0)
            {
                // 최종 제안
                var finalOffer = customer.Group._finalOffer;
                text = (finalOffer != null && finalOffer.Count > 0)
                    ? finalOffer[UnityEngine.Random.Range(0, finalOffer.Count)].GetFormattedText(_dialogueParams)
                    : string.Empty;
            }
            else
            {
                var negotiation = customer.Group._negotiation;
                text = (negotiation != null && negotiation.Count > 0)
                    ? negotiation[UnityEngine.Random.Range(0, negotiation.Count)].GetFormattedText(_dialogueParams)
                    : string.Empty;
            }

            return text;
        }
        /// <summary>
        /// 거래를 취소함
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="customer"></param>
        /// <param name="askPrice"></param>
        /// <param name="prevPrice"></param>
        public string CancelBargain(uint itemId, CustomerData customer, ulong askPrice = 0, ulong prevPrice = 0)
        {
            if (customer == null)
            {
                Debug.LogError("[BargainSimulatingService] customer가 존재하지 않습니다.");
                return string.Empty;
            }
            if (!_registeredItems.TryGetValue(itemId, out var item))
            {
                Debug.LogError($"[BargainSimulatingService] {itemId}가 존재하지 않습니다.");
                return string.Empty;
            }
            RemoveCustomer(itemId, customer);

            ulong pPrice = prevPrice > 0 ? prevPrice : customer.CurrentPrice;
            UpdateDialogueParams(item, customer, currentPrice: customer.CurrentPrice, prevPrice: pPrice, askPrice: askPrice);

            var rejection = customer.Group._rejection;
            string text = (rejection != null && rejection.Count > 0)
                ? rejection[UnityEngine.Random.Range(0, rejection.Count)].GetFormattedText(_dialogueParams)
                : string.Empty;
            return text;
        }
        public string SettleBargain(IShopItem item, CustomerData customer, ulong finalPrice, ulong askPrice = 0, ulong prevPrice = 0)
        {
            _playerData.AddMoney(finalPrice);
            ulong unitPrice = item.Count > 0 ? (finalPrice / item.Count) : finalPrice;
            _playerData.RecordTradePrice(item.ItemId, unitPrice, item.Value);

            ulong aPrice = askPrice > 0 ? askPrice : finalPrice;
            ulong pPrice = prevPrice > 0 ? prevPrice : customer.CurrentPrice;
            UpdateDialogueParams(item, customer, currentPrice: finalPrice, prevPrice: pPrice, askPrice: aPrice);

            var acception = customer.Group._acception;
            string text = (acception != null && acception.Count > 0)
                ? acception[UnityEngine.Random.Range(0, acception.Count)].GetFormattedText(_dialogueParams)
                : string.Empty;
            _onItemSettledSubject.OnNext(item);
            RemoveItem(item.Id);
            if (item is IDisposable disposableItem) disposableItem.Dispose();
            return text;
        }
        public void Clear()
        {
            foreach (var kvp in _itemCts)
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();
            }
            _itemCts.Clear();
            _registeredItems.Clear();
            _activeGroupRoutines.Clear();

            foreach (var kvp in _bargainCustomers)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    kvp.Value[i].Dispose();
                }
                kvp.Value.Clear();
            }
            _bargainCustomers.Clear();
        }

        public void Dispose()
        {
            Clear();
            _onCustomerArrived.Dispose();
            _onCustomerLeft.Dispose();
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}