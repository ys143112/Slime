# Slime (Slime Stigma Ranch)

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
- `Unity_ManageAsset Action=Move` 는 실패 응답(`MoveAsset call failed
  unexpectedly`)을 내고도 실제로는 이동에 성공하는 경우가 있었다 — 응답을
  못 믿겠으면 파일시스템으로 직접 확인.

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
