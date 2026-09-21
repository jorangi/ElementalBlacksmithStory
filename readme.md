# 🔨 Elemental Blacksmith Story (원소 대장장이 이야기)

> **무기 강화를 핵심으로 하는 옛날 감성 강화 프로젝트입니다.**

---

## 📌 목차

1. [프로젝트 소개](#-프로젝트-소개)
2. [주요 기능](#-주요-기능)
3. [기술 스택 및 아키텍처](#-기술-스택-및-아키텍처)
4. [폴더 및 프로젝트 구조](#-폴더-및-프로젝트-구조)
5. [핵심 시스템 메커니즘](#-핵심-시스템-메커니즘)
6. [개발 일지 및 변경 이력](#-개발-일지-및-변경-이력-changelog)

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
    │   ├── GameLifetimeScope.cs  # VContainer 링커 및 의존성 주입 정의
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

---

## 📝 개발 일지 및 변경 이력 (Changelog)

프로젝트의 상세한 일자별 개발 과정, 트러블슈팅, 변경 사항 기록은 [CHANGELOG.md](./CHANGELOG.md)에서 확인하실 수 있습니다.
