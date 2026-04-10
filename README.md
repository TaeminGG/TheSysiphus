# MediaProject

Unity `6000.3.10f1` 기반의 Isometric View 2D 로그라이크 액션 게임 프로젝트입니다.

이 프로젝트는 2D 기반이지만 일부 Entity와 타일 시스템에서 Z값을 사용해 점프, 중력, 높이 차이를 표현하는 2.5D 구조를 사용합니다. 플레이어는 위에서 아래로 내려오는 제한 구역을 피하면서 아래 방향으로 계속 생성되는 지형을 따라 이동하고, 전투와 아이템 성장으로 진행을 이어갑니다. 이 문서는 Codex와 사람이 함께 작업할 때 빠르게 컨텍스트를 맞추기 위한 초안입니다.

## Project Snapshot

- Engine: Unity `6000.3.10f1`
- Render Pipeline: URP
- Input: Unity Input System
- Test Package: Unity Test Framework
- Current scene: `Assets/Scenes/SampleScene.unity`
- Current play structure: single-scene prototype with downward chunk generation

## Core Gameplay

- Isometric View 2D 로그라이크 액션
- Z값을 활용한 2.5D 이동, 점프, 중력, 높이 차이 처리
- 아래 방향으로 계속 생성되는 청크형 맵 진행
- 위에서 내려오는 제한 구역을 피하는 생존 압박
- 플레이어와 적의 공격/피격 중심 전투
- 액티브/패시브 아이템 기반 성장

## Current Goals

- 플레이어 이동, 점프, 중력, 높이 차이 판정을 안정화한다.
- 플레이어 공격, 적 공격, 피격, 체력 흐름을 안정화한다.
- 적 AI와 전투 상호작용을 확장 가능하게 유지한다.
- 아이템과 무기 데이터를 재사용 가능한 구조로 정리한다.
- 청크 기반 레벨 생성과 제한 구역 시스템을 점진적으로 고도화한다.
- UI와 전투 데이터를 느슨하게 연결한다.

## Main Folders

- `Assets/Scripts`
  - 런타임 게임 로직
- `Assets/Scenes`
  - 플레이 가능한 씬
- `Assets/Prefabs`
  - 적, 아이템, 청크, 공격 이펙트 관련 프리팹
- `Assets/Sprites`
  - 스프라이트 에셋
- `Assets/Tiles`
  - 타일 및 타일맵 관련 에셋
- `Assets/Palettes`
  - 타일 팔레트 프리팹
- `Assets/Fonts`
  - 폰트 및 TMP 에셋
- `Assets/PixelArtStudio`
  - 임시 에셋 사용용 리소스, 현재 핵심 개발 구조에서는 우선순위를 낮게 둠
- `ProjectSettings`
  - Unity 프로젝트 설정
- `Packages`
  - 패키지 및 의존성 정의

## Script Overview

- `Assets/Scripts/Entity`
  - 플레이어, 적, 드롭 아이템, 피격 인터페이스 등 엔티티 공통 로직
- `Assets/Scripts/Entity/Player`
  - 플레이어 이동, 높이 판정, 공격 방향, 스탯
- `Assets/Scripts/Entity/Enemies`
  - 적 스탯, 감지, 공격, 이동
- `Assets/Scripts/Weapons`
  - 공격 콤보, 공격 판정, 공격 시각효과, 무기 데이터
- `Assets/Scripts/PlayerItems`
  - 액티브/패시브 아이템 시스템
- `Assets/Scripts/LevelGenerator`
  - 청크 로딩, 레벨 매니저, 맵 생성 관련 로직
- `Assets/Scripts/UI`
  - HP 바, 골드 UI, 포션 UI, 데미지 텍스트 등

## How To Open

1. Unity Hub에서 프로젝트를 연다.
2. Editor version `6000.3.10f1`를 사용한다.
3. 패키지 복원이 끝날 때까지 기다린다.
4. `Assets/Scenes/SampleScene.unity`를 연다.
5. Play Mode에서 이동, 전투, 맵 진행 루프를 확인한다.

## Development Workflow

1. 작은 기능 단위로 브랜치를 만든다.
2. 변경 전 관련 스크립트, 프리팹, 타일맵 연결 지점을 확인한다.
3. 코드 수정 후 Unity Console 에러 여부를 먼저 확인한다.
4. Play Mode에서 이동, 높이 차이, 전투, UI, 맵 생성 흐름을 수동 검증한다.
5. `.meta` 파일 포함 여부를 확인하고 커밋한다.

## Manual Smoke Test

- 프로젝트가 Unity `6000.3.10f1`에서 경고 없이 열린다.
- `SampleScene`가 로드된다.
- 플레이어가 이동 가능하다.
- 플레이어의 점프, 중력, 높이 차이 판정이 의도대로 동작한다.
- 플레이어 공격이 정상 작동한다.
- 적이 플레이어를 감지, 추적, 공격한다.
- 적 피격, 체력 UI, 데미지 텍스트가 정상 표시된다.
- 아이템 드롭 또는 획득 흐름이 깨지지 않는다.
- 아래 방향 맵 진행과 청크 추가가 깨지지 않는다.
- 제한 구역 관련 기능을 수정했다면 이동 속도와 압박 흐름이 의도대로 유지된다.

## Working With Codex

Codex에게 작업을 요청할 때는 아래 정보를 함께 주면 가장 효율적입니다.

- 목표 기능 또는 버그 현상
- 수정 대상 추정 폴더 또는 스크립트
- 기대 동작
- 건드리면 안 되는 시스템
- 수동 테스트 방법
- 완료 기준

예시:

- "플레이어 대시를 추가해줘. `Assets/Scripts/Entity/Player` 중심으로 작업하고, 기존 공격 로직은 유지해줘."
- "적 감지 범위를 시각화해줘. `PlayerDetectionRange` 기반으로 Gizmo를 추가하고 Play Mode 동작은 바꾸지 말아줘."
- "높이 차이 판정을 수정해줘. `PlayerMove`와 `SurroundTileCheck` 중심으로 보고, 기존 공격 시스템은 건드리지 말아줘."
- "청크 생성 간격을 조정해줘. `ChunkLoader`와 레벨 생성 관련 스크립트 중심으로 보고, 제한 구역 로직은 바꾸지 말아줘."

## Next Docs

- 아키텍처 개요: `Docs/Architecture.md`
- 코딩/폴더 규칙: `Docs/Conventions.md`
