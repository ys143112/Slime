# Slime (Slime Stigma Ranch)

**도구가 실패하면 반복 재시도하지 말 것.** 한두 번 진단성 시도까지만 —
그래도 안 되면 멈추고 상황과 해결 방법(우회안 포함)을 사용자에게 제시하고
물어본다. 토큰 낭비된다(사용자 지시, 2026-08-06).

로그라이트 몬스터 목장 시뮬레이션. 기획서 원본은
`C:/dev/Game-Developer-AI/Doc/설계/plans/slime-rancher-roguelite.json`
(spec-001~012, 각 goal/implementationScope/acceptanceCriteria/unityHints 포함).
**이 파일이 오래돼 기획서와 어긋나면 기획서가 맞다** — 이 파일은 탐색 시간을
줄이는 캐시일 뿐, 진실의 원천이 아니다.

## 상태

**12개 spec 전부 코드 구현 완료, DEV 브랜치에 병합됨.** 남은 일은 스펙별
런타임 검증 + 시각 자산 보강 + 자동 테스트(전무). 브랜치 전략: 기능별로
`feat-*` 브랜치 파서 작업 후 `dev` 로 머지.

spec-002/011 HUD 타이밍은 satchel 기준으로 확정됨(2026-08-05): 다이브
중 캡처는 `RunSatchel` 에 쌓이고, 추출 성공해야 `PlayerRoster` 로 옮겨간다
("죽으면 몰수"가 의미 있으려면 이래야 함) — spec-002 원본 문서의
acceptanceCriteria 를 satchel 기준으로 고쳤다(원본:
`Doc/설계/plans/slime-rancher-roguelite.json`).

## Spec → 파일 매핑

| spec | 핵심 파일 |
|---|---|
| 001 런루프 | `GameManager` `EventBus` `GameEvent` `SaveSystem` `RunLogWriter` `ExtractionPoint` `BiomeDiveTrigger` `CameraFollow` `ExtractionDirectionArrow` |
| 002 캡처 | `CaptureTool` `PlayerRoster` `CapturePromptUI` `InventoryUI` `InventorySlotView` |
| 003 교배 | `BreedingPen` `TraitInheritanceTable` `BreedingUIPanel` `RosterSlotButton` `EggSlotView` |
| 004 낙인/오염티어 | `BiomeStigmaManager` `CorruptionOverlay` |
| 005 돌연변이 | `MutantRollService` |
| 006 오염 유전 계보 | `CorruptedGeneTagger` `LineageEvolutionChecker` `LineageEvolutionTable` |
| 007 전투 | `PlayerHealth` `PlayerMeleeAttack` `PlayerMovement` `IDamageable` `SlimeInstance` `SlimeStatBlock` `HealthBar` `DamageFlash` |
| 008 교배장 상호작용 | `BreedingPenInteractor` |
| 009 바이옴 로스터 | `BiomeCatalog` (`Assets/SO/BiomeCatalog.asset`, 3바이옴: default/marsh/ashfall) |
| 010 목장 시설 | `RanchFacility` `RanchFacilityInteractor` `LaborOutputTable` |
| 011 런 전리품 | `RunSatchel` `SatchelCounterUI` `SatchelSlotView` |
| 012 야생 슬라임 AI | `WildSlimeAgent` |

## 슬라임 종·이로치 (2026-08-06 신규)

종 하나가 곧 그림 하나다. `SlimeSpecies`(ScriptableObject, `Assets/SO/Species/`)가
그림·이로치 규칙·스탯 편향을 들고, `SlimeSpeciesCatalog`
(`Assets/Resources/SlimeSpeciesCatalog.asset`)가 `speciesId` 로 그걸 찾는다.
**Resources 에 두는 이유**: `SlimeInstance` 는 세이브로 오가는 순수 데이터라 씬
참조를 못 들고, 교배·스폰·목록 UI 가 저마다 같은 표를 봐야 한다.

| 종 | 등장 | 스탯 편향 | 이로치 |
|---|---|---|---|
| `slime_basic` 파란 | 전 바이옴 | 기본 | 늪지=초록, 용암지=빨강, **초원엔 없음** |
| `slime_guard` 방어형 | 초원 | HP1.4 / **공격 0** / 방어 2.2 / 속도 0.8 | 지정 3색 중 랜덤 |
| `slime_rainbow` 무지개 | **교배로만** | 전체 0.8~0.9 | **없음** (단일 개체) |
| `slime_bog` 늪지대 | 늪지 | 기본 | 지정 3색 중 랜덤 |
| `slime_lava` 용암 | 용암지(잿벌) | 전체 1.1~1.2 | 지정 3색 중 랜덤 |

**이로치는 돌연변이의 하위 종류다** — `mutantFlag` 가 켜진 개체에서만 다시
굴린다(`MutantRollService.TryRollShiny`, 돌연변이 안에서 25%). 그래서 이로치는
항상 스탯 반전도 함께 갖는다(역은 아니다). `RollMutantAndShiny()` 가 두 굴림을
묶어 두므로 포획·교배가 같은 순서를 밟는다.

**색 규칙이 없는 바이옴에서는 이로치가 아예 안 뜬다**(`CanBeShinyIn`). 이 검사가
없으면 파란 슬라임이 초원에서 "흰색 이로치"가 된다 — 플래그만 켜지고 눈으로는
평범한 개체와 구분이 안 되는 상태다. 실측으로 잡아 막았다.

**PNG 유전**: 교배 자손의 `speciesId` 를 부모 중 50:50 으로 고른다
(`BreedingPen.Breed`). 종이 곧 그림이므로 이것이 PNG 유전이다. 실측 200회
98:102. `SlimeEgg` 도 `shinyFlag`/`shinyTint` 를 실어 나른다 — 안 그러면 부화
순간 색이 사라진다.

**색은 `SpriteRenderer.color` 로만 건다**(`SlimeAppearance`). 이 값은 렌더러마다
따로라 같은 프리팹에서 나온 다른 슬라임에 안 번진다. `sharedMaterial` 을
건드리면 그 머티리얼을 쓰는 개체가 전부 같이 변하고 에디터에서는 자산 파일에까지
남는다. 이로치 **전용 그림**이 있으면 색은 안 곱한다(두 번 어두워진다).

**아직 안 만든 것**: 종별 패시브(방어형 도발, 무지개 범위 힐, 늪지대 둔화,
용암 도트). 스탯 편향과 그림까지만 있고 스킬은 없다.

## 애니메이션·효과음 (2026-08-06 신규)

플레이어와 슬라임이 **같은** `ActorAnimation` 컴포넌트를 쓴다 — 둘 다 idle /
8방향 이동 / 8방향 공격 / 피격이라는 같은 상태 집합이라 나눌 이유가 없다.

- Animator 파라미터: `MoveX` `MoveY` `Speed`(float), `Attack` `Hit`(trigger), `Dead`(bool)
- 컨트롤러: `Assets/Animations/{Player,Slime}Animator.controller` —
  `Assets/Editor/ActorAnimatorGenerator.cs` 가 만든다
  (메뉴 `SlimeRanch/Generate Actor Animators`). **이미 있으면 건너뛴다** —
  사용자가 끼운 클립을 지우지 않기 위해서다. 다시 만들려면 파일을 먼저 지운다.
- Idle 도 8방향이다. 정지할 때 `MoveX/MoveY` 를 0 으로 덮지 **않는다** — 덮으면
  블렌드트리가 원점으로 돌아가 어느 쪽을 보고 섰는지 잃는다.
- 8방향 블렌드트리의 Motion 칸은 **비어 있다.** 클립을 끼우면 바로 돈다.

**효과음은 애니메이션 이벤트가 아니라 스크립트가 낸다** — 클립이 아직 없는
상태에서도 배선이 성립해야 하고(이벤트는 클립에 붙는다), 클립을 갈아 끼워도
효과음 배선이 안 날아간다. `ActorAnimation` 인스펙터에 idle/이동/공격/피격/사망
칸이 있고 전부 `AudioManager.PlaySfx` 를 거친다 — 여기서 `AudioSource` 를 직접
두면 그 개체만 설정 패널의 효과음 슬라이더를 무시하게 된다.

`AudioManager`(Boot 영속 싱글턴)는 BGM/SFX `AudioSource` 두 개와 볼륨을 들고
`PlayerPrefs` 에 저장한다. 배경음 칸은 타이틀/목장/바이옴 세 개이고
`GameManager.LoadContentScene` 이 씬마다 `PlaySceneBgm` 을 부른다(같은 곡이면
다시 시작하지 않는다 — 씬을 오갈 때마다 처음으로 튀면 끊긴 것처럼 들린다).
**클립은 아직 하나도 없다.** 전부 null 이면 조용히 넘어간다.

## 조작

WASD 이동, Space 근접공격, E 포획, I 인벤토리, Q/F/G 교배장, Z/X/C 목장 시설.
spec-001 다이브 트리거는 키 없음 (`OnTriggerEnter2D` + `CompareTag("Player")`).

## 전투 수치

플레이어 HP 30 / 공격 8. 야생 슬라임 HP 20(tier0) / 공격 5. 티어 배율
`1 + 0.15×tier`. `defense` 필드는 존재하지만 데미지 계산에서 안 읽힘(미사용).
Space 3타로 약화, 접촉 6회로 플레이어 사망. 낙인 스택 3당 티어 1, 티어 캡 5.

## 씬 · 영속 오브젝트

씬: `Boot`(부팅) → `Hub`(목장) / `Biome` `BiomeMarsh` `BiomeAshfall`(바이옴 3종,
`GameManager.SceneNameFor()` 가 biomeId→씬 해석). `Boot` 에 `DontDestroyOnLoad`
싱글턴 전부 있음: `GameManager` `PlayerRoster` `BiomeStigmaManager`
`CorruptedGeneTagger` `LineageEvolutionChecker` `InventoryUI`(Canvas 포함, I 키
토글, Screen Space Overlay라 씬 안 가려도 항상 뜬다). `PersistentRoot`
붙은 `PersistentUICanvas` 밑에 `BreedingUIPanel`(U 키)도 같은 방식으로 얹혀
있다 — `pen` 필드는 Hub 씬에만 있는 `BreedingPen` 을 매번
`GameObject.Find("BreedingPen")` 로 다시 찾는다(캐시하면 씬 전환마다 죽은
참조가 됨), Hub 밖에서는 U 를 눌러도 안 열린다. `InventoryUI`/`BreedingUIPanel`
은 서로 `static Instance` 를 참조해 상호 배타로 연다(I 누르면 U 닫히고 반대도
같음) — 안 하면 두 창이 겹쳐 뜬다(2026-08-05).

`PlayerRoster.RosterChanged` 를 구독하는 UI(`InventoryUI`/`BreedingUIPanel`)는
**`OnEnable` 이 아니라 `Start` 에서 구독한다** — 같은 Boot 씬 영속 오브젝트끼리
`Awake` 순서가 안 보장돼, `OnEnable` 시점엔 `PlayerRoster.Instance` 가 아직
null 일 수 있고 그러면 구독이 조용히 스킵된 채 다시는 안 걸린다(2026-08-05,
교배 UI에 잡은 슬라임이 안 뜨는 버그로 드러남). `Start` 는 씬의 모든 `Awake`가
끝난 뒤 실행이 보장된다.

`BreedingUIPanel` 로스터 그리드 클릭은 **선택/해제만** 한다 — 예전엔 2마리가
차는 즉시 자동으로 교배(소모)했는데, 그리드가 다시 그려지며 자리가 밀리는 와중에
연타하면 의도 안 한 조합이 곧바로 소모돼 버렸다(2026-08-05, 로스터 전멸 버그).
실제 교배는 `breedButton` 을 눌러야만 일어난다. 선택된 슬롯은 라벨 앞
`▶` 표시 + 살짝 확대로 구분된다.

**Boot 씬으로 Play 눌러야 한다** — Hub/Biome 씬에서 바로 Play 하면
`GameManager.Instance` 가 null (Boot 를 거치지 않아 싱글턴이 생성 안 됨).

Boot 씬에 시작 화면(`BootCanvas`)이 있다 — 시작하기/설정/종료 버튼
(`BootMenuUI`), 설정 버튼을 누르면 뜨는 `SettingsPanel`(배경음·효과음
슬라이더, 조작키 설명, 게임 설명, 닫기)로 구성된다(2026-08-06).
`GameManager.Start()` 가 예전엔 즉시 Hub 를 띄웠지만 이제는 그 자동 진입을
없애고 `GameManager.BeginGame()` 을 시작하기 버튼이 호출하는 방식으로 바꿨다 —
Play 를 눌러도 시작 화면에서 멈춰 있는 게 정상이다. 배경은 흰 placeholder
Image(`Background`), 실제 PNG 는 나중에 교체 예정.

**`SettingsPanel` 은 씬에 활성 상태로 두고, `BootMenuUI.Awake` 가 런타임에
끈다.** 씬에서 비활성으로 저장해두면 MCP 도구가 그 서브트리를 아예 못
건드리게 되기 때문이다(아래 `Unity_ManageGameObject` 함정 참고) — 편집
가능성을 유지하려는 의도적 선택이다.

두 스크립트(`BootMenuUI`/`SettingsPanel`)는 인스펙터 배선 대신
`transform.Find` 로 자식을 찾는다 — 위 MCP 함정(오브젝트 참조 필드 배선이
안 먹힘) 때문에 고른 방식이다.

캔버스는 **1920×1080 기준**이다: `CanvasScaler` 가 `ScaleWithScreenSize`,
reference resolution 1920×1080, match 0.5. UI 좌표는 전부 이 해상도 기준의
픽셀값으로 넣는다 — 예전에 800×600 짜리 `ConstantPixelSize` 였던 걸 바꿨다.

`AudioManager`(Boot 씬 영속 싱글턴)가 BGM/SFX `AudioSource` 두 개와 볼륨을
들고 있고 `PlayerPrefs` 에 저장한다. **실제 클립은 아직 하나도 없다** —
`PlayBgm`/`PlaySfx` 와 `BootMenuUI.clickSfx` 는 클립을 꽂으면 바로 도는
빈 틀이다(클립이 null 이면 조용히 무시).

EventSystem 은 씬마다 하나씩 필요(uGUI 버튼 클릭용) — Hub, Biome 3종 전부
갖고 있음. 새 씬 만들면 빠뜨리기 쉬움, 확인할 것.

세이브 파일: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Slime\Saves\
{biome_stigma,player_roster}.json`. 런타임 검증하다 낙인/로스터에 테스트
데이터 섞이기 쉽다 — 실제 플레이 전에 확인.

## 에셋 파이프라인 (PixelLab 경유)

`mcp__asset__generate_*` 계열 도구는 **`gameId` 를 반드시 명시**해야 한다
(`slime-rancher-roguelite`) — 안 주면 `default` 게임/팔레트로 새서 스타일이
어긋난다. 생성 후 `mcp__unity__import_asset` 으로 Unity 로 가져오면
**`Assets/Generated/` 에 떨어지는데 이 폴더는 `.gitignore` 대상**이다 — 그대로
두면 커밋에서 빠져 다른 사람 클론에서 참조가 깨진다. 반드시
`AssetDatabase.MoveAsset` 으로 `Assets/Resources/{Icons,Tiles,Decor,UI}/` 로
옮긴 뒤 커밋한다(GUID 보존되어 참조 안 깨짐).

Import 직후 `.meta` 기본값이 프로젝트 관례와 다르다 — 고쳐야 하는 필드:
`filterMode: 1`→`0`(Point), `spriteMode: 2`→`1`(Single),
`spriteMeshType: 1`→`0`(Full Rect, world sprite 타일링 쓸 때만 중요),
`spritePixelsToUnits: 100`→ 생성 시 넘긴 `size` 값과 동일하게(예: 32px 요청
→ PPU 32, 128px 아이콘 → PPU 128) — 안 맞추면 씬에서 크기가 어긋난다.
고친 뒤 `Unity_ManageAsset Action=Import` 로 강제 재임포트.

## ExtractionPoint 는 벽 안쪽에 있어야 한다

바이옴 씬의 `Walls`/`Collision` `CompositeCollider2D` 가 실제 걸어다닐 수 있는
영역을 정한다 — `ExtractionPoint` 를 그 바깥에 두면 트리거 자체는 멀쩡해도
플레이어가 걸어서 절대 도달할 수 없다(2026-08-05, 세 바이옴 씬 전부 (16, -10)
에 있었는데 벽 범위는 대략 x/y ±8~±12 였다 — 트리거·스프라이트 다 정상인데
"눌러도 안 됨" 버그로만 드러났다). 좌표를 손으로 넣기 전에
`CompositeCollider2D.bounds`/`OverlapPoint` 로 벽 안쪽인지 확인할 것 — 지금은
세 씬 다 (9, -6) 로 옮겼다.

## 씬 병합 함정

`.unity` 는 텍스트 병합이 줄 단위로 되지만 의미 단위(오브젝트 하나)로는 안 된다
— 두 브랜치가 같은 fileID 슬롯 근처를 건드리면 git 이 충돌 표시 없이 한쪽
오브젝트 전체를 통째로 삼켜버릴 수 있다. 실제로 `f6999ef`(`feat-lobby`→`dev`
머지)가 이렇게 `InventoryUI` Canvas 전체(패널·슬롯 컨테이너·타이틀, 4개
GameObject)를 말끔히 지웠다 — diff 에 삭제선(`-`)이 없어서 아무도 눈치 못 챘고,
"I 키 눌러도 아무것도 안 뜸" 버그 리포트로만 드러났다(2026-08-05, 복구함).
씬을 건드리는 브랜치를 머지한 뒤에는 `grep -c "^--- !u!" before after` 로 오브젝트
총수가 두 브랜치 합보다 부자연스럽게 줄지 않았는지 확인하는 게 값싸다.

## Unity_RunCommand 함정 (겪은 것들)

- `Image` 단독 타입명은 다른 어셈블리의 namespace 와 충돌해 컴파일 에러
  (`CS0118`). 항상 `UnityEngine.UI.Image` 로 완전한 이름 써야 한다.
- `System.Reflection` 사용 금지(프로젝트 규칙이자 RunCommand 자체가 막음).
- `GameObject.Find` 는 **비활성 오브젝트를 못 찾는다** — 부모가 `SetActive(false)`
  면 자식도 못 찾음. `transform.Find("Parent/Child")` 로 활성 루트에서부터
  경로 탐색할 것.
- `SceneManager.LoadScene` 은 같은 프레임에 안 끝난다 — 씬 전환 직후 같은
  RunCommand 안에서 결과를 읽으면 이전 씬 상태가 보인다. 별도 호출(=다음
  프레임)로 나눠서 확인.
- Play 모드 진입 직후 새로 생성된 오브젝트도 `Update()` 가 최소 한 프레임은
  지나야 반영된다 — 씬 전환과 마찬가지로 별도 호출로 확인.
- `EditorSceneManager.OpenScene` 은 Play 모드 중엔 예외 던짐 — 씬 편집 전엔
  꼭 `Stop` 부터.
- MCP 브리지가 가끔 "Unity not detected" 로 한 번씩 끊긴다(도메인 리로드
  타이밍 추정) — 재시도하면 대개 바로 붙는다.
- **Play 중에 RunCommand 를 여러 번 부르면 `static` 이 조용히 날아간다.**
  RunCommand 는 매번 코드를 컴파일하고, 그게 도메인 리로드를 부른다.
  `DontDestroyOnLoad` 오브젝트는 살아남지만 `Awake` 는 다시 안 돌아서
  `GameManager.Instance` 같은 static 싱글턴이 **전부 null 이 된다**
  (2026-08-06 실측: 오브젝트는 DontDestroyOnLoad 씬에 멀쩡히 있는데
  `Instance` 만 null). 증상이 "씬은 맞는데 매니저가 없다" 라서 씬 배선을
  의심하게 되지만 배선 문제가 아니다. 긴 런타임 검증은 **호출 수를 줄이고**,
  중간에 null 이 보이면 Stop→Play 로 다시 시작한다.
- `AssetDatabase.DeleteAsset` 은 그 자산이 에디터에서 열려 있으면 대화상자를
  띄우려다 `User interactions are not supported` 로 죽는다. 마찬가지로
  `AnimatorController.CreateAnimatorControllerAtPath` 와
  `CreateBlendTreeInController` 도 같은 이유로 MCP 경유로는 못 쓴다 —
  이런 자산 생성은 `Assets/Editor/` 에 `[MenuItem]` 스크립트로 두고
  `Unity_ManageMenuItem Action=Execute` 로 부른다(파일을 새로 만들었으면
  컴파일이 끝나야 메뉴에 올라온다 — `IsCompiling` 이 false 가 될 때까지 기다릴 것).
- `PrefabUtility.UnloadPrefabContents` 뒤에 그 프리팹 안의 오브젝트를 만지면
  `MissingReferenceException` 이다. 로그에 쓸 이름 같은 값은 **언로드 전에**
  문자열로 뽑아 둔다.
- `Unity_ManageAsset Action=Move` 는 실패 응답(`MoveAsset call failed
  unexpectedly`)을 내고도 실제로는 이동에 성공하는 경우가 있었다 — 응답을
  못 믿겠으면 파일시스템으로 직접 확인.

## Unity_ManageGameObject 함정 (겪은 것들, 2026-08-06 Boot 시작화면 작업)

- `create` 액션에 같이 넘긴 `component_properties` 는 **RectTransform/Text 등
  새로 붙은 컴포넌트에 조용히 안 먹는다** — 에러 없이 성공 응답을 주고 값은
  기본값(예: RectTransform `sizeDelta 100x100`, `anchorMin/Max 0.5,0.5`,
  `Text.text` 빈 문자열)으로 남는다. `create` 로 오브젝트만 만들고, 속성은
  **별도의 `modify` 호출**로 나눠야 실제로 적용된다.
- 컴포넌트 키는 짧은 이름("RectTransform")이 아니라 **완전한 이름
  ("UnityEngine.RectTransform")을 써야 한다** — 짧은 이름은 에러 없이 조용히
  무시된다. `Image` 가 이름 충돌로 에러 내는 것과 달리 이건 성공 응답을 주면서
  아무 일도 안 하므로 더 놓치기 쉽다.
- `modify`/`delete`/`get_components` 는 **비활성(`SetActive(false)`) 오브젝트를
  못 찾는다** — `target` 이 이름이든 경로든 인스턴스ID든, `search_inactive: true`
  를 줘도 마찬가지다(전부 "not found"). `find` 액션만 `search_inactive` 를
  지킨다. 자식까지 다 배선한 뒤에 마지막으로 부모를 비활성화할 것 — 비활성화
  먼저 하면 그 서브트리는 이 도구로 더 이상 못 건드리고, `.unity` 를 직접
  텍스트로 고치는 수밖에 없다(RectTransform/Text 블록은 `m_GameObject:
  {fileID: N}` 로 유일하게 식별된다 — 씬을 `Save` 한 뒤 고치고 `Load` 로
  다시 읽어들인다).
- 이 두 함정이 겹치면: 자식 오브젝트를 만들고 → `modify` 로 값 배선 시도 →
  실패를 못 알아채고 부모를 비활성화 → 이제 그 값들을 고칠 방법이 도구
  안에는 없다. **`create` 직후 반드시 `get_components` 로 실제 값이 들어갔는지
  확인**하고 나서 다음 단계(특히 비활성화)로 넘어갈 것.
- 실제로 물린 사례: `create` 로 Canvas 를 만들며 `renderMode: 0`
  (Screen Space Overlay)을 같이 넘겼는데 씹혔고, **World Space(2)로 남았다.**
  월드 캔버스는 하이라키에는 멀쩡히 보이지만 화면에는 아무것도 안 나온다 —
  "오브젝트는 다 있는데 화면이 비었다"는 증상으로만 드러났다(2026-08-06,
  Boot 시작 화면). `CanvasScaler` 에 `m_PresetInfoIsWorld: 1` 이 같이 박히는
  것이 흔적이다. Canvas 를 만들었으면 `renderMode` 를 눈으로 확인할 것.

**UI 배치는 씬 좌표가 아니라 화면 좌표로 확인한다.** RectTransform 의
`anchoredPosition` 만 봐서는 화면에 들어오는지 알 수 없다(위 월드 캔버스
사고가 그랬다 — 좌표는 전부 정상이었다). Play 중에
`RectTransform.GetWorldCorners()` 로 각 요소의 좌하·우상 코너를 받아
`0..Screen.width`, `0..Screen.height` 안에 있는지 보는 것이 값싸고 확실하다.

## 코드 규약

새 Input System만 사용(`Keyboard.current`) — 레거시 `Input.`/`KeyCode` 금지.
월드 공간 UI(HP바, 오염 베일)는 Canvas 없이 프리팹/카메라 자식 스프라이트로
처리 — Hub 는 World Space Canvas 하나뿐이고 나머지 UI 배선이 씬마다 늘어나는
걸 피하려는 선택. 화면 전체를 덮는 오버레이(오염 베일 등)는 **Main Camera 의
자식**으로 두면 카메라 위치를 안 따라다녀도 항상 화면을 덮는다(카메라
로컬좌표라 자동으로 따라감), 뷰포트 크기 계산 불필요 — 그냥 확실히 큰
스케일(60×40 같은)로 깔면 됨.

테스트 프레임워크(`com.unity.test-framework`)는 설치돼 있지만 **테스트
0개, asmdef 0개** — 기획서의 `Test_*` 이름 25개가 전부 미작성 상태.

## 이 파일 갱신 규칙

세션 끝에 구조가 바뀌었으면(새 spec 완료, 새 씬, 새 영속 싱글턴, 새로 겪은
RunCommand 함정 등) 이 파일을 그 자리에서 고친다. 오래된 정보를 남겨두는
것보다 지우는 게 낫다 — 틀린 캐시는 안 읽느니만 못하다.
