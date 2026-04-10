# Test Plan Draft

이 문서는 자동 테스트를 어떤 순서와 기준으로 추가할지 정리한 초안입니다. 현재 프로젝트는 이동, 높이 판정, 전투, 아이템, 청크 생성, 제한 구역이 서로 강하게 연결되어 있으므로, 변경 영향이 큰 핵심 규칙부터 테스트를 확보하는 방향을 우선합니다.

## Goals

- 자주 깨질 수 있는 핵심 규칙을 먼저 보호한다.
- Play Mode 의존성이 큰 시스템도, 가능한 계산 로직은 EditMode로 분리해 테스트한다.
- Codex가 기능 수정 후 어떤 테스트를 우선 확인해야 하는지 기준을 만든다.

## Recommended Order

1. 높이 차이 및 Z 판정
2. 플레이어 공격/피격 규칙
3. 아이템 획득 및 효과 적용
4. 청크 생성
5. 제한 구역 진행

## EditMode Priority

### 1. Height And Z Rules

우선 테스트 대상:

- 현재 Z값과 목표 타일 높이 차이 비교 규칙
- 점프 가능 여부 계산
- 착지 가능 여부 계산
- 낙하 시작 조건 계산
- 공격 가능 높이 차이 계산

후보 테스트 이름:

- `HeightRule_AllowsMove_WhenHeightDifferenceIsValid`
- `HeightRule_BlocksMove_WhenHeightDifferenceIsTooHigh`
- `HeightRule_TransitionsToFall_WhenGroundIsMissing`
- `AttackHeightRule_HitsTarget_WithinUpperAndLowerRange`
- `AttackHeightRule_IgnoresTarget_OutsideUpperAndLowerRange`

### 2. Combat Rules

우선 테스트 대상:

- 콤보 인덱스 순차 진행
- 공격 속도에 따른 공격 시간 계산
- 공격력 반영 데미지 계산
- 점프 중 공격 불가 규칙
- 낙하 중 피격 가능 규칙

후보 테스트 이름:

- `AttackCombo_AdvancesSequentially_WhenBufferedInputExists`
- `AttackCombo_Resets_WhenInputWindowExpires`
- `AttackTiming_ScalesWithAttackSpeed`
- `PlayerAttack_Disabled_WhileJumping`
- `PlayerDamage_Allowed_WhileFalling`

### 3. Item Rules

우선 테스트 대상:

- 액티브 아이템 등록
- 패시브 아이템 등록
- 패시브 아이템 스탯 증가 반영
- 중복 획득 처리 규칙

후보 테스트 이름:

- `ActiveInventory_AddsItem_WhenPickupSucceeds`
- `PassiveItemList_AddsPassiveItem_WhenPickupSucceeds`
- `PassiveItem_AppliesStatBonus_WhenEquipped`
- `ItemRule_HandlesDuplicatePickup_AsDesigned`

### 4. Chunk Logic

우선 테스트 대상:

- 청크 오프셋 계산
- 아래 방향 청크 생성 좌표 계산
- 청크 인덱스 선택 규칙

후보 테스트 이름:

- `ChunkOffset_CalculatesExpectedPosition_ForCurrentChunkCount`
- `ChunkGeneration_PlacesChunkDownward`
- `ChunkSelection_ReturnsValidChunkIndex`

### 5. Restriction Zone Rules

우선 테스트 대상:

- 기본 이동 속도 계산
- 속도 감소 조건 적용
- 정지 조건 적용
- 확장 규칙 계산

후보 테스트 이름:

- `RestrictionZone_MovesAtBaseSpeed_ByDefault`
- `RestrictionZone_SlowsDown_WhenSlowConditionIsMet`
- `RestrictionZone_Stops_WhenStopConditionIsMet`
- `RestrictionZone_Expands_AsDesigned`

## PlayMode Priority

### 1. Player Movement And Height

우선 테스트 대상:

- 플레이어가 실제 씬에서 이동한다.
- 점프 후 착지한다.
- 높이 차이에 따라 이동 가능/불가능이 나뉜다.
- 낙하 상태 전환이 실제로 발생한다.

후보 테스트 이름:

- `PlayerMove_ChangesPosition_WhenMoveInputIsApplied`
- `PlayerJump_LandsCorrectly_AfterJump`
- `PlayerMove_BlockedByInvalidHeightDifference`
- `PlayerFall_Starts_WhenNoGroundExists`

### 2. Combat Flow

우선 테스트 대상:

- 지상 상태에서 공격 가능
- 점프 중 공격 불가
- 낙하 중 피격 가능
- `AttackHitBox`가 높이 범위에 따라 판정을 다르게 적용

후보 테스트 이름:

- `PlayerAttack_Starts_WhenGrounded`
- `PlayerAttack_DoesNotStart_WhileJumping`
- `PlayerDamage_Applies_WhileFalling`
- `AttackHitBox_HitsOnlyTargets_WithinZRange`

### 3. Enemy Interaction

우선 테스트 대상:

- 적 감지
- 적 추적
- 적 공격
- 플레이어 피격

후보 테스트 이름:

- `Enemy_DetectsPlayer_WhenInRange`
- `Enemy_TracksPlayer_WhenDetected`
- `EnemyAttack_DamagesPlayer_WhenHit`

### 4. Item Pickup Flow

우선 테스트 대상:

- 액티브 아이템 획득
- 패시브 아이템 획득
- 골드 획득
- UI 반영

후보 테스트 이름:

- `PickupItem_AddsActiveItem_ToInventory`
- `PickupItem_AddsPassiveItem_ToInventory`
- `DroppedItem_AddsGold_WhenCollected`
- `ItemPickup_UpdatesUI_AsExpected`

### 5. Chunk And Restriction Zone

우선 테스트 대상:

- 청크가 씬 타일맵에 복사된다.
- 청크 생성 후 플레이가 계속 가능하다.
- 제한 구역이 이동한다.
- 제한 구역 속도 변경이 적용된다.

후보 테스트 이름:

- `ChunkLoader_CopiesTiles_ToSceneTilemap`
- `ChunkGeneration_PreservesPlayablePath`
- `RestrictionZone_MovesDuringPlay`
- `RestrictionZone_SpeedChanges_WhenConditionChanges`

## Initial Implementation Strategy

1. 먼저 EditMode에서 순수 규칙 테스트를 만든다.
2. 테스트하기 어려운 계산 로직은 일반 C# 클래스로 분리한다.
3. 그 다음 PlayMode에서 실제 상호작용 테스트를 최소 개수로 만든다.
4. 기능 추가 시 관련 테스트를 함께 늘린다.

## Suggested First Deliverables

- `Assets/Tests/EditMode`에 높이 판정 규칙 테스트 2개 이상 추가
- `Assets/Tests/EditMode`에 공격 높이 범위 테스트 2개 추가
- `Assets/Tests/PlayMode`에 점프 중 공격 불가 테스트 1개 추가
- `Assets/Tests/PlayMode`에 낙하 중 피격 가능 테스트 1개 추가

## Notes

- 처음부터 모든 시스템을 자동화하려고 하기보다, 자주 바뀌는 규칙과 버그가 치명적인 부분부터 테스트를 추가하는 편이 효율적이다.
- 현재 구조상 MonoBehaviour 의존성이 강한 부분이 많을 수 있으므로, 테스트를 추가하면서 자연스럽게 계산 로직 분리를 진행하는 것을 권장한다.
