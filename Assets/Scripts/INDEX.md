# Assets/Scripts — 코드 표지판

런타임 코드 전부. 어셈블리는 **`Game.Gameplay`** 하나(`Game.Gameplay.asmdef`),
네임스페이스는 `Game.Gameplay`. 새 Input System 만 쓴다(`Keyboard.current`,
레거시 `Input.`/`KeyCode` 금지). 파일 머리마다 `// 기능: spec-0NN` 주석이
있으니, 표에서 후보를 고른 뒤 **그 파일만** 연다.

폴더 규칙: `Data` = ScriptableObject 표, `Systems` = 영속 매니저·공용 규칙,
`Player` = 플레이어 입력·행동, `World` = 씬 안 개체, `UI` = 화면.

## Data — 밸런스·목록 표 (ScriptableObject, 재컴파일 없이 수정)

| 파일 | spec | 역할 |
|---|---|---|
| `BiomeCatalog.cs` | 009 | 바이옴 목록. biomeId → 씬 이름·등장 종 |
| `SlimeSpecies.cs` | 005 확장 | 종 하나(그림·이로치 규칙·스탯 편향·패시브) |
| `SlimeSpeciesCatalog.cs` | 005 확장 | speciesId → `SlimeSpecies` 조회표. `Resources/` 에 둔다 |
| `TraitInheritanceTable.cs` | 003 | 교배 스탯 상속·분산. 자손은 부모와 최소 1스탯 다름 |
| `LineageEvolutionTable.cs` | 006 | 오염 계보 진화 조건표 |
| `LaborOutputTable.cs` | 010 | 목장 시설 산출량 (defense/speed 를 처음 읽는 곳) |
| `StagePalette.cs` | 스테이지 | 구역별 타일 묶음. 생성기가 파일명을 하드코딩하지 않게 한다 |

## Systems — 매니저·공용 규칙 (대부분 Boot 씬 영속 싱글턴)

| 파일 | spec | 역할 |
|---|---|---|
| `GameManager.cs` | 001 | 런 상태(`RunState`)·씬 로드·현재 biomeId. **모든 흐름의 중심** |
| `EventBus.cs` / `GameEvent.cs` | 001 | 정적 이벤트 허브 + `GameEventId` enum |
| `SaveSystem.cs` | 001 | JSON 저장. WebGL 은 `PlayerPrefs` 로 갈린다 |
| `RunLogWriter.cs` | 001 | 런 로그. WebGL 은 `Debug.Log` |
| `PlayerRoster.cs` | 002 | 보유 슬라임 목록 + `RosterChanged`(UI 는 `Start` 에서 구독) |
| `RunSatchel.cs` | 011 | 다이브 중 임시 배낭. 추출 성공해야 로스터로, 죽으면 몰수 |
| `BiomeStigmaManager.cs` | 004 | 바이옴별 낙인 스택 → 오염 티어(3스택=1티어, 캡 5) |
| `CorruptionOverlay.cs` | 004 | 티어에 비례해 화면을 덮는 카메라 자식 오버레이 |
| `MutantRollService.cs` | 005 | 돌연변이 굴림 + 그 안에서 이로치 재굴림(25%) |
| `CorruptedGeneTagger.cs` | 006 | 포획 시 오염 유전자 태깅 |
| `LineageEvolutionChecker.cs` | 006 | 계보 진화 조건 판정 |
| `IDamageable.cs` | 007 | `IFactionMember`/`Faction`/피해 인터페이스. **아군 오사 가드가 여기 있다** |
| `Slowable.cs` | 패시브 | `ISlowable` — 늪지대 패시브가 거는 둔화 |
| `ActorAnimation.cs` | 007/012 | 플레이어·슬라임 **공용** 애니메이션+효과음 배선 |
| `AudioManager.cs` | — | BGM/SFX 소스 2개·볼륨(`PlayerPrefs`). 씬별 BGM 은 GameManager 가 호출 |
| `EggIncubator.cs` | 003 | 알 부화 타이머(껐다 켜도 시각 기준으로 처리) |
| `CameraFollow.cs` | 001 | 카메라 추적 |
| `HealthBar.cs` / `DamageFlash.cs` | 007 | 월드 체력바 / 피격 점멸 (플레이어·슬라임 공용) |
| `PersistentRoot.cs` | 003 | 붙이면 `DontDestroyOnLoad` |
| `SlimeBestiary.cs` | QA-09 | 잡아 본 종·이로치 기록(도감). `SaveSystem` 키 `bestiary` |
| `WantedBoard.cs` | QA-09 | 현상수배 — 다이브마다 강화 개체 1마리, 진행도 저장 |

## Player — 입력·행동 (Player.prefab 에 붙는다)

| 파일 | spec | 역할 |
|---|---|---|
| `PlayerMovement.cs` | 007 | WASD 이동 + 마지막 바라본 방향 유지 |
| `PlayerMeleeAttack.cs` | 007/012 | 마우스 좌클릭 근접 공격. `IFactionMember` 만 구현(맞는 쪽 아님) |
| `PlayerHealth.cs` | 007 | HP 30, `IDamageable`+`ISlowable` |
| `CaptureTool.cs` | 002 | E 포획 → 배낭. 돌연변이/오염 태깅은 각각 005/006 이 이어받음 |
| `RanchFacilityInteractor.cs` | 010 | Z 선택 전환 / X 배치 / C 회수 |

## World — 씬 안 개체

| 파일 | spec | 역할 |
|---|---|---|
| `SlimeInstance.cs` | 007 | 개체 하나의 순수 데이터(세이브로 오감, 씬 참조 불가) |
| `SlimeStatBlock.cs` | 007 | 스탯 4종 + 피해 계산(방어 차감, 최소 1) |
| `WildSlimeAgent.cs` | 012 | 야생 슬라임 상태기·추격. `Initialize` 2종 — **밖에서 스탯 다시 걸지 말 것** |
| `CompanionAgent.cs` | 주말 B | 동행 슬라임(상한 1). 쓰러지면 로스터에서 영구 삭제 |
| `SpeciesPassive.cs` | 주말 B | 종별 패시브 4종(도발·범위 힐·둔화·도트), 전부 "주기마다 반경 훑기" |
| `PassiveRangeRing.cs` | 주말 B | 패시브 반경을 바닥에 원으로 표시 |
| `Steering.cs` | 주말 B | 벽에 박지 않고 미끄러지도록 속도를 꺾는 정적 헬퍼 |
| `SlimeSpawner.cs` | 스테이지 | 방 목록을 받아 야생 슬라임 배치 |
| `StageLayout.cs` | 스테이지 | 방·복도 **계산**(그리기 없음) + `RoomPattern` |
| `StageMapGenerator.cs` | 스테이지 | 계산 결과를 타일맵에 **그리기** + 플레이어·추출구 이동. 장식은 벽 안쪽만 |
| `StageRegionTrigger.cs` | 스테이지 | 방에 들어오면 `CurrentBiomeId` 를 그 구역으로 교체 |
| `BreedingPen.cs` | 003/005/006/008 | 교배 본체. speciesId 를 부모 50:50 으로 상속(=PNG 유전) |
| `SlimeEgg.cs` | 003 | 알. `shinyFlag`/`shinyTint` 를 실어 나른다 |
| `RanchFacility.cs` | 010 | 배치된 슬라임이 주기마다 자원 산출 |
| `BiomeDiveTrigger.cs` | 001/004/009 | 목장→바이옴 진입 트리거(키 없음, `OnTriggerEnter2D`) |
| `ExtractionPoint.cs` | 001 | 추출구. **벽 안쪽에 있어야 한다**(도달 불가 사고 이력) |
| `SlimeAppearance.cs` | 005 확장 | 개체별 그림·색·컨트롤러 적용. 색은 `SpriteRenderer.color` 로만 |
| `ShinyGlow.cs` | 005 확장 | 이로치 윤곽 발광(색만으로는 구분이 안 돼 추가) |
| `ActorShadow.cs` | QA-09 | 발밑 타원 그림자. `ActorAnimation` 이 **플레이어에게만** 붙인다 |

## UI — 화면 (캔버스 기준 1920×1080, `ScaleWithScreenSize`)

| 파일 | spec | 역할 |
|---|---|---|
| `BootMenuUI.cs` | — | 시작 화면 겸 일시정지 메뉴. 자식은 `transform.Find` 로 찾는다 |
| `SettingsPanel.cs` | — | 배경음·효과음 슬라이더 |
| `HelpPanel.cs` | — | 조작키·규칙 설명(부팅 시 1회 자동) |
| `InventoryUI.cs` / `InventorySlotView.cs` | 002 | I 키 보유 목록(영속). 슬롯 뷰는 교배·목장이 재사용 |
| `BreedingUIPanel.cs` / `RosterSlotButton.cs` / `EggSlotView.cs` | 003 | U 키 교배 패널. **클릭은 선택만, 교배는 버튼으로만** |
| `SatchelCounterUI.cs` | 011 | B 키 배낭 — 명패 + 격자 창을 코드로 만든다(씬 것은 끈다) |
| `SlimeIconSlot.cs` | 011/QA-09 | 코드로 만드는 슬라임 칸(슬롯 틀 + 그림 + hover 쪽지) |
| `MinimapUI.cs` | QA-09 | 미니맵. `StageLayout` 을 텍스처로 굽고 점 둘만 매 프레임 옮긴다 |
| `SatchelSlotView.cs` | 011 | 옛 배낭 줄. 씬 템플릿에만 남아 있다 |
| `CapturePromptUI.cs` | 002 | 포획 가능 안내 |
| `ExtractionDirectionArrow.cs` | 001 | 추출구 방향 화살표 |
| `HudRoot.cs` | — | 코드로 만드는 고정 HUD 의 공용 캔버스 뿌리 |
| `ScreenInfoHud.cs` | QA-09 | 조작 안내(H 로 접기) + 오염 티어·돌연변이 확률 |
| `BestiaryPanel.cs` | QA-09 | J 도감 창. 안 잡은 종은 실루엣 + `???` |
| `MainVolumeControls.cs` | QA-09 | 시작/일시정지 메뉴의 배경음·효과음 슬라이더 |
| `SlimeTooltipUI.cs` | QA-09 | 슬롯에 마우스를 올리면 뜨는 스탯 쪽지(인벤토리·교배창 공용) |
| `AttackCooldownBar.cs` / `CompanionHudBar.cs` | 주말 B | 공격 쿨다운 / 동행 체력 |
| `FloatingText.cs` / `WorldLabel.cs` | — | 떠오르는 피해 숫자 / 월드 이름표 |
| `ScreenWipe.cs` | — | 씬 전환 와이프 |

## 루트

| 파일 | 역할 |
|---|---|
| `FogOfWarReveal.cs` | 대상 주변만 안개 타일을 걷어낸다(내·외 반경 2단) |
| `Game.Gameplay.asmdef` | 런타임 어셈블리 정의 |

## 자주 틀리는 지점

- `PlayerRoster.RosterChanged` 구독은 **`Start`**(`OnEnable` 아님) — Boot 영속 오브젝트끼리 `Awake` 순서가 안 보장된다.
- `IDamageable` 구현부의 `Factions.IsFriendlyFire` 가 유일한 아군 오사 가드다. 호출부에 가드 추가 금지.
- `WildSlimeAgent.Initialize(species, tier)` 는 티어 배율 → 종 편향 순서로 스탯을 만든다. 두 번 부르면 두 번 곱해진다.
- 종별 스탯·그림·패시브는 코드가 아니라 `Assets/SO/Species/*.asset` 에 있다.
- `Steering.IsWall` 은 "리지드바디가 없거나 Static" 을 벽으로 친다. 새 장애물을 Dynamic 으로 두면 슬라임이 그걸 밀며 끼인다.
- 화면에 붙는 새 UI 는 씬이 아니라 `HudRoot.Get()` 밑에 코드로 만든다.
