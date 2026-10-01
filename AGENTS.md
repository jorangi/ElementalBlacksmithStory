# Unity Client Development Guidelines (Low-End Mobile Optimization)

## 1. Environment & Target

- Unity Version: 6000.5.2f1
- C# Language Version: C# 9.0 (Do NOT use C# 10+ features like file-scoped namespaces).
- Target Platform: Ultra Low-End Mobile (Android/iOS).
- Primary Constraint: Zero-Allocation (GC-free) in all gameplay loops and hot paths.

## 2. Core Libraries & Architecture

- DI: VContainer (Prefer constructor injection over Awake/Start dependency resolution).
- Async: UniTask (NEVER use standard C# Tasks or Unity Coroutines).
- Reactive: R3 (Do NOT use UniRx).
- Messaging: MessagePipe (Decouple systems via typed messaging).
- Serialization: MemoryPack (Target data models must use `[MemoryPackable] partial struct/class`).
- State / Logic: Decouple domain logic into pure C# classes/structs. MonoBehaviour is strictly for rendering/input presentation.

## 3. Strict GC & Memory Rules (Critical)

1. NO LINQ:
   - Prohibit all `System.Linq` calls (Where, Select, Any, ToList, FirstOrDefault, etc.).
   - Use standard `for` loops or index-based cached array traversal instead.
2. NO Closures & Anonymous Lambdas in loops:
   - Do not pass local variable-capturing lambdas inside loops or frequently called methods (causes allocation).
   - In R3 subscriptions, ensure closures are avoided or state is explicitly passed.
3. Collection & Struct Allocations:
   - Cache collections and reusable buffers (`List<T>`, arrays) at initialization.
   - Use non-allocating Unity APIs (e.g., `Physics.RaycastNonAlloc`, `Physics2D.OverlapCircleNonAlloc`).
   - Use `readonly struct` / `in` parameters where copy overhead can be avoided.
4. Strings:
   - Avoid `string.Format`, `+` concatenation, or `$"..."` interpolation in Update/Tick.
   - Use `ZString` (if available) or pre-allocated `StringBuilder` / cached string lookups.
5. Boxing Prohibition:
   - Strictly avoid object boxing (e.g., passing primitives/enums to `object`, untyped events, or `Debug.Log` in update loops).
   - Use `struct : IEquatable<T>` for enum/struct comparisons to prevent boxing.

## 4. Coding Conventions

- Encapsulation:
  - Do NOT expose `public` fields.
  - Use `[SerializeField] private` for Inspector references.
  - Use properties (`public int Value { get; private set; }`) for external state read.
- Naming:
  - Private serialized fields: `_camelCase` or `camelCase` (maintain file consistency).
  - Interfaces: `IFoo`.
  - UniTask methods: End with `...Async` and always pass/accept `CancellationToken ct = default`.
- Disposal & Lifecycle:
  - Explicitly dispose R3 `IDisposable` (use `CompositeDisposable` or `.AddTo(destroyCancellationToken)`).
  - Always link UniTask cancellations to `destroyCancellationToken`.
