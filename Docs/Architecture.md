# Architecture Draft

이 문서는 현재 프로젝트 코드와 기획 메모를 바탕으로 정리한 아키텍처 초안입니다. 이후 구현이 진행되면 시스템 책임과 데이터 흐름에 맞춰 계속 업데이트합니다.

## Game Summary

- 장르: Isometric View 2D 로그라이크 액션 게임
- 표현 방식: 2D 기반이지만 일부 Entity와 Tile 시스템에서 Z값을 사용해 점프, 중력, 높이 차이를 표현하는 2.5D 구조
- 핵심 진행: 플레이어가 위에서 아래로 내려오는 제한 구역을 피하면서 아래 방향으로 계속 생성되는 지형을 따라 이동
- 전투: 플레이어와 적 모두 공격/피격 시스템을 사용
- 성장: 패시브 아이템과 액티브 아이템으로 플레이어 능력치와 기능을 확장

## Scope Notes

- `Assets/PixelArtStudio`는 임시 에셋 사용을 위한 패키지성 리소스로 간주한다.
- 위 폴더는 현재 게임 구조를 설명하는 핵심 자산으로 다루지 않으며, 향후 개발 문서와 구조 정리에서도 우선순위를 낮게 둔다.

## High-Level Loop

1. 게임은 기본적으로 하나의 플레이 씬에서 진행된다.
2. 플레이어는 아래 방향으로 이동하면서 새로 생성되는 맵 청크를 따라 전진한다.
3. 위쪽에서는 제한 구역이 내려오며 플레이어를 압박한다.
4. 플레이어는 이동, 점프, 높이 차이 활용, 공격, 아이템 획득을 반복한다.
5. 적은 플레이어를 감지하고 이동 방식에 따라 추적 및 공격한다.
6. 전투 결과와 아이템 획득 결과는 플레이어 스탯 및 UI에 반영된다.
7. 맵 생성과 제한 구역 속도 제어가 진행 흐름의 긴장감을 만든다.

## Main Systems

### Player

관련 경로:

- `Assets/Scripts/Entity/Player/PlayerMove.cs`
- `Assets/Scripts/Entity/Player/PlayerAttackController.cs`
- `Assets/Scripts/Entity/Player/PlayerAttackDirection.cs`
- `Assets/Scripts/Entity/Player/PlayerStats.cs`
- `Assets/Scripts/Entity/Player/SurroundTileCheck.cs`

책임:

- 입력을 받아 이동, 방향 전환, 공격 입력 처리를 수행한다.
- 타일의 높이 정보와 Entity의 Z값을 활용해 점프, 중력, 층 이동을 처리한다.
- 플레이어의 체력, 공격력, 공격 속도, 이동 속도 등 핵심 수치를 관리한다.
- 무기, 아이템, 공격 시스템의 영향을 받는 중심 엔티티 역할을 한다.

### Enemy

관련 경로:

- `Assets/Scripts/Entity/Enemies/EnemyStats.cs`
- `Assets/Scripts/Entity/Enemies/PlayerDetectionRange.cs`
- `Assets/Scripts/Entity/Enemies/Movement/*.cs`
- `Assets/Scripts/Entity/Enemies/Attack/*.cs`

책임:

- 플레이어를 감지한다.
- 이동 타입에 따라 플레이어를 추적하거나 위치를 조정한다.
- 공격 타입에 따라 플레이어에게 피해를 준다.
- 높이 차이, 타일 구조, 전투 상태에 맞는 반응을 수행한다.
- 사망 시 드롭 또는 보상 시스템과 연결될 수 있다.

현재 확인된 적 타입:

- Specter
- Undead

### Combat

관련 경로:

- `Assets/Scripts/Weapons/AttackController.cs`
- `Assets/Scripts/Weapons/AttackHitBox.cs`
- `Assets/Scripts/Weapons/AttackVisual.cs`
- `Assets/Scripts/Weapons/WeaponController.cs`
- `Assets/Scripts/Weapons/ScriptableObjects/AttackData.cs`
- `Assets/Scripts/Weapons/ScriptableObjects/WeaponData.cs`
- `Assets/Scripts/Entity/IDamageable.cs`

책임:

- 플레이어 공격 콤보를 관리한다.
- 공격 판정, 공격 속도, 피해량, 시각 효과를 연결한다.
- 피격 가능한 대상에게 일관된 피해 전달 규약을 제공한다.

현재 확인된 구조:

- 플레이어 공격의 중심은 `AttackController.cs`이다.
- 콤보는 `AttackData[]` 배열 순회 방식으로 구성된다.
- 연속 공격 입력 시 배열 내 `AttackData`를 순차 적용한다.
- 공격 속도와 공격력에 따라 실제 공격 속도와 데미지가 달라진다.
- 직접적인 충돌 판정은 `AttackHitBox.cs`에서 처리한다.
- `AttackData`는 `Z Upper Range`, `Z Lower Range`를 통해 공격 시작 Z값 기준 위쪽/아래쪽 어느 높이 차이까지 판정을 적용할지 정의한다.
- 플레이어는 점프 중 공격할 수 없다.
- 플레이어는 낙하 중에도 피격 판정을 받는다.

### Player Items

관련 경로:

- `Assets/Scripts/PlayerItems/Active/*.cs`
- `Assets/Scripts/PlayerItems/Passive/*.cs`
- `Assets/Scripts/PlayerItems/ActiveInventory.cs`
- `Assets/Scripts/PlayerItems/PassiveItemList.cs`
- `Assets/Scripts/Entity/PickupItem.cs`

책임:

- 액티브 아이템과 패시브 아이템을 구분한다.
- 획득 시 플레이어 인벤토리와 스탯 시스템에 효과를 반영한다.
- 플레이어의 능력치 향상 또는 추가 기능 제공을 담당한다.

현재 확인된 흐름:

- 아이템 획득 처리는 `PickupItem.cs`가 담당한다.
- 액티브 아이템은 `ActiveInventory.cs`를 통해 관리된다.
- 패시브 아이템은 `PassiveItemList.cs`를 통해 관리된다.

현재 확인된 예시:

- Active: `GorgonEye`, `TisiphoneTear`
- Passive: `AttackUp`, `ASUp`, `MaxHpUp`, `SpeedUp`

### Level Generation

관련 경로:

- `Assets/Scripts/LevelGenerator/ChunkLoader.cs`
- `Assets/Scripts/LevelGenerator/LevelManager.cs`
- `Assets/Scripts/LevelGenerator/TartarusLevel.cs`

책임:

- 진행 방향에 맞춰 맵을 이어 붙이거나 생성한다.
- 청크 기반 타일 데이터를 실제 플레이 타일맵에 반영한다.
- 플레이어가 계속 아래 방향으로 이동할 수 있는 구조를 제공한다.
- 제한 구역과의 상호작용을 통해 진행 압박을 만든다.

현재 확인된 구조:

- 청크 프리팹의 타일 정보를 `SampleScene`의 `tmap`, `bmap` 타일맵에 복사하는 방식으로 맵을 추가한다.
- 이 동작은 `ChunkLoader.cs`를 중심으로 처리된다.

구현 예정 내용:

1. 아래 방향으로 맵을 계속 생성한다.
2. 위쪽 제한 구역이 아래로 내려오며 점점 넓어진다.
3. 특정 상황에서 제한 구역의 속도를 줄이거나 멈출 수 있어야 한다.

### UI

관련 경로:

- `Assets/Scripts/UI/PlayerHpBar.cs`
- `Assets/Scripts/UI/EnemyHpBar.cs`
- `Assets/Scripts/UI/GoldUI.cs`
- `Assets/Scripts/UI/PotionUI.cs`
- `Assets/Scripts/UI/DamageText.cs`
- `Assets/Scripts/UI/HealEffect.cs`
- `Assets/Scripts/UI/ActiveItemUI.cs`

책임:

- 플레이어와 적의 상태를 시각적으로 표시한다.
- HP, 골드, 포션, 액티브 아이템 상태를 표시한다.
- 전투 결과를 데미지 텍스트와 힐 이펙트로 전달한다.

## Prefab Roles

### Chunks

- 게임 진행 중 맵을 생성하기 위한 프리팹이다.
- 현재는 `SampleScene`의 `tmap`, `bmap` 타일맵에 복사되는 방식으로 사용된다.
- 관련 구현은 `ChunkLoader.cs`를 참고한다.

### Enemies

- 적 캐릭터 프리팹이다.
- 이동, 감지, 공격, 피격, 드롭과 연결된다.

### HitBoxAnim

- 플레이어 및 적의 공격 시 Sprite와 Animation을 표시하기 위한 프리팹이다.
- 공격 판정 자체보다는 시각 표현용 데이터에 가깝다.

### Items

- 액티브 및 패시브 아이템 획득용 프리팹을 포함한다.
- `PickupItem.cs`를 통해 플레이어가 획득한다.
- `DroppedItem.cs` 기반의 Gold 획득 프리팹을 포함한다.
- 특정 액티브 또는 패시브 아이템 효과 적용을 위한 더미 프리팹도 포함한다.

### Probs

- 플레이어나 적의 공격으로 파괴 가능한 구조물 프리팹이다.
- 파괴 시 `DroppedItem`을 생성한다.

## ScriptableObject Usage

현재 ScriptableObject의 주요 사용 대상은 다음과 같다.

1. 패시브 아이템 및 액티브 아이템
2. 공격 판정 및 공격 수치
3. 플레이어가 사용하는 무기 설정

의도:

- 밸런스 데이터와 재사용 가능한 설정값을 코드에서 분리한다.
- 공격/아이템/무기 로직을 데이터 중심으로 확장 가능하게 만든다.

## Data Flow Draft

1. 입력 시스템이 플레이어 이동과 공격 입력을 생성한다.
2. `PlayerMove`와 관련 컴포넌트가 이동, 점프, 높이 차이 반영을 처리한다.
3. 플레이어가 지상 상태일 때 공격 입력이 들어오면 `AttackController`가 현재 무기와 콤보 데이터를 바탕으로 공격을 시작한다.
4. `AttackHitBox`가 실제 충돌 판정과 피해 전달을 처리한다.
5. 이때 공격 판정은 공격 시작 Z값과 `AttackData`의 `Z Upper Range`, `Z Lower Range`를 기준으로 적용 범위를 결정한다.
6. 피격 대상은 `IDamageable` 규약에 따라 체력 감소 및 반응을 수행한다.
7. 플레이어는 낙하 중에도 피격 판정을 받는다.
8. 적 처치나 오브젝트 파괴 결과는 드롭 시스템으로 이어질 수 있다.
9. 아이템 획득 시 `PickupItem`이 액티브 또는 패시브 인벤토리 시스템에 효과를 전달한다.
10. UI는 체력, 골드, 아이템 상태, 데미지 수치를 갱신한다.
11. 맵 생성 시스템은 아래 방향 진행에 맞춰 새 청크를 추가하고 제한 구역과 함께 긴장감을 유지한다.

## Scene Composition Draft

현재 확인된 씬:

- `Assets/Scenes/SampleScene.unity`

현재 상태:

- 아직 테스트용 `SampleScene`만 사용 중이다.
- 앞으로 타이틀 화면 등 추가 씬이 들어갈 예정이다.
- 기본 게임 진행은 한 씬에서 아래로 계속 이동하면서 맵이 생성되는 방식으로 구상 중이다.

설계 메모:

- 자원 관리, 로딩 비용, 오브젝트 누적 문제에 따라 추후 멀티 씬 구조나 구간 분리 구조로 바뀔 수 있다.
- 다만 현재 기준으로는 단일 플레이 씬 + 동적 맵 생성 구조를 기본 가정으로 둔다.

권장 역할 분리:

- Game bootstrap 또는 시스템 초기화 오브젝트
- Player
- Camera
- Level manager / chunk loader
- Restriction zone controller
- UI canvas
- Enemy spawner 또는 배치형 적 프리팹

## Risks To Watch

- 높이 차이, 점프, 중력을 2D 타일맵과 함께 운용하므로 충돌 및 판정 기준이 복잡해질 수 있다.
- 제한 구역과 맵 생성 속도, 플레이어 이동 속도의 밸런스가 핵심 재미를 크게 좌우한다.
- 전투 판정과 시각 효과 프리팹이 분리되어 있으므로 동기화가 어긋나지 않게 관리해야 한다.
- 청크 복사 방식은 단순하고 빠르게 구현 가능하지만, 오브젝트 수와 타일 수가 커지면 성능 또는 메모리 관리 이슈가 생길 수 있다.
- 액티브/패시브 아이템 효과가 누적되면 플레이어 스탯 반영 지점이 분산될 위험이 있다.

## Recommended Next Refinements

- 제한 구역 시스템을 별도 핵심 시스템으로 정의하고, 이동 속도, 확장 규칙, 정지 조건을 명문화한다.
- 단일 씬 구조를 유지할지, 구간 단위 씬 분할 또는 멀티 씬 구조로 전환할지 성능 기준을 세워 판단한다.
- 청크 생성 규칙을 문서화한다. 기준에는 생성 주기, 배치 규칙, 제거 규칙, 난이도 상승 방식이 포함되어야 한다.
- 높이 차이와 Z값 판정 규칙을 정리한다. 점프 가능 조건, 낙하 처리, 공격 가능 높이 차이 같은 전투 연계 규칙이 필요하다.
- 아이템 ScriptableObject 구조를 확정한다. 공통 필드, 즉시 효과, 지속 효과, 더미 프리팹 연결 방식까지 포함해두는 것이 좋다.
- 공격 데이터 구조를 정리한다. 콤보 단계별 애니메이션, 판정 시간, 공격 범위, 넉백, 높이 판정 필요 여부를 한 번에 설명할 수 있어야 한다.
- `SampleScene`에서 실제 플레이 씬으로 넘어갈 시점에 맞춰 씬 역할과 초기화 순서를 별도 문서로 분리한다.
