# Assets — 자산 표지판

코드는 [Scripts/INDEX.md](Scripts/INDEX.md), 여기는 **씬·프리팹·SO·그림·소리**.
`.unity`/`.prefab`/`.asset` 을 텍스트로 통째 열지 말 것 — Unity MCP 도구로 조회한다.

## 씬 (`Scenes/`)

| 씬 | 무엇 | 맵 |
|---|---|---|
| `Boot.unity` | **여기로 Play 해야 한다.** 영속 싱글턴 전부 + 시작 화면 + 유일한 EventSystem | — |
| `Hub.unity` | 목장(로비). 교배장·시설·다이브 트리거 | **손배치** |
| `Biome.unity` | 초원 | `StageMapGenerator` 런타임 생성 |
| `BiomeMarsh.unity` | 늪지대 | 〃 |
| `BiomeAshfall.unity` | 잿벌(용암) | 〃 |
| `SampleScene.unity` | Unity 기본 잔재, 미사용 | — |

Boot 영속 오브젝트: `GameManager` `PlayerRoster` `BiomeStigmaManager`
`CorruptedGeneTagger` `LineageEvolutionChecker` `AudioManager` `InventoryUI`
`PersistentUICanvas`(`BreedingUIPanel`) `EventSystem`.
**새 씬에 EventSystem 을 추가하지 말 것** — 둘이 되면 입력이 씹힌다.

## 프리팹 (`Prefabs/`, `Resources/Companion.prefab`)

| 프리팹 | 쓰는 곳 |
|---|---|
| `Player.prefab` | 각 씬. `swingVisibleSeconds` 가 공격 애니 fps 의 근거 |
| `WildSlime.prefab` | `SlimeSpawner`, 손배치 |
| `PlacedSlime.prefab` | 목장 시설에 배치된 개체 |
| `Resources/Companion.prefab` | 동행 (`Resources.Load` 경로라 여기 있다) |
| `BreedingPen.prefab` `ExtractionPoint.prefab` `BiomeDiveTrigger.prefab` | 씬 배치. **텔레포트 둘 다 `Props/carriage.png` 마차**(2026-08-09, 예전 추출구는 파란 포털) |
| `UI/InventorySlot.prefab` | 인벤토리·교배·배낭 목록 한 칸 |

## ScriptableObject (`SO/`, `Resources/`)

| 자산 | 읽는 스크립트 |
|---|---|
| `SO/BiomeCatalog.asset` (3바이옴) | `BiomeCatalog` |
| `SO/TraitInheritanceTable.asset` | `BreedingPen` |
| `SO/LineageEvolutionTable.asset` | `LineageEvolutionChecker` |
| `SO/LaborOutputTable.asset` | `RanchFacility` |
| `SO/Species/{slime_basic,bog,guard,lava,rainbow}.asset` | `SlimeSpeciesCatalog` |
| `SO/Stages/{Biome,Marsh,Ashfall}Palette.asset` | `StageMapGenerator` |
| **`Resources/SlimeSpeciesCatalog.asset`** | `SlimeInstance` 계열 — 씬 참조를 못 들어서 Resources 에 둔다 |

## Resources (런타임 `Resources.Load` 대상 — 경로가 곧 코드다)

| 폴더 | 개수 | 내용 |
|---|---|---|
| `Tiles/{hub,biome,marsh,ashfall}/` | 264 | wang 4-corner 바닥 16장씩 + 충돌 타일. **늪지만 이름이 `MarshTile_*`**. `hub/Tile_HubFence.asset` 은 안마당 경계용(그림 있는 Grid 콜라이더) |
| `Props/{hub,biome,marsh,ashfall}_decor/`, `trees/`, `MoreProps/` | 106 | 장식 |
| `Icons/` `UI/` `Fonts/` `Decor/` `Overlay/` | 32 | 아이콘·창 그림·폰트·오염 베일 |

`Assets/Generated/` 는 MCP 가 떨구는 자리이고 **`.gitignore` 대상**이다 —
`AssetDatabase.MoveAsset` 으로 `Resources/` 아래로 옮긴 뒤 커밋한다(GUID 보존).

## 그림 원본과 생성물 (`Art/`, `Animations/`)

```
Art/Characters/**.gif   원본(GIF 은 Unity 가 첫 프레임만 읽는다)
  ↓ python Tools/extract_slime_strips.py
Art/SlimeStrips/*.png (108) + ppu.csv
  ↓ Unity 메뉴  SlimeRanch/Build Actor Animations
Animations/Species/*.controller (5) + 클립 114 + Animations/PlayerAnimator.controller
```

- **두 단계는 항상 세트다.** 파이썬만 돌리면 클립이 삭제된 스프라이트를 가리킨다.
- `Animations/` 는 전부 **생성물**이다. 손으로 고치지 말 것.
  단 `PlayerAnimator.controller` 는 GUID 유지를 위해 **제자리에서 채운다**(지우면 프리팹 배선이 끊긴다).
- `Art/TileSheets/` = 받은 256×256 시트 원본. `python Tools/apply_tile_sheets.py` 가 각 폴더의 16장으로 자른다.
  파일명 `A ↗ B` 에서 **B 가 upper** 다.
- 두 스크립트 다 `--demo` 로 자체 검사(셀 크기·PPU·실루엣 높이)를 돈다. 규칙을 바꿨으면 먼저 이걸 돌린다.

## 소리 (`Sounds/`) — 10개 전부 꽂혀 있다 (2026-08-09 확인)

| 꽂힌 곳 | 클립 |
|---|---|
| `Boot.unity` 의 `AudioManager` | `로비씬`(hubBgm) `바이옴기본`(biomeBgm) |
| `Boot.unity` UI | `UI클릭` `교배완료` |
| `Player.prefab` | `캐릭터이동` `캐릭터공격` |
| `WildSlime.prefab` · `Resources/Companion.prefab` | `슬라임이동` `슬라임사망` |
| `Hub.unity` | `텔레포트` |
| `SO/Species/slime_lava.asset` (`passiveSfx`) | `화염슬라임` |

빈 칸은 `titleBgm` 하나(타이틀 무음). 재생은 전부 `AudioManager.PlaySfx`/`PlayBgm`
경유다 — 개체에 `AudioSource` 를 직접 달면 설정 패널 볼륨 슬라이더를 무시한다.

## 에디터 도구 (`Editor/`) — MCP 로는 못 만드는 자산을 메뉴로 만든다

| 메뉴 | 스크립트 | 비고 |
|---|---|---|
| `SlimeRanch/Generate Actor Animators` | `ActorAnimatorGenerator.cs` | 컨트롤러가 **이미 있으면 건너뛴다**(꽂은 클립 보호) |
| `SlimeRanch/Build Actor Animations` | `SlimeAnimationBuilder.cs` | 스트립 → 클립·컨트롤러, 공격 fps·전이까지 조정 |

`AnimatorController` 생성 API 는 MCP `RunCommand` 로는 못 부른다(대화상자 시도로
죽는다). 그래서 `[MenuItem]` + `Unity_ManageMenuItem Action=Execute` 조합이다.

## 테스트 (`Tests/PlayMode/`)

`CombatTests` `PursuitTests` `BreedingPenTests` `RunSatchelTests`
`SpeciesBiasTests` `StageLayoutTests` `SteeringTests` + 헬퍼(`LogCatcher`
`SceneLoadWait` `SoloPlayerTag`). EditMode 테스트는 없다.

## 기타

`Settings/` = URP·Input System 액션(`InputSystem_Actions.inputactions`)·볼륨 프로파일.
`Fonts/`, `TextMesh Pro/` = 폰트 패키지, 손댈 일 없음.
기준 해상도 960×540(16:9) 고정 창, 캔버스는 1920×1080 기준, 카메라 ortho 5.
