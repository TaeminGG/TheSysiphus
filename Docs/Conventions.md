# Conventions Draft

Codex와 사람이 함께 작업할 때 수정 충돌을 줄이고 유지보수성을 높이기 위한 초안 규칙입니다. 현재 프로젝트의 핵심 전제는 Isometric View 2D 로그라이크 액션, 2.5D 높이 시스템, 단일 플레이 씬 기반 진행, 청크 생성형 맵 구조입니다.

## General Rules

- Unity 버전은 `6000.3.10f1`로 고정한다.
- `.meta` 파일은 에셋과 함께 항상 커밋한다.
- 큰 기능 변경은 기능 브랜치에서 진행한다.
- 한 작업은 가능한 한 하나의 목적만 가지도록 작게 나눈다.
- Unity Console 에러가 있는 상태로 커밋하지 않는다.
- 현재 핵심 게임 구조와 무관한 임시 패키지성 에셋 폴더는 아키텍처 판단 기준에서 분리해서 본다.
- `Assets/PixelArtStudio`는 현재 기준으로 핵심 게임 구조를 설명하는 근거로 사용하지 않는다.

## Folder Rules

### Runtime Code

- 런타임 스크립트는 `Assets/Scripts` 아래에 둔다.
- 주요 도메인 구조는 현재 기준을 최대한 유지한다.

현재 기준 도메인:

- `Entity`
- `Weapons`
- `PlayerItems`
- `LevelGenerator`
- `UI`

### Editor Code

- 에디터 전용 스크립트는 반드시 `Editor` 폴더 아래에 둔다.
- 런타임 어셈블리와 에디터 어셈블리를 섞지 않는다.

### Data Assets

- ScriptableObject는 기능별 하위 폴더에 두거나 별도 데이터 폴더를 만든다.
- 무기, 공격, 아이템 데이터는 폴더명만 보고 구분 가능해야 한다.
- 액티브 아이템, 패시브 아이템, 공격 데이터, 무기 데이터는 서로 분리 가능한 구조를 유지한다.

권장 예시:

- `Assets/Data/Weapons`
- `Assets/Data/Items`
- `Assets/Data/Enemies`

### Scene Rules

- 씬 파일명은 역할이 드러나게 짓는다.
- 테스트용 씬은 접두사 `Test_`를 사용한다.
- 샘플 씬을 계속 사용할 경우 실제 역할에 맞는 이름으로 추후 변경한다.
- 현재 플레이 구조는 단일 씬 기반으로 가정하되, 추후 성능이나 자원 관리 문제로 변경 가능함을 문서에 남긴다.

## Script Design Rules

### MonoBehaviour

- MonoBehaviour는 입력, 연결, 라이프사이클, Unity 이벤트 처리에 집중한다.
- 순수 계산 로직은 가능한 한 일반 C# 클래스로 분리한다.
- 하나의 컴포넌트가 이동, 공격, UI, 데이터 저장까지 모두 맡지 않도록 한다.
- 높이 차이 판정, 점프, 중력처럼 재사용 가능성이 높은 규칙은 가능한 한 한 곳에 모아 일관되게 유지한다.

### ScriptableObject

- 밸런스 값, 무기 데이터, 공격 데이터, 아이템 데이터처럼 재사용 가능한 값은 ScriptableObject를 우선 고려한다.
- 런타임 상태값은 ScriptableObject에 직접 기록하지 않는다.
- 아이템 효과 적용에 더미 프리팹 연결이 필요하다면 데이터와 프리팹 책임 경계를 문서로 남긴다.

### Interfaces And Abstractions

- 서로 다른 대상이 같은 피해 처리 규약을 따를 경우 `IDamageable` 같은 인터페이스를 유지한다.
- 적 이동/공격처럼 타입 확장이 예상되는 시스템은 인터페이스 또는 베이스 클래스를 우선 검토한다.
- 높이 판정, 공격 가능 높이 차이, 타격 가능 조건처럼 여러 시스템이 공유하는 규칙은 하드코딩 분산을 피한다.

## Naming Rules

- 클래스명은 PascalCase를 사용한다.
- 메서드와 프로퍼티는 PascalCase를 사용한다.
- private 필드는 팀 스타일에 맞춰 일관되게 유지한다.
- 폴더 이름은 역할이 드러나게 작성한다.
- 약어는 팀에서 이미 쓰는 경우만 허용하고 새 약어는 가급적 피한다.

## Prefab Rules

- 프리팹은 도메인별로 폴더를 나눈다.
- 프리팹 이름만 보고 타입과 용도를 알 수 있어야 한다.
- 임시 검증용 프리팹은 `Debug` 또는 `Prototype` 폴더에 분리하는 것을 권장한다.

현재 주요 프리팹 그룹:

- `Prefabs/Chunks`
- `Prefabs/Enemies`
- `Prefabs/HitBoxAnim`
- `Prefabs/Items`
- `Prefabs/Probs`

역할 기준:

- `Chunks`는 플레이 타일맵에 복사되는 맵 조각이다.
- `Enemies`는 적 본체 프리팹이다.
- `HitBoxAnim`은 공격 시각 표현용 프리팹이다.
- `Items`는 아이템 및 드롭 획득 프리팹이다.
- `Probs`는 파괴 가능한 구조물 프리팹이다.

## Scene Reference Rules

- 다른 씬 오브젝트에 대한 직접 참조는 꼭 필요한 경우에만 사용한다.
- 가능한 경우 SerializeField로 연결 지점을 명시한다.
- 런타임에 반드시 필요한 참조는 `Awake` 또는 `Start` 시점에 검증 로그를 남기는 것을 권장한다.
- 타일맵 이름이나 씬 오브젝트 이름에 의존하는 연결은 초기 프로토타입 단계에서는 허용되지만, 구조가 커지면 명시적 참조 방식으로 전환을 검토한다.

## Combat And Height Rules

- 공격 판정은 가능하면 `AttackController`와 `AttackHitBox` 중심 흐름을 유지한다.
- 콤보 공격은 `AttackData[]` 기반 순차 적용 구조를 유지하되, 데이터 역할과 실행 책임을 혼동하지 않는다.
- `AttackHitBox`가 사용하는 `AttackData`에는 `Z Upper Range`와 `Z Lower Range`가 포함되며, 이는 공격이 시작된 Z값을 기준으로 위쪽/아래쪽 어느 범위의 상대까지 공격 판정을 적용할지 의미한다.
- 플레이어는 점프 중 공격할 수 없다.
- 플레이어는 낙하 중에도 피격 판정을 받는다.
- 공격 시각 효과와 실제 판정 타이밍이 어긋나지 않도록 검증한다.

## Level Generation Rules

- 현재 맵 생성은 아래 방향 진행을 전제로 한다.
- 청크는 실제 플레이 타일맵에 복사되는 구조이므로 생성과 제거 기준을 함께 고려한다.
- 제한 구역은 별도 핵심 시스템으로 취급하고 속도, 범위, 정지 조건을 한 곳에서 관리하는 것을 권장한다.
- 레벨 생성 변경 시 플레이어 이동 속도, 적 배치, 제한 구역 압박을 함께 검증한다.

## Testing Rules

- 핵심 게임 규칙은 EditMode 테스트로 분리 가능한지 먼저 검토한다.
- 전투, 상호작용, 씬 연결은 PlayMode 테스트 또는 수동 체크리스트로 검증한다.
- 버그 수정 시 가능하면 재현 케이스를 테스트나 문서로 남긴다.
- 높이 차이 판정, 청크 생성, 제한 구역 속도 조절은 가능하면 별도 검증 항목으로 관리한다.

권장 초기 구조:

- `Assets/Tests/EditMode`
- `Assets/Tests/PlayMode`

## Git Rules

- `Assets`, `Packages`, `ProjectSettings` 변경은 의도를 알고 커밋한다.
- `Library`, `Temp`, `Logs`, `obj`, `UserSettings`는 커밋하지 않는다.
- 에셋 이동이나 이름 변경 시 관련 `.meta` 유지 여부를 확인한다.
- 대용량 원본 리소스는 Git LFS 적용 여부를 팀 기준으로 정한다.

## Codex Request Rules

Codex에 요청할 때 아래 형식을 권장한다.

- 목표:
  - 무엇을 바꾸고 싶은지
- 범위:
  - 어느 폴더 또는 파일 중심인지
- 제약:
  - 건드리면 안 되는 부분
- 검증:
  - 어떻게 확인할지

좋은 예시:

- "플레이어 공격 선입력을 추가해줘. `Assets/Scripts/Entity/Player`와 `Assets/Scripts/Weapons` 중심으로 보고, 기존 무기 데이터 구조는 유지해줘."
- "언데드 적 이동이 벽에 끼는 문제를 고쳐줘. `UndeadMovement`와 충돌 처리 위주로 보고, 다른 적 AI는 바꾸지 말아줘."
- "점프 후 착지 판정을 수정해줘. `PlayerMove`, `SurroundTileCheck` 중심으로 보고 기존 아이템 시스템은 건드리지 말아줘."
- "제한 구역 속도 감소 기능을 추가해줘. 레벨 생성 및 진행 제어 관련 스크립트 중심으로 보고 전투 시스템은 바꾸지 말아줘."

## Documentation Rules

- 새 시스템을 도입하면 최소한 목적, 진입점, 주요 데이터 흐름을 문서에 남긴다.
- 네이밍 또는 폴더 규칙을 바꾸면 `Docs/Conventions.md`를 함께 갱신한다.
- 큰 구조 변경 후에는 `Docs/Architecture.md`를 업데이트한다.
