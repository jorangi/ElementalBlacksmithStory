---
name: analyze-draw-calls
description: Analyzes, diagnoses, and optimizes Unity draw calls, batching issues (Static/Dynamic Batching, SRP Batcher, GPU Instancing), 2D Sprite sorting splits, and uGUI canvas batch breaking. Use when the user asks about draw calls, Batches, SetPass calls, Frame Debugger, rendering performance, or why batching failed.
---

# Analyze Draw Calls & Rendering Batching

Unity 렌더링 파이프라인(URP, 2D Renderer, Built-in)에서 발생하는 **드로우콜(Draw Calls / Batches), SetPass Calls, 배칭 실패 원인 분석 및 최적화**를 위한 전문 가이드입니다.

---

## 1. 핵심 지표 이해

| 지표 | 의미 | 최적화 목표 |
| :--- | :--- | :--- |
| **Batches (Draw Calls)** | CPU가 GPU에게 "이 형상을 그려라"고 보내는 명령 횟수 | 가능한 적게 유지 (2D 모바일 기준 50~100 이하 권장) |
| **Saved by Batching** | 배칭(합치기)으로 인해 절약된 드로우콜 수 | 높을수록 좋음 |
| **SetPass Calls** | 셰이더 패스 변경 / 렌더 상태 전환 횟수 (가장 비싼 CPU 작업) | 셰이더/머티리얼 개수를 줄여 낮게 유지 |

---

## 2. 드로우콜이 깨지는 주요 원인 및 해결책 (2D & UI 중심)

### A. 2D Sprite 배칭 실패 원인
1. **스프라이트 아틀라스(Sprite Atlas) 미사용 / 다중 텍스처 사용**
   - 서로 다른 텍스처를 사용하는 스프라이트는 배칭될 수 없습니다.
   - **해결책**: 같은 화면에 나오는 스프라이트들은 `SpriteAtlas`로 묶어 동일 텍스처 페이지를 공유하게 만듭니다.
2. **Sorting Layer / Order in Layer 교차 (Interleaving)**
   - 예: `A-B-A-B` 순서로 렌더링 순서가 꼬이면 A와 B가 번갈아 그려지며 드로우콜이 4번 발생합니다.
   - **해결책**: 같은 아틀라스/머티리얼을 쓰는 오브젝트끼리 Order in Layer를 연속되게 배치 (`A-A-B-B`).
3. **Z축 좌표 불일치 (2D 환경)**
   - 2D Sprite의 Z 좌표가 서로 다르면 투명도 정렬 알고리즘 때문에 배칭이 분리될 수 있습니다.
   - **해결책**: 2D 렌더링 시 Z축을 `0`으로 일치시키고 `Sorting Layer / Order`로만 순서를 제어합니다.
4. **Material 인스턴스화 (`renderer.material`)**
   - 스크립트에서 `.material` 프로퍼티에 접근하면 새로운 복제 머티리얼이 생성되어 배칭이 완전히 깨집니다.
   - **해결책**: 공유 머티리얼(`sharedMaterial`)을 사용하거나 SpriteRenderer의 `color` 속성 사용.

### B. uGUI (Canvas) 드로우콜 폭증 원인
1. **서로 다른 텍스처/폰트가 계층(Hierarchy)에서 교차 배치**
   - 이미지(A) - 텍스트(B) - 이미지(A)가 겹쳐있으면 3 Batches 발생.
   - **해결책**: Hierarchy에서 같은 텍스처를 쓰는 UI 요소를 연속해서 배치하거나, Sub Canvas를 분리.
2. **동적 UI와 정적 UI가 한 Canvas에 공존**
   - 움직이는 UI 요소(타이머, 골드 카운터, 체력바) 하나 때문에 캔버스 전체가 매 프레임 재빌드(Rebatch)됨.
   - **해결책**: 정적 배경 UI 캔버스와 자주 바뀌는 동적 UI 캔버스를 **Sub Canvas(중첩 캔버스)**로 분리.
3. **비활성화 대신 투명화(Alpha = 0) 사용**
   - Alpha가 0이어도 드로우콜과 버텍스 정점 연산은 여전히 일어납니다.
   - **해결책**: `CanvasGroup.alpha = 0` 대신 `gameObject.SetActive(false)` 또는 `Canvas` 컴포넌트 자체를 enable/disable.
4. **불필요한 Raycast Target**
   - 드로우콜뿐 아니라 Canvas 정점 연산 및 레이캐스트 연산량 낭비.
   - **해결책**: 클릭하지 않는 모든 Image/Text의 `Raycast Target` 체크 해제.

### C. URP (Universal Render Pipeline) & 3D 최적화
1. **SRP Batcher 호환성 유지**
   - SRP Batcher는 동일한 셰이더 변형(Shader Variant)을 공유하는 드로우콜의 바인딩 비용을 극적으로 낮춥니다.
   - **주의**: `MaterialPropertyBlock`을 사용하면 SRP Batcher가 깨지므로, URP에서는 머티리얼 속성 블록보다 SRP Batcher 친화적 워크플로우를 유지합니다.
2. **GPU Instancing**
   - 동일한 메시(Mesh)와 머티리얼을 수백 개 이상 렌더링할 때(예: 풀, 탄환 등) 머티리얼 인스펙터의 **Enable GPU Instancing** 활성화.

---

## 3. Unity Frame Debugger 분석 워크플로우

1. **Window > Analysis > Frame Debugger** 열기
2. 게임 실행 중 화면이 멈춘 상태에서 **Enable** 클릭
3. 왼쪽 목록에서 렌더링 패스(`Render2D.Draw`, `RenderForward`, `Draw Transparent Objects` 등) 트리 확인
4. 연속된 드로우콜을 클릭했을 때 우측 상단의 배칭 정보 확인:
   - **"Why this draw call cannot be batched with the previous one:"** 메시지 확인
   - `Different materials`: 머티리얼이 다름
   - `Different textures`: 아틀라스가 다르거나 텍스처 분리
   - `Render state change`: 블렌드 모드, Z-Write, 컬링 모드 차이
   - `Different sprite atlas`: 스프라이트가 서로 다른 아틀라스에 속함

---

## 4. 진단 체크리스트 스크립트 가이드

프로젝트 내 UI나 2D 스프라이트의 드로우콜 낭비를 진단할 때 아래 항목을 점검합니다:
- [ ] 같은 패널 내의 Sprite 이미지들이 단일 Sprite Atlas에 포함되어 있는가?
- [ ] Hierarchy 순서가 아틀라스/텍스처 단위로 군집화(Clustering)되어 있는가?
- [ ] 2D 스프라이트의 Transform.Z 좌표가 모두 0으로 통일되어 있는가?
- [ ] 정적 캔버스와 매 프레임 수치가 바뀌는 동적 캔버스가 분리되어 있는가?
- [ ] 클릭 상호작용이 없는 정적 텍스트/이미지의 Raycast Target이 꺼져 있는가?
