# 주말 작업 분담 (토·일, 2명)

> 확정 사항: **레벨링 버림**(defense + 종편향만) · **빌드 대상 WebGL** ·
> **스테이지 구조 맵 신설**(현재 단일 맵 → 다층 스테이지) + 타일맵 랜덤 스포너.

---

## 0. 실측 근거 — 왜 이렇게 나누는가

| 사실 | 수치 | 출처 |
|---|---|---|
| 맵이 좁다 | 바이옴 타일맵 **24×16 타일 = 24×16 유닛** | `Biome.unity` `m_Size` |
| 화면 크기 | ortho 5 → **17.8 × 10 유닛** | 카메라 |
| → 맵 전체가 **가로 1.4화면 / 세로 1.6화면** | 탐험할 게 없는 게 당연 | 계산 |
| 슬라임 스폰 | 씬에 **손배치 2마리 고정**, 스포너 스크립트 0개 | `Instantiate` 호출처 전부 UI/교배장 |
| 진영 개념 | `IDamageable.ApplyDamage(양, 출처)` — **출처를 아무도 안 읽는다** | 동행 넣으면 아군 피격 |
| 스탯 주입 | `WildSlimeAgent.Awake()` 가 스탯을 **자가 생성**(HP 20 하드코딩) | 스포너·동행 둘 다 여기 막힘 |
| 사운드 | `AudioManager` **이미 완성**(BGM/SFX 2소스 + PlayerPrefs) — 클립만 0개 | 코딩 불필요 |

---

## 1. 토요일 오전 — 공동 선행 (브랜치 나누기 **전**, `dev` 직접)

둘 다 여기서 막힌다. 같이 앉아서 끝내고 갈라선다. 목표 90분.

| # | 작업 | 파일 | 없으면 생기는 일 |
|---|---|---|---|
| P0-1 | `IDamageable` 에 진영 추가 | `IDamageable.cs`(7줄) `PlayerMeleeAttack` `WildSlimeAgent` `PlayerHealth` | 동행이 플레이어를 때린다 |
| P0-2 | `WildSlimeAgent.Initialize(SlimeInstance, tier)` 주입 경로 | `WildSlimeAgent.Awake()` | 스포너·동행이 **같은 곳을 각자** 고쳐 충돌 |
| P0-3 | `GameEventId` 에 주말에 쓸 값 전부 미리 추가 | `GameEvent.cs`(26줄) | enum 한 줄 추가마다 충돌 |
| P0-4 | **WebGL 파일 IO 가드** | `RunLogWriter` `SaveSystem` | 아래 §5 |

커밋 4개 → `dev` push → 각자 브랜치.

---

## 2. 브랜치 배정

| | **A — `feat-stage`** | **B — `feat-companion`** |
|---|---|---|
| 맡는 것 | 스테이지 맵 생성 + 랜덤 스포너 + 스테이지 이동 | 동행 시스템 + 전투 수치 + WebGL 빌드 |
| 소유 씬 | `Hub` `Biome` `BiomeMarsh` `BiomeAshfall` | `Boot` **단독** |
| 소유 프리팹 | `BiomeDiveTrigger` `ExtractionPoint` `BreedingPen` | `Player` `WildSlime` `PlacedSlime` `UI/InventorySlot` |
| 소유 핵심 파일 | `GameManager` `BiomeCatalog` `EventBus` | `SlimeInstance` `SlimeStatBlock` `PlayerRoster` `SaveSystem` `MutantRollService` `BreedingPen` |
| 금지 | `SlimeInstance` `PlayerRoster` `Player.prefab` `Boot.unity` | `GameManager` `BiomeCatalog` `Hub/Biome*.unity` |

**씬 소유가 1순위 근거다.** `.unity` 는 줄 단위로만 병합돼 오브젝트가 조용히 통째로 사라진 사고 있음
(`f6999ef`, `InventoryUI` Canvas 4개 소실, diff 에 삭제선 없이).

---

## 3. A — 스테이지 맵 + 스포너

### 3-1. 방식 결정: **런타임 타일 생성**. 씬을 늘리지 않는다

| 후보 | 판정 |
|---|---|
| 스테이지마다 `.unity` 새로 만들기 | ❌ 3바이옴 × N스테이지 = 씬 폭발. 병합 사고 위험 최대 |
| 방 프리팹 조립 | 🔺 프리팹 손작업 필요. 시간 남으면 |
| **기존 타일맵에 런타임 생성** | ✅ **채택.** 씬 편집 거의 0, 병합 충돌 0, 스포너와 데이터 공유 |

기존 `Ground`/`Walls` 타일맵을 그대로 쓰고 손배치 타일만 지운다.
생성기가 `SetTile` 로 방·복도를 그리면 `CompositeCollider2D` 는 자동 갱신된다.
**바닥은 wang 인덱스 0 하나면 충분** — 16장 블렌드는 나중 에셋 작업 때.

### 3-2. 신규 파일

| 파일 | 하는 일 | 대략 |
|---|---|---|
| `Scripts/World/StageMapGenerator.cs` | `Generate(seed, depth)` — 방 N개 + 복도, `Ground`/`Walls` 에 SetTile. **방 목록을 public 으로 노출**(스포너·게이트가 쓴다) | ~150줄 |
| `Scripts/World/SlimeSpawner.cs` | 생성기의 방 목록 받아 **바닥 타일 위 랜덤 좌표**에 스폰. 방당 마리수·동시 상한·티어 반영. `Initialize()` 로 스탯 주입 | ~80줄 |
| `Scripts/World/StageGate.cs` | 밟으면 depth+1 → 생성기 재실행 + 플레이어 위치 리셋 | ~40줄 |

### 3-3. 고칠 파일

| 파일 | 변경 |
|---|---|
| `GameManager` | `CurrentStageDepth` 추가. **스테이지 이동에 `LoadScene` 쓰지 않는다** — 재생성만. 씬 로드는 바이옴 진입/추출/사망 때만 |
| `BiomeDiveTrigger` + Hub | 바이옴 3종 선택 UI (지금 진입점 1개뿐) |
| Biome ×3 씬 | 손배치 `WildSlime` 2마리 삭제, 생성기·스포너 오브젝트 추가 |

### 3-4. 스테이지 규칙 (기본안 — 실측 후 조정)

| 항목 | 값 |
|---|---|
| 방 개수 | depth 1 = 4방, depth 당 +1, 상한 8 |
| 방 크기 | 12×10 ~ 20×16 타일 |
| 슬라임 | 방당 1~3마리, 동시 상한 12 |
| 난이도 | 낙인 티어에 `depth` 를 더해 스탯 배율(`1 + 0.15×tier`) |
| 진행 | `StageGate` = 더 깊이 / `ExtractionPoint` = 언제든 탈출 |

깊이 = 위험, 탈출 = 보상 확정. 배낭이 이미 "죽으면 몰수"라 이 축과 그대로 맞는다.

### 3-5. 함정

| 함정 | 대응 |
|---|---|
| `ExtractionPoint` 가 벽 밖이면 **도달 불가** (실제 사고, 세 씬 전부) | 생성기가 **방 안에 배치**하게. 손좌표 금지 |
| `LoadScene` 은 같은 프레임에 안 끝남 | 스테이지 이동은 씬 로드 안 씀 → 회피됨 |
| 늪지대만 타일 이름이 `MarshTile_*` | 생성기가 타일 에셋을 **인스펙터 참조**로 받게. 이름 하드코딩 금지 |
| 타일 PPU | 셀 64 = PPU 64 = 1타일 1유닛. 이 전제 깨면 지도가 2×2 로 깨진다 |

---

## 4. B — 동행 + 전투 수치

### 4-1. 동행 (확정 사양)

| 항목 | 값 |
|---|---|
| 상한 | **1마리** |
| 사망 | **로스터 영구 제거** (배낭 몰수와 같은 축) |
| 선택 UI | 기존 인벤토리 패널(I)에 버튼 추가. **새 화면 없음** |
| 체력바 | 화면 고정 HUD (영구 제거라 빼는 판단이 필요) |

**`WildSlimeAgent` 재사용 불가** — 막는 것 5개: 진영 없음 / 스탯 자가생성 / 0HP 가 죽음이 아니라 약화 플래그 /
추적 대상 태그 고정 / 때릴 대상과 따라갈 대상이 갈림. 공유되는 건 "추격 이동" 한 조각뿐.
**상속 아닌 별도 파일.**

| 신규 | `Scripts/World/CompanionAgent.cs` · `Scripts/UI/CompanionHudBar.cs` |
|---|---|
| 수정 | `InventoryUI`(동행 버튼) `PlayerRoster`(영구 제거) `BreedingPenInteractor`·`RanchFacilityInteractor`(동행 중 배치 불가) |

> **HUD 는 Boot 씬 `PersistentUICanvas` 에 얹는다.** 바이옴 3씬에 두면 A 소유 씬을 건드려 충돌한다.
> 주의: `PersistentUICanvas` 만 아직 800×600 기준(나머지 1920×1080) — 크기 어긋나 보이면 여기.

> `PlayerRoster.RosterChanged` 구독은 **`OnEnable` 아니라 `Start`.**
> Boot 영속끼리 `Awake` 순서 미보장 → 구독이 조용히 스킵된다(실제 버그 이력).

### 4-2. 전투 수치 (레벨링 대신 이 2개만)

| # | 결함 | 고칠 곳 |
|---|---|---|
| 1 | **`defense` 가 데미지 계산에서 안 읽힌다** — 방어형 슬라임 방어 2.2배가 체감 0 | `PlayerMeleeAttack` `WildSlimeAgent` `SlimeStatBlock` |
| 2 | **교배 자손에 종 편향이 안 걸린다** — `ApplyBias` 가 야생 스폰에만 있어, 종은 용암인데 스탯은 파란 슬라임 | `BreedingPen.Breed` |

가차 재미가 프로젝트 중심이므로 이 둘이 곧 체감이다. `SlimeInstance` 스키마는 **안 건드린다** → 세이브 호환 문제 회피.

---

## 5. WebGL 빌드 — 토요일에 미리 손대야 하는 것

일요일 저녁 첫 빌드는 자살이다. **토요일 밤에 한 번 돌려 놓는다.**

| 위험 | 지금 상태 | 대응 | 언제 |
|---|---|---|---|
| 🔴 파일 IO | `RunLogWriter` 가 **이벤트마다** `File.AppendAllText`, `SaveSystem` 도 직접 IO | `#if UNITY_WEBGL && !UNITY_EDITOR` 로 로그 끄고 세이브는 `PlayerPrefs` 폴백 | **P0-4, 토 오전** |
| 🔴 첫 빌드 시간 | IL2CPP, 20~40분 | 토요일 밤 1회 선행 빌드 | 토 밤 |
| 🟡 압축 헤더 | GitHub Pages 등 정적 호스팅은 Brotli 헤더를 못 준다 | Player Settings → **Decompression Fallback 켜기** | 일 |
| 🟡 스레드 | WebGL 은 스레드 없음 | `System.Threading` 사용처 0인지 확인 | 일 |
| 🟡 Resources 이중 포함 | 원본 타일 시트가 `Resources` 에 남으면 빌드에 2번 | `Assets/Art/TileSheets/` 로 이동 확인 | 일 |
| 🟡 `Assets/Generated/` | `.gitignore` 대상 | `Assets/Resources/{Icons,Tiles,Decor,UI}/` 로 `MoveAsset` 후 커밋 | 상시 |
| 🟡 씬 순서 | Boot 가 0번이어야 함 | 안 거치면 `GameManager.Instance` null | 일 |
| 🟢 입력 | 새 Input System, WebGL 키보드 OK | 확인만 | — |

---

## 6. 추가 작업 (사람 작업, 코딩 거의 없음)

| 작업 | 담당 | 배선 위치 | 코딩 |
|---|---|---|---|
| BGM·효과음 | B | `AudioManager`(Boot) 배경음 3칸 + `ActorAnimation` 인스펙터 5칸 | ❌ **이미 완성** |
| 슬라임 아이콘 | B | `SlimeSpecies.defaultSprite` (종 5개 SO) | ❌ |
| 도움말 단락 | B | `SettingsPanel` 의 `ControlsText`/`DescriptionText` (칸 이미 있음) | ❌ |
| UI 에셋 | 나중 | — | — |

효과음은 애니메이션 이벤트가 아니라 **스크립트가 낸다** — `ActorAnimation` 칸이 전부 `AudioManager.PlaySfx` 를 거친다.
여기서 `AudioSource` 를 직접 두면 그 개체만 볼륨 슬라이더를 무시한다.

에셋 생성 시 `mcp__asset__generate_*` 에 `gameId="slime-rancher-roguelite"` **필수** — 안 주면 `default` 팔레트로 샌다.

---

## 7. 일정

| 시간 | A (`feat-stage`) | B (`feat-companion`) |
|---|---|---|
| 토 오전 | 🤝 **P0-1~4 공동, `dev` 직접** | 🤝 동일 |
| 토 오후 | `StageMapGenerator` 방·복도 생성 | `CompanionAgent` 이동·공격 |
| 토 저녁 | `SlimeSpawner` 랜덤 스폰 | 동행 HUD + 인벤토리 버튼 |
| 토 밤 | ⚠️ **`dev` 머지 1회** | ⚠️ **머지 후 WebGL 선행 빌드 1회** |
| 일 오전 | `StageGate` + depth 난이도 | `defense` 반영 + 종 편향 |
| 일 오후 | 바이옴 선택 UI (Hub) | 사운드·아이콘·도움말 꽂기 |
| 일 저녁 | 🤝 **최종 머지 → WebGL 빌드 → 오류 해결** | 🤝 동일 |

**B 가 일요일 오전에 먼저 끝나면 A 의 방 배치 규칙·밸런싱 수치를 나눠 받는다** (코드가 아니라 인스펙터 값으로).

---

## 8. 머지 규칙

| 규칙 | 이유 |
|---|---|
| 상대 소유 파일은 **요청 → 소유자가 `dev` 에 단독 커밋 → 둘 다 rebase** | 인바운드 상위 파일은 양쪽에서 고치면 충돌 확정 |
| 씬 파일은 소유자 외 **열지도 마라** | Unity 로 열기만 해도 `.unity` 가 더러워진다 |
| 머지는 하루 1회 이상 | 충돌을 하루치로 제한 |

머지 후 오브젝트 총수 확인:

```bash
grep -c "^--- !u!" Assets/Scenes/Hub.unity Assets/Scenes/Biome.unity Assets/Scenes/BiomeMarsh.unity Assets/Scenes/BiomeAshfall.unity Assets/Scenes/Boot.unity
```

기준선: **Hub 67 / Biome 108 / BiomeMarsh 393 / BiomeAshfall 114**.
머지 후 이 숫자가 줄면 오브젝트가 먹힌 것이다. (A 가 손배치 슬라임을 지우면 Biome 계열은 정당하게 줄어든다 — 그 감소분만큼인지 확인)

---

## 9. 타협 순서 (시간 없을 때 버리는 순서)

| 순위 | 버릴 것 | 남길 이유 / 버려도 되는 이유 |
|---|---|---|
| 1 | 바이옴 선택 UI | 지금처럼 진입점 1개로도 게임은 돈다 |
| 2 | `StageGate` 깊이 난이도 | 스테이지 생성만 돼도 "맵이 넓어짐"은 달성 |
| 3 | 종 편향 수정 | 눈에 안 보이는 결함 |
| 4 | 동행 HUD 체력바 | 동행 자체는 남기고 체력은 콘솔 로그로 |
| — | **끝까지 남길 것** | 스테이지 생성 + 랜덤 스포너 + `defense` + **WebGL 빌드 성공** |

---

## 부록 A. 스크립트 연관성 지도

`Assets/{Scripts,Editor,Tests}` 63개 `.cs` 전수 식별자 스캔 결과. 이 배정의 근거다.

### A-1. 공유 척추 — 인바운드 상위 (둘 다 건드리면 충돌 확정)

| 파일 | 인바운드 | 주말 소유자 |
|---|---|---|
| `SlimeInstance` | **20** | B |
| `PlayerRoster` | 11 | B |
| `SlimeStatBlock` | 10 | B |
| `GameManager` | 10 | **A** |
| `GameEvent` / `GameEventId` | 8 / 8 | 공동(P0-3 에서 선반영) |
| `EventBus` | 7 | A |
| `RunLogWriter` | 7 | B (WebGL 가드) |
| `BiomeStigmaManager` | 6 | 읽기만 |
| `SaveSystem` | 3 | B (WebGL 가드) |

**척추가 A/B 로 깨끗하게 갈린다** — A 는 `GameManager`+`BiomeCatalog`(씬·이동 축),
B 는 `SlimeInstance`+`PlayerRoster`+`SaveSystem`(개체·저장 축). 서로 안 겹친다.

### A-2. 클러스터 3덩이

| 덩이 | 스크립트 | 주말 담당 |
|---|---|---|
| **월드/다이브** | `WildSlimeAgent`(283줄, 최대 허브) `ExtractionPoint` `ExtractionDirectionArrow` `BiomeDiveTrigger` `BiomeCatalog` `CameraFollow` `CorruptionOverlay` `FogOfWarReveal` | A |
| **전투/포획** | `PlayerMovement` `PlayerMeleeAttack` `PlayerHealth` `IDamageable` `DamageFlash` `HealthBar` `ActorAnimation` `CaptureTool` `CapturePromptUI` `RunSatchel` `SatchelCounterUI` `SatchelSlotView` | B |
| **목장/교배/로스터** | `BreedingPen` `BreedingPenInteractor` `BreedingUIPanel` `RosterSlotButton` `EggSlotView` `EggIncubator` `SlimeEgg` `TraitInheritanceTable` `InventoryUI` `InventorySlotView` `RanchFacility` `RanchFacilityInteractor` `LaborOutputTable` `CorruptedGeneTagger` `LineageEvolutionChecker` | B (이번 주말은 `BreedingPen`·`InventoryUI` 만) |
| **종/외형** (양쪽에 걸침) | `SlimeSpecies` `SlimeSpeciesCatalog` `SlimeAppearance` `MutantRollService` + `Editor/SlimeAnimationBuilder` `Tools/*.py` | **B 소유.** A 의 스포너는 호출만 |

### A-3. A↔B 를 넘는 간선 — 3개뿐

| 간선 | 처리 |
|---|---|
| `SlimeSpawner`(A) → `WildSlimeAgent.Initialize()`(B) | **P0-2 에서 시그니처 고정.** 이후 A 는 호출만 |
| `SlimeSpawner`(A) → `SlimeSpeciesCatalog`·`MutantRollService`(B) | 읽기·호출만. B 가 시그니처 안 바꿈 |
| `CompanionAgent`(B) → `GameManager.CurrentState`(A) | 읽기만 |

이벤트 경계(`SlimeCaptured` 발행 = `CaptureTool`, 구독 = `CorruptedGeneTagger`)는
**파일이 안 겹친다** — 이미 좋은 seam. `GameEventId` 값 추가만 P0-3 에서 미리 끝낸다.

### A-4. 씬 · 프리팹 배치 (GUID 실측)

| 프리팹 | Hub | Biome | BiomeMarsh | BiomeAshfall |
|---|---|---|---|---|
| `Player` | ✅ | ✅ | ✅ | ✅ |
| `WildSlime` | — | 2마리 | 2마리 | 2마리 |
| `BiomeDiveTrigger` | ✅ | — | — | — |
| `ExtractionPoint` | — | ✅ | ✅ | ✅ |
| `BreedingPen` | ✅ | — | — | — |

`Boot` 씬에 `DontDestroyOnLoad` 싱글턴 전부: `GameManager` `PlayerRoster`
`BiomeStigmaManager` `CorruptedGeneTagger` `LineageEvolutionChecker` `AudioManager`
`EggIncubator` `InventoryUI` `BreedingUIPanel`(`PersistentUICanvas` 밑).

---

## 부록 B. 테스트 현황

PlayMode 테스트 7파일 존재(`BreedingPenTests` `CombatTests` `PursuitTests`
`RunSatchelTests` + 헬퍼 3). **asmdef 0개.**

| 테스트 | 이번 주말 영향 |
|---|---|
| `CombatTests` `PursuitTests` | 🔴 P0-1(진영) · P0-2(주입) 로 **깨진다.** 토요일 오전에 같이 고친다 |
| `RunSatchelTests` | 🟡 스포너가 손배치 슬라임을 지우면 영향 가능 — A 가 확인 |
| `BreedingPenTests` | 🟡 종 편향 수정 시 B 가 확인 |

입력이 필요한 경로는 여전히 검증 불가 — `InputSystem` 으로 키를 밀어 넣어도
`CaptureTool.Update` 의 `wasPressedThisFrame` 에 닿지 않는다. 주말 범위 밖.
