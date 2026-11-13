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

## 5. Asset Generation (AI Workflow)

- Follow the [game image asset guide](.agents/rules/image-asset-generation.md) for AI-generated artwork, including emblems, UI artwork, monsters, characters, and backgrounds.

### Weapon sprites and icons

- Follow the [weapon asset generation rules](.agents/rules/weapon-asset-generation.md) for all AI weapon image generations.
- **Cost Rule**: Always generate 3~5 weapons per 16:9 sheet (target: 5 weapons) to minimize per-asset generation cost (~20 KRW per weapon).
- **Framing**: Upright vertical alignment on pure white background with generous safe margins to prevent cropping.
- **Post-processing**: Automate background removal, slicing, and 45-degree rotation via Python scripts.

## 6. Planning, Changelog & Reporting Workflow

### 6.1 Plan First (CHANGELOG.md)

- `CHANGELOG.md` lives in the project root. Write entries in Korean, matching the existing entries.
- The changelog is a draft and work record for the user's pre-commit review. The user reviews and edits it before committing, so write it concisely and honestly.
- Existing entries written by the user are reference for tone and format only. Do not copy their level of detail (they may contain concrete values or identifiers because they are written after the fact).
- When the user assigns a task, write a brief plan in `CHANGELOG.md` BEFORE touching any source code. Do not start coding until the plan entry exists.
- Entry format:
  - Use a date header (`## YYYY-MM-DD`). Add the entry under today's header, and create the header at the top if it doesn't exist yet. The newest date always stays on top.
  - Use a short bullet list. Sub-bullets are allowed for details.
  - Start each new bullet as an unchecked checkbox (`- [ ]`).
- Content rules:
  - For each source file to be changed, state in one short natural-language sentence what will be done and, when relevant, the key technique and why.
  - Example: "In the HUD presenter, subscribe to the money stream and update the label. Use a static lambda that receives `this` as state, so no closure is allocated on the heap."
  - Keep it brief. It is a plan, not a design document.
  - Do NOT write concrete identifiers in the plan: no variable, field, type, method, or generic signature names (e.g. avoid `IObservable<Unit>` or `float _minMoney`). Describe them in plain language ("the money value", "the cooldown timer").
  - File names may be mentioned.
  - Only plan what was actually requested. Do not add unrelated work.

### 6.2 Completion Marking

- When the work is done, change the matching checkboxes to checked (`- [x]`).
- Do not rewrite the plan text itself. Only the check state changes, unless the result differs from the plan (see 6.3).

### 6.3 Changing the Plan Mid-Task

- If the plan must change during implementation, update `CHANGELOG.md` FIRST, then continue coding.
- NEVER edit, delete, or reword existing changelog entries (past dates or the current plan). Only append.
- Add the change as a clearly marked temporary entry directly under the related plan item, for example:
  - `  - [임시 체인지로그] 변경 사유와 새 방식을 한두 문장으로 기재`
- The user reviews the changelog before committing and will merge or remove temporary entries manually.
- Temporary entries follow the same rules as 6.1 (brief, no concrete identifiers).

### 6.4 Final Report (Mandatory, Every Task)

After finishing, report in the reply how the work was actually done. The report must include:

- Which files were changed and what was implemented in each.
- The technique used and why, especially anything related to GC, allocation, or lifecycle rules in this document.
- Any shortcut, workaround, assumption, or limitation. If something is incomplete or only partially handled, say so explicitly. Never present it as complete.
- Anything that differs from the plan in `CHANGELOG.md`.
- What general problem the solution addresses, which other cases it covers, and which cases it does not.

### 6.5 Solve the General Problem, Not Just the Current Case

Do not write code that only makes the current symptom or example go away. Solve the underlying problem with a design that keeps working for other inputs and cases.

- Before implementing, identify what general problem the user's example represents, and design for that.
- Do NOT special-case the specific example, value, name, or input the user mentioned. If the code would stop working when the example changes, it is wrong.
- Do NOT use hardcoded keyword lists, magic values, or per-case `if` branches to fake detection or branching.
  - Bad: when asked to detect that a delivery app was launched, checking whether the text contains a few hand-picked app names.
  - Good: a data-driven or rule-driven mechanism (such as a data table, a configurable rule, or a shared abstraction) that covers the whole category and where new cases are added as data, not as new code branches.
- Do NOT default to one technique for everything. Regex, string patching, and similar quick tools are acceptable only when they are the right fit for the actual problem (for example, parsing a truly regular text format). Never use them as a way to patch around a design problem or to avoid a structural change.
- Prefer fixing the root cause at the right layer over adding a workaround on top of the symptom.
- Follow the architecture in this document (pure C# domain logic, MonoBehaviour for presentation, messaging for decoupling) instead of inserting one-off logic wherever it is quickest.
- If a general solution is not feasible within the request, stop and explain why in the final report instead of silently shipping a narrow hack. If a temporary workaround is unavoidable, mark it clearly in the code and state it in the report.
- In the final report (6.4), state explicitly what general problem the solution addresses and which other cases it covers. If it only covers some cases, list which ones are not covered.
