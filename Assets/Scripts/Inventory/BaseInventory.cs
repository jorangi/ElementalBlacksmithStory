using System;
using System.Collections.Generic;
using ElementalBlacksmithStory.Data;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Inventory
{
    /// <summary>
    /// 수량 누적(스택) 기반 인벤토리의 공통 추상 클래스
    /// </summary>
    /// <typeparam name="T">보관 대상 데이터 타입 (SO_MaterialData, SO_RuneData 등)</typeparam>
    public abstract class BaseInventory<T> : IInventory<T>, IStartable, IDisposable where T : class, IIdentifiable
    {
        protected readonly Dictionary<T, uint> _inventory = new();
        protected readonly Subject<(T item, uint count)> _onItemCountChangedSubject = new();
        protected readonly CompositeDisposable _disposables = new();

        public Observable<(T item, uint count)> OnItemCountChangedAsObservable => _onItemCountChangedSubject;

        public virtual void Start() { }

        public virtual void Add(T item, uint amount)
        {
            if (item == null)
            {
                Debug.LogError($"[{GetType().Name}] 추가할 아이템이 없습니다.");
                return;
            }

            if (_inventory.ContainsKey(item))
            {
                _inventory[item] += amount;
            }
            else
            {
                _inventory.Add(item, amount);
            }
            _onItemCountChangedSubject.OnNext((item, _inventory[item]));
        }

        public virtual void Remove(T item)
        {
            if (item == null || !_inventory.ContainsKey(item))
            {
                Debug.LogError($"[{GetType().Name}] {(item != null ? item.Id.ToString() : "null")}를 찾을 수 없습니다.");
                return;
            }
            _inventory.Remove(item);
            _onItemCountChangedSubject.OnNext((item, 0));
        }

        /// <summary>
        /// 인벤토리에서 아이템을 지정 수량만큼 차감하여 획득
        /// </summary>
        public virtual (T item, uint amount) Get(T item, uint amount)
        {
            if (item != null && _inventory.TryGetValue(item, out uint count))
            {
                if (amount > count)
                {
                    Debug.LogError($"[{GetType().Name}] {item.Id}가 {amount - count}개 부족합니다.");
                    return (null, 0);
                }
                _inventory[item] -= amount;
                _onItemCountChangedSubject.OnNext((item, _inventory[item]));
                return (item, amount);
            }
            Debug.LogError($"[{GetType().Name}] {(item != null ? item.Id.ToString() : "null")}를 찾을 수 없습니다.");
            return (null, 0);
        }

        public virtual IReadOnlyDictionary<T, uint> GetAll() => _inventory;

        public virtual uint GetCount(T item) => (item != null && _inventory.TryGetValue(item, out uint count)) ? count : 0;

        public virtual void Clear()
        {
            _inventory.Clear();
        }

        public virtual void Dispose()
        {
            _onItemCountChangedSubject.Dispose();
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
