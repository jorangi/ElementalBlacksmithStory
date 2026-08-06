# 🔨 Elemental Blacksmith Story (원소 대장장이 이야기)

> **무기 강화를 핵심으로 하는 옛날 감성 강화  프로젝트입니다.**

---

## 📌 목차
1. [프로젝트 소개](#-프로젝트-소개)
2. [주요 기능](#-주요-기능)
3. [기술 스택 및 아키텍처](#-기술-스택-및-아키텍처)
4. [폴더 및 프로젝트 구조](#-폴더-및-프로젝트-구조)
5. [핵심 시스템 메커니즘](#-핵심-시스템-메커니즘)

---

## 📖 프로젝트 소개
`Elemental Blacksmith Story`는 대장간에서 다양한 재료와 무기를 조합 및 강화하며 성장해나가는 게임입니다.
후에 타워 디펜스 요소를 추가할 예정입니다.
객체지향 설계 기법과 모던 유니티 아키텍처 패턴을 적용하여 **유연한 데이터 확장성**과 **디커플링된 이벤트 기반 UI/게임 로직**을 제공합니다.

---

## ✨ 주요 기능
- **🗡️ 무기 강화 및 제작 (Crafting & Enhancement System)**
  - 조합 레시피(`SO_CraftRecipe`) 기반의 무기 강화/제작
  - 강화 성공 확률 판정 및 무기 변화 메커니즘
- **🎨 비동기 에셋 로딩 (Dynamic Asset Loading)**
  - Addressable Asset System을 통한 무기 데이터 및 리소스 동적 로딩
- **🔄 R3기반 반응형 UI**
  - 대장간 모션, 무기 정보, 강화 확률 표기를 실시간 반영

---

## 🛠 기술 스택 및 아키텍처

### 1. 개발 환경
- **Engine**: Unity 6
- **Language**: C#

### 2. 라이브러리 & 프레임워크
- **VContainer**: 의존성 주입(Dependency Injection) 및 객체 생명주기 관리
- **MessagePipe**: Pub/Sub 패턴 기반의 비동기/동기 이벤트 브로커 시스템
- **UniTask**: C# async/await 및 타임라인 컨트롤의 메모리 효율적 비동기 처리
- **R3**: Reactive Extensions 기반 반응형 상태 관리
- **Addressables**: 에셋 번들 및 데이터 동적 로딩 관리

### 3. 디자인 패턴
- **MVP (Model-View-Presenter) 패턴**:
  - `View`: UI 입력 수집 및 렌더링 담당 (예: `AnvilWeaponView`, `EnhanceButtonView`)
  - `Presenter`: View와 Model 간 중계 및 UI 비즈니스 로직 제어 (예: `AnvilWeaponPresenter`, `EnhanceButtonPresenter`)
  - `Model/Service`: 도메인 로직 처리 (예: `EnhancementService`, `ForgeManager`)
- **Event-Driven Architecture**:
  - `MessagePipe`를 통해 모듈 간 직접적인 참조 없이 `EnhanceRequestEvent`, `ChangeWeaponEvent` 등의 이벤트를 주고받아 결합도를 대폭 감소시켰습니다.

---

## 📁 폴더 및 프로젝트 구조

```text
Assets/
└── Scripts/
    ├── Core/                     # 핵심 도메인 서비스 및 DI 생명주기 관리
    │   ├── GameLifetimeScope.cs  # VContainer 린커 및 의존성 주입 정의
    │   ├── EnhancementService.cs # 무기 강화 비즈니스 로직
    │   └── ForgeManager.cs       # 현재 대장간 상태 및 무기 데이터 관리
    ├── Data/                     # 데이터 구조 및 레시피 정의
    │   ├── RecipeKeyHelper.cs
    │   └── Scriptable Object/    # ScriptableObject 기반 데이터베이스
    │       ├── SO_WeaponData.cs
    │       ├── SO_CraftRecipe.cs
    │       └── SO_WeaponDatabase.cs
    ├── Event/                    # MessagePipe 이벤트 메시지 클래스
    │   └── Events.cs (Request/Result/Change Events)
    └── UI/                       # MVP 패턴 기반 UI 컴포넌트
        ├── Presenter/            # UI 비즈니스 로직 Presenter
        └── View/                 # UI View 및 비주얼 제어
```

---

## ⚡ 핵심 시스템 메커니즘

### 이벤트 흐름 (MessagePipe & MVP)
1. **사용자 요청**: 사용자가 강화 버튼 클릭 ➡️ `EnhanceButtonPresenter`가 `EnhanceRequestEvent` 이벤트 발행
2. **로직 처리**: `EnhancementService`가 이벤트를 수신하여 확률 판정 및 UniTask 기반 Addressable 무기 데이터 동적 로드
3. **상태 변경**: 처리 완료 후 `ChangeWeaponEvent` 발행 ➡️ `ForgeManager` 상태 업데이트 및 `AnvilWeaponPresenter`를 통해 UI View 렌더링 갱신

---
<hr>

## 2026-08-03
    readme의 초안은 Antigravity로 작성했다.
    이번 프로젝트는 Addressables와 모바일, 실제 빌드를 중점으로 진행하고자 하는 프로젝트이다.
    때문에 게임 자체는 매우 간단하다. 그냥 검을 강화시키면 된다. 지금은 검 뿐이기는 한데 나중에 검 말고 단검, 창 등등 무기를 추가할 예정이다.
    또한 단순한 강화와는 차별점을 주기 위해 재료를 이용해 재료의 조합에 따라 다른 무기가 되게끔 할 예정이다. 일종의 테크트리.
    `Scriptable Object`로 무기의 정보를 작성하였고 데이터베이스 역할을 주기도 했다.
    다만 무기 정보를 일일이 드래그하는 것은 비효율적이므로 `ContextMenu`를 이용해 자동으로 `AssetDatabase`를 참조해 `t:SO_WeaponData`를 긁어오게끔 했다.
    `MessagePipe`와 `R3`, `VContainer`등 다양한 라이브러리를 써보고 있다. 내장인 `Addressables`도 당연히...
    최근 느끼는 바는 이벤트 중심의 설계에 점점 빠져들고 있는 것 같다. 이게 좋다 나쁘다는 모르겠는데, R3를 위시로 MVP, MVVM 등 이벤트 기반 디자인 패턴들에 익숙해진다는 건 장점?
    `VContainer`도 꽤 익숙해져간다. 아직은 잘 모르는게 많지만 적어도 어떤 식으로 작동하는지, 왜 쓰는지는 얼추 알 것 같다.
    다만 싱글톤 기반이 많이 쓰여 다소 걸린다.


## 2026-08-06
    무기를 몇 개 더 추가했다.
    아직 데이터의 구조를 잡고 있는 중이다.
    무기 속성은 포켓몬 타입처럼 부여만 하고 값은 없다.
    대신에 상태이상이라는 느낌으로 진행하려고 한다.
    상태이상은 타입, 지속시간, 효과를 작성한다.
    
    이미지를 추가하면서 연기나 발광 등 여러 이펙트가 있는 경우 어떻게 할까 고민을 좀 했다.
    여러 시도를 해봤는데, 에셋과 규모의 문제를 고려했을 때 그냥 단일로 이미지 처리를 하는 것이 낫다고 여겼다.
    다만 이 때 화염, 연기, 발광 등 효과가 있는 무기의 경우 외곽선이 지저분하기에 생기지 않도록 처리하는 것이 낫다고 여겼다.
    이를 위해서 처음 고안한 방법은 png의 메타데이터를 이용하여 자동으로 외곽선 여부를 결정 짓도록 하는 것이었고,
    그래서 png metadata injector 프로그램을 WinForm을 이용해서 만들었으나, 스프라이트 아틀라스와 양립이 불가능하다는 것을 알게되었다.
    따라서 그냥 Scriptable Object에 bool 값을 하나 추가하여 외곽선 표시 여부를 처리하게끔 하였다.
    
    유니티 파티클 에셋을 임포트 했다가 지웠는데, 그 잔여물이 남아있는 것 같다.