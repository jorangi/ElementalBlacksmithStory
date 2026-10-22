using System.Collections.Generic;
using R3;

namespace ElementalBlacksmithStory.Data
{
    /// <summary>
    /// 인벤토리 기본 인터페이스
    /// </summary>
    public interface IInventory
    {
        void Clear();
    }

    /// <summary>
    /// 단일 아이템 데이터 타입 기반의 인벤토리 제네릭 인터페이스
    /// </summary>
    /// <typeparam name="T">보관 대상 데이터 타입</typeparam>
    public interface IInventory<T> : IInventory where T : class, IIdentifiable
    {
        Observable<(T item, uint count)> OnItemCountChangedAsObservable { get; }

        void Add(T item, uint amount);
        void Remove(T item);
        (T item, uint amount) Get(T item, uint amount);
        uint GetCount(T item);
        IReadOnlyDictionary<T, uint> GetAll();
    }
}