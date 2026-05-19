# The Sysiphus

Unity `6000.3.10f1` 기반의 Isometric View 2D 로그라이크 액션 게임 프로젝트입니다.

이 프로젝트는 2D 기반이지만 일부 Entity와 타일 시스템에서 Z값을 사용해 점프, 중력, 높이 차이를 표현하는 2.5D 구조를 사용합니다. 플레이어는 위에서 아래로 내려오는 제한 구역을 피하면서 아래 방향으로 계속 생성되는 지형을 따라 이동하고, 전투와 아이템 성장으로 진행을 이어갑니다.

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
