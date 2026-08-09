# Slime (Slime Stigma Ranch)

**표지판 먼저**: 이 파일은 42KB 라 통째로 읽지 말고 절 단위로만 본다.
어디를 볼지는 [MAP.md](MAP.md) 가 정한다 — 코드는
[Assets/Scripts/INDEX.md](Assets/Scripts/INDEX.md), 자산은
[Assets/INDEX.md](Assets/INDEX.md).

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
| 008 교배장 상호작용 | **`BreedingPenInteractor` 는 없어졌다** — 배치/회수를 U 패널(`BreedingUIPanel`)로 합쳤다. Q/F/G 키도 같이 사라졌다 |
| 009 바이옴 로스터 | `BiomeCatalog` (`Assets/SO/BiomeCatalog.asset`, 3바이옴: default/marsh/ashfall) |
| 010 목장 시설 | `RanchFacility` `RanchFacilityInteractor` `LaborOutputTable` |
| 011 런 전리품 | `RunSatchel` `SatchelCounterUI` `SatchelSlotView` |
| 012 야생 슬라임 AI | `WildSlimeAgent` `Steering` |

spec 밖(주말 작업)에서 들어와 표에 자리가 없는 것들:

| 묶음 | 파일 |
|---|---|
| 스테이지 생성 (STAGE_A_DESIGN.md) | `StageLayout`(계산) `StageMapGenerator`(그리기) `StageRegionTrigger` `SlimeSpawner` `StagePalette` `FogOfWarReveal` |
| 동행·패시브 (WEEKEND_PLAN.md §4) | `CompanionAgent` `SpeciesPassive` `PassiveRangeRing` `Slowable` `CompanionHudBar` |
| 알·부화 | `EggIncubator` `SlimeEgg` `EggSlotView` |
| 표현 계층 | `ActorAnimation` `AudioManager` `ShinyGlow` `ScreenWipe` `FloatingText` `WorldLabel` `HudRoot` `AttackCooldownBar` `ActorShadow` |
| 시작 화면·설명 | `BootMenuUI` `SettingsPanel` `HelpPanel` |

파일 단위 한 줄 설명은 [Assets/Scripts/INDEX.md](Assets/Scripts/INDEX.md) 에 있다 —
이 표는 spec 추적용이고, 저 표가 전수 목록이다.

## 발밑 그림자 (2026-08-09)

**플레이어만** 그림자를 갖는다(사용자 결정, 2026-08-09). 슬라임은 제자리에서
통통 뛰는 그림이라 그림자가 같이 움직여 오히려 떠 보였다.
`ActorShadow.Attach` 를 `ActorAnimation.Awake` 가 부르되 `PlayerMovement` 가
붙어 있을 때만 건다. Player 프리팹에 있던 손배치 `Shadow` 자식은 지웠다(몸
한가운데에 작게 박혀 그림에 가려 안 보였다).

- 타원 스프라이트는 **코드로 굽는다**(정적 텍스처 한 장 공유). PNG 하나 때문에
  프리팹 넷에 배선을 늘리지 않는다.
- **`Assets/Art/SlimeStrips/player__*.png` 33장은 Tight 메시로 임포트돼 있다.**
  Full Rect 면 `vertices` 가 캔버스 네 귀퉁이뿐이라 계산이 캔버스(3.85유닛)로
  떨어져 그림자가 발보다 한참 아래에 크게 깔린다. Tight 로 재임포트하면 실루엣이
  x±0.56 / y[-1.04, 0.96] 로 잡힌다. 새 플레이어 시트를 넣으면 이 설정을 다시
  확인할 것.
- 크기·바닥은 `Sprite.vertices`(알파 외곽을 따라 잘린 tight 메시)에서 낸다.
  **`Sprite.bounds` 를 쓰면 안 된다** — 투명 여백까지 포함한 캔버스라 플레이어
  (104px 캔버스에 실루엣 50px)는 그림자가 발보다 1유닛 아래에 깔린다. 콜라이더
  (반지름 0.5 원)도 몸 한가운데라 가슴께에 뜬다. 텍스처를 읽지 않으므로
  Read/Write Enabled 도 필요 없다.
- 그림자 **높이는 폭에서 낸다**(폭 0.85배, 높이는 그 0.38배). 몸 높이를 곱하면
  키 큰 배우 발밑에 세로로 긴 얼룩이 생긴다.
- `sortingOrder = 몸 - 1`. 같은 order 면 Y 정렬이 그림자를 몸 위로 올린다.
- 이미 자식이 있으면 아무것도 안 한다.

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

**종별 패시브는 구현됐다**(`SpeciesPassive`, 주말 B 작업). 네 종류가 전부
"주기마다 반경 안을 훑어 뭔가 한다" 라는 같은 모양이라 클래스를 넷으로 나누지
않았다 — 값(`passive` `passiveRadius` `passiveInterval` `passiveAmount`
`passiveSfx`)은 `SlimeSpecies` SO 가 들고 있어 밸런스는 재컴파일 없이 만진다.

| 종류 | 종 | 동작 |
|---|---|---|
| `Taunt` | 방어형 | 반경 안의 적이 나를 먼저 노린다. **주기가 없다** — 정적 `Taunters` 목록에 있는 것만으로 성립하고, `WildSlimeAgent` 가 대상을 고를 때 `FindTauntTarget` 을 본다 |
| `AreaHeal` | 무지개 | 반경 안을 **진영 가리지 않고** 회복(기획대로 적까지 낫는다) |
| `Slow` | 늪지대 | 적에게 `ISlowable.ApplySlow` |
| `Burn` | 용암 | 적에게 지속 피해 |

- 컴포넌트는 프리팹에 얹지 않고 `SpeciesPassive.Attach(host, instance)` 가 붙인다
  — 야생/동행 프리팹이 따로라 손으로 얹으면 종이 늘 때마다 두 번씩 고쳐야 한다.
  종이 바뀌어 패시브가 없어지면 컴포넌트와 `PassiveRangeRing` 을 **둘 다** 뗀다.
- 반경은 `PassiveRangeRing` 이 바닥에 원으로 그린다(회복 초록 / 도트 주황 /
  둔화 보라 / 도발 파랑). 안 보이면 얼마나 붙어야 하는지 알 방법이 없다.
- 효과음은 **실제로 대상이 걸렸을 때만** 낸다 — 주기마다 무조건 내면 아무도
  없는 곳에 선 슬라임이 계속 운다.
- `Taunters` 는 `static` 이라 `OnDisable` 에서 반드시 뺀다(씬 전환 시 죽은 참조).

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
- `Assets/Animations/{Player,Slime}Animator.controller` 의 Motion 칸은 **비어 있다.**
  슬라임은 아래 종별 컨트롤러가 런타임에 이걸 덮으므로 실질 기본값일 뿐이다.

## 배우 애니메이션 — 슬라임 5종 + 플레이어 (2026-08-07 신규)

**Unity 의 GIF 임포터는 첫 프레임만 가져온다.** `Assets/Art/Characters/`
아래의 `.gif` 들은 그래서 그냥 두면 정지 그림이다(`.gif.meta` 의 spriteSheet 에
`..._0` 하나만 있는 것이 그 증거). 프레임을 밖에서 펴야 한다:

```bash
python Tools/extract_slime_strips.py          # GIF → Assets/Art/SlimeStrips/*.png
python Tools/extract_slime_strips.py --demo   # 스트립 규칙 자체 검사
```
그다음 Unity 메뉴 `SlimeRanch/Build Actor Animations`
(`Assets/Editor/SlimeAnimationBuilder.cs`). **원본 GIF 을 새로 넣었으면 두 단계를
순서대로 다시 밟는다** — 파이썬만 돌리면 클립이 삭제된 스프라이트를 가리킨다.

### 플레이어

`Idle_running-4-frames_*` = 이동, `Idle_cross-punch_*` = 공격,
`cute_anime_girl_farmer_b-state_of_being_attac/.../rotations/*` = 피격,
`pixellab-cute-anime-girl--farmer--brown-*.gif` = **대기**(64×64 8프레임,
프레임 하나가 방향 하나).

**대기 시트의 방향 순서는 파일명에 없어 그림에서 알아냈다.** 머리 영역의
피부색 비율과 좌우 치우침을 재면 앞모습(피부 많고 중앙) → 옆모습(중간, 한쪽
쏠림) → 뒷모습(거의 없음)이 단조롭게 이어진다. 방향이 이름에 박힌 달리기 GIF
로 같은 값을 재서 대조해 8/8 일치를 확인했다:
`south, south-east, east, north-east, north, north-west, west, south-west`
(시계방향). 픽셀을 통째로 비교하는 방식은 포즈가 달라 안 통했다 — 8프레임 중
4개가 north 로 몰렸다.

**대기 시트만 캔버스가 64×64 이고 나머지는 104×104 인데, 인물의 픽셀 크기는
둘이 같고(높이 50 안팎) 둘 다 캔버스 중앙에 놓여 있다.** 그래서 확대하지 않고
104 캔버스 가운데에 그대로 붙인다 — 1.625배로 늘리면 픽셀이 뭉갠다. 한 배우의
셀 크기가 섞이면 PPU 하나로 크기를 못 맞춰 상태가 바뀔 때마다 인물이 커졌다
작아지므로, `--demo` 가 이 조건을 검사한다.

**프레임 속도는 동작마다 다르다.** 원본 GIF 은 전부 5fps(프레임당 200ms)로
나왔는데 슬라임이 통통 뛰는 데는 맞아도 사람이 달리고 주먹을 지르는 데 쓰면
슬로모션이 된다. `SlimeAnimationBuilder.FrameRateFor` 가 정한다:

| | fps | 길이 |
|---|---|---|
| 플레이어 공격 | 50 | 0.12초 |
| 플레이어 이동 | 10 | 0.4초 |
| 약화(Dead) | 8 | 0.6초 |
| 나머지 | 5 | — |

**공격 fps 는 상수가 아니라 계산된 값이다.** SwingArc 가 떠 있는 동안 클립이
다 돌아야 하는데 그 시간은 `Player.prefab` 의
`PlayerMeleeAttack.swingVisibleSeconds`(0.12초)가 정하므로, 빌더가 그 필드를
직렬화 이름으로 찾아 읽어 `프레임 수 / 그 시간` 으로 낸다 — 둘 중 하나만
바뀌어 어긋나는 일이 없게 하려는 것이다.

공격 **전이**도 같이 손본다(`TuneAttackTransitions`): 들어가는 전이의 0.05초
크로스페이드는 0.12초 클립의 절반 가까이를 이전 자세와 섞어 흐리게 만들므로
0 으로, 나가는 전이의 `exitTime` 은 0.9 면 마지막 프레임이 뜨기 전에 빠져나가
므로 1.0 으로 올린다. 이 조정은 **빌더가** 모든 컨트롤러에 건다 — 이미 있는
`PlayerAnimator` 는 제자리에서 채워져 `ActorAnimatorGenerator` 를 안 거친다.

플레이어 컨트롤러는 **`Assets/Animations/PlayerAnimator.controller` 를 제자리에서
채운다.** 지웠다 다시 만들면 GUID 가 바뀌어 `Player.prefab` 의 Animator 배선이
끊긴다. 슬라임 컨트롤러는 반대로 매번 지우고 다시 만든다(종 자산이 GUID 를
다시 물어 준다).

### 약화(포획 가능) 상태 = Dead

`Dead` 의 Motion 을 비워 두면 애니메이터가 `m_Sprite` 를 아예 안 굴리고
**프리팹에 저장된 값으로 되돌린다** — 슬라임이 약화되는 순간 예전 PNG 가
튀어나오던 원인이 이것이다(2026-08-07 QA 로 드러남). 두 겹으로 막았다:

1. `extract_slime_strips.py` 의 `melt_frames()` 가 피격 그림에서 **위에서 아래로
   눌리며 검게 타는 5프레임**을 만들어 `{actor}__Dead__south.png` 로 낸다.
   바닥은 셀 바닥이 아니라 **알파 경계의 바닥**에 고정한다 — 셀 바닥에 맞추면
   그림이 원래 떠 있던 만큼 아래로 뚝 떨어진 뒤 눌리기 시작한다.
   Dead 는 블렌드트리가 아니라 단일 상태라 방향이 하나면 된다. 반복 안 한다 —
   마지막 프레임(눌려서 검게 탄 모습)으로 남아 있어야 잡을 수 있다.
2. `SlimeAppearance.Apply` 가 컨트롤러를 걸면서 **정지 그림도 같이 깐다.**
   되돌아갈 값 자체를 그 종의 그림으로 만들어 둔다.

색을 커브로 넣지 않고 **그림에 구워 넣은** 이유: `SpriteRenderer.color` 는
이로치 tint 와 `DamageFlash` 가 이미 쓰고 있어 서로 덮어쓴다.

- 스트립 규칙은 파일 모양 하나로 끝난다: **셀 = 이미지 높이, 프레임 = 너비 / 높이.**
  원본 GIF 이 전부 정사각(60/92/120/172)이라 별도 메타데이터가 필요 없다.
- **PPU 는 셀 크기가 아니라 "보이는 실루엣 높이"로 정한다.** 파이썬이
  `{배우}__Idle__south` 의 알파 경계 높이를 목표 유닛(슬라임 0.75 /
  플레이어 1.875 — 둘 다 예전 그림의 실루엣 높이 그대로)으로 나눠
  `Assets/Art/SlimeStrips/ppu.csv` 에 적고 빌더가 그걸 읽는다.
  예전엔 PPU=셀 크기라 캔버스가 1×1 유닛이었는데, **캔버스 대비 실루엣 비율이
  종마다 달라서**(파랑 30/60, 무지개 38/120) 무지개만 다른 종보다 36% 작게
  나왔다. `--demo` 가 배우마다 실루엣이 목표 높이(±0.03)로 떨어지는지 검사한다.
  배우 하나 안에서는 PPU 가 하나라 동작이 바뀌어도 크기·발 위치가 안 튄다.
- 결과: `Assets/Animations/Species/{speciesId}.controller` 5개 + 클립 68개.
  `SlimeSpecies.animatorController` 에 꽂히고 `SlimeAppearance.Apply` 가 런타임에
  `Animator.runtimeAnimatorController` 로 건다(같은 컨트롤러면 다시 안 건다 —
  재대입하면 재생이 처음으로 튄다). `defaultSprite` 는 남쪽 Idle 첫 프레임으로
  자동 지정된다(인벤토리·교배 UI 용 정지 얼굴).

**GIF 파일명을 믿지 마라 — 그림을 봐라.** 이름이 전부 `Idle_custom-` 으로 시작해
동작 구분이 프롬프트 문구에만 남아 있고, 그 문구조차 그림과 어긋난다. 실제로
세 건이 틀렸다: `The_creature_abruptly_leans_fo` 는 이름만 보면 늪지대인데
그림은 무지개(120px 오렌지·초록)와 같은 캐릭터였고, 파란 슬라임의
`compresses`(차분한 눈 깜빡임)와 `settles_slightly`(파란 폭발)는 Idle/Hit 가
서로 뒤바뀌어 있었다. 매핑은 `extract_slime_strips.py` 의 `ACTION_MAP` 한 곳이다.

**빈 칸은 이렇게 메운다**(`SlimeAnimationBuilder.Resolve`): 대각선 클립이 없으면
가까운 가로 방향(NE/SE→E, NW/SW→W), 그래도 없으면 Idle. 그래서 4방향뿐인 용암
슬라임도 8칸이 다 찬다. 슬라임은 Move 전용 그림이 없어 Idle 클립을 그대로 쓴다
— 제자리에서 통통 뛰므로 이동 그림이 따로 필요 없다(플레이어는 달리기 그림이
있어 Resolve 가 그걸 집는다). Hit/Dead 는 방향이 없어 south 클립 한 장씩만 쓴다.

**효과음은 애니메이션 이벤트가 아니라 스크립트가 낸다** — 클립이 없는 상태에서도
배선이 성립해야 하고(이벤트는 클립에 붙는다), 클립을 갈아 끼워도 효과음 배선이
안 날아간다. `ActorAnimation` 인스펙터에 idle/이동/공격/피격/사망 칸이 있고
전부 `AudioManager.PlaySfx` 를 거친다 — 여기서 `AudioSource` 를 직접 두면 그
개체만 설정 패널의 효과음 슬라이더를 무시하게 된다.

`AudioManager`(Boot 영속 싱글턴)는 BGM/SFX `AudioSource` 두 개와 볼륨을 들고
`PlayerPrefs` 에 저장한다. 배경음 칸은 타이틀/목장/바이옴 세 개이고
`GameManager.LoadContentScene` 이 씬마다 `PlaySceneBgm` 을 부른다(같은 곡이면
다시 시작하지 않는다 — 씬을 오갈 때마다 처음으로 튀면 끊긴 것처럼 들린다).

**클립 10개가 `Assets/Sounds/` 에 들어왔고 전부 꽂혀 있다**(2026-08-09 확인).
칸이 여러 자산에 흩어져 있으니 소리가 안 나면 아래에서 찾는다:

| 꽂힌 곳 | 클립 |
|---|---|
| `Boot.unity` 의 `AudioManager` | `로비씬사운드.mp3`(hubBgm) `바이옴기본사운드.mp3`(biomeBgm) |
| `Boot.unity` UI | `UI클릭사운드.wav` `교배완료사운드.wav` |
| `Player.prefab` 의 `ActorAnimation` | `캐릭터이동사운드.wav` `캐릭터공격사운드.ogg` |
| `WildSlime.prefab` · `Resources/Companion.prefab` | `슬라임이동사운드.ogg` `슬라임사망사운드.wav` |
| `Hub.unity` | `텔레포트사운드.wav` |
| `SO/Species/slime_lava.asset` 의 `passiveSfx` | `화염슬라임사운드.wav` |

빈 칸은 `titleBgm` 하나뿐이다(타이틀 화면은 무음). null 이면 조용히 넘어가므로
"안 들린다" 와 "안 꽂혔다" 가 겉으로 같다 — 위 표부터 볼 것.

## 조작

WASD 이동, **마우스 좌클릭** 근접공격(2026-08-08, 예전 Space — 이동이 WASD 라
왼손만 바빴다), E 포획, I 인벤토리, U 교배 패널, B 배낭, **J 도감**,
Z/X/C 목장 시설, ESC 일시정지 메뉴, **H 화면 조작 안내 접기**. 에디터 전용으로 바이옴 씬에서 R 을 누르면 `StageMapGenerator`
가 시드를 다시 굴려 지도를 새로 그린다(`#if UNITY_EDITOR`).
**Q/F/G 교배장 키는 없어졌다** — 배치·선택·교배를 전부 U 패널로 합쳤다.
좌클릭은 `EventSystem.current.IsPointerOverGameObject()` 로 UI 위를 걸러낸다 —
안 걸면 인벤토리 슬롯을 누를 때마다 뒤에서 칼을 휘두른다.
spec-001 다이브 트리거는 키 없음 (`OnTriggerEnter2D` + `CompareTag("Player")`).

## 화면에 늘 떠 있는 것 (2026-08-09, 팀 QA 반영)

씬을 고치지 않고 **코드로 만든다** — 전부 `HudRoot.Get()` 캔버스(Screen Space
Overlay, 1920×1080 기준, sortingOrder 100) 밑이다. Boot 씬은 병합 사고로 UI 가
통째로 사라진 전력이 있어(「씬 병합 함정」) 새 위젯은 씬에 안 넣는다.

| 무엇 | 스크립트 | 자리 |
|---|---|---|
| 조작 안내 — **키캡 아이콘 10줄**(H 로 접기, `PlayerPrefs` 에 기억) | `ScreenInfoHud` | 오른쪽 아래 |
| 바이옴·오염 티어·돌연변이/이로치 확률·천장까지 남은 수 | 〃 | 오른쪽 위 |
| 미니맵(걸어본 곳만·내 자리·추출구) | `MinimapUI` | 왼쪽 위 |
| 배낭 개수 명패 + B 창 | `SatchelCounterUI` | 왼쪽 위(미니맵 아래) |
| 도감(J) | `BestiaryPanel` | 화면 가운데 |
| 슬라임 스탯 쪽지(hover) | `SlimeTooltipUI` | 커서 옆 |
| 배경음·효과음 슬라이더 | `MainVolumeControls` | 시작/일시정지 메뉴 왼쪽 아래 |
| `PAUSED - Esc to resume` | `BootMenuUI` | 게임 중 메뉴 열렸을 때 위쪽 |

- `ScreenInfoHud`/`BestiaryPanel` 은 `GameManager.BeginGame()` 이 띄운다 —
  타이틀 화면에 겹치면 시작 버튼을 가린다.
- **폰트는 `Resources/Fonts/KoreanFont`(Galmuri11) 하나다**(2026-08-09).
  씬·프리팹의 `Text` 36개와 `WorldLabel` 을 전부 이걸로 갈았다. Kenney Pixel 도
  픽셀 폰트지만 **한글 글리프가 없어** 한국어를 넣는 순간 빈칸이 된다(WebGL 은
  OS 폰트 폴백도 없다). Galmuri11 은 11px 그리드라 **글자 크기를 11의 배수로
  잡는다**(11/22/33) — 아니면 픽셀이 뭉갠다. 씬 값도 11의 배수로 내림했다.
- **UI 문구는 한국어다.** 코드가 만드는 문자열(포획 알림·쪽지·수배·휴식소·
  일시정지)과 씬 버튼 24개, 종 이름(`SO/Species/*.asset` 의 `displayName`)까지.
  `HelpPanel` 만 영어 본문을 남겨 두고 버튼으로 오간다(기본 한국어).
- **코드로 만드는 판은 `HudRoot.Frame`/`HudRoot.Slot` 을 쓴다**(2026-08-09).
  씬 창들이 쓰는 것과 같은 9-slice 자산
  (`Resources/UI/window_.../elements/{Window,slot}`)이라 톤이 맞는다. 색만 칠한
  `HudRoot.Panel` 은 이제 막대(체력·쿨다운 fill)처럼 틀이 필요 없는 것에만 쓴다.
  작은 것(도감 칸)에는 `Slot`. **키캡만은 자산을 안 쓴다** — 26px 칸에 나무
  슬롯을 넣으니 테두리가 칸을 다 먹어 글자가 동그라미에 갇혀 보였다(사용자,
  2026-08-09). 밝은 사각형 + 어두운 글자가 실제 키캡 대비다.
- **글자 칸은 창틀 테두리(왼 12 / 아래 10 / 오른 12 / 위 16)보다 넓게 들인다.**
  `ScreenInfoHud` 는 22/18/22/26 을 쓴다 — 예전 값(12/8)은 위 테두리를 밟아
  글자가 판을 벗어난 것처럼 보였다.
- **Esc 는 떠 있는 창부터 닫는다**(`BootMenuUI.CloseTopmostPanel`). 설명 →
  설정 → 인벤토리 → 교배 → 도감 순으로 하나 찾아 닫고, 닫을 게 없을 때만
  일시정지 메뉴를 연다. 타이틀 화면(`_inGame == false`)에서는 창만 닫는다 —
  메뉴가 곧 화면이라 끄면 아무것도 안 남는다.
- 포획 알림(`CapturePromptUI`)은 2초 뒤 스스로 지워진다. 지우는 자리가 없어
  "파란 슬라임 포획!" 이 플레이어를 따라다니며 런 끝까지 남아 있었다.
- **배낭(B)도 코드로 만든다**(2026-08-09). 씬 셋(바이옴)에 복제돼 있던 글자
  목록을 버리고 `SatchelCounterUI` 가 `HudRoot` 밑에 명패(왼쪽 위) + 격자 창을
  만든다. 칸은 `SlimeIconSlot`(슬롯 틀 + 종 그림 + hover 쪽지) — 인벤토리와 같은
  규칙이다. 씬에 남은 옛 오브젝트(`HudBackdrop` `SatchelIcon` `MeleeToolIcon`
  `SatchelDetailScroll`)는 `HideLegacyHud` 가 끈다.
  - **`SatchelCounterUI` 는 `SatchelCounterText` 와 같은 GameObject 에 붙어 있다.**
    그 오브젝트를 `SetActive(false)` 하면 자기 `Update` 까지 멈춰 B 키가 죽는다 —
    글자만 `Text.enabled = false` 로 끈다(2026-08-09 실측).
  - **격자 폭은 뷰포트 안에 들어가게 계산한다.** 창 760 − 여백 28×2 = 704,
    격자 안쪽 여백 16×2 를 빼면 672. 5×120 + 4×12 = 648 ≤ 672 다. 130/12 로
    잡았을 때 다섯째 칸이 오른쪽으로 삐져나왔다.
- 알 목록 한 줄(`EggSlotView.Enlarge`)은 높이 76 / 아이콘 64 / 글자 22 로 코드가
  덮는다. 씬 템플릿 값(40/32)은 그림도 글자도 안 읽혔다. 줄 높이는 부모가
  `VerticalLayoutGroup` 이라 `LayoutElement` 로 줘야 한다 — RectTransform 높이를
  직접 넣으면 다음 프레임에 레이아웃이 도로 덮는다.
- 조작 안내는 글자 덩어리가 아니라 `ScreenInfoHud.HintRows` 표(키 배열 + 동작)를
  키캡 사각형으로 그린다. 키를 추가·수정하는 자리는 그 표 하나다.
  키캡은 그림 자산이 아니라 uGUI 판+글자다 — PixelLab MCP 가 이 세션에 안 붙어
  있었고, 키가 11개라 자산으로 뽑을 이유도 없다.
- 도감은 인벤토리와 같은 **격자**다(4열, 160×190). 안 잡은 종은 그림을 검게
  눌러 실루엣 + `???`. 스탯 표는 칸에 안 들어가서 뺐다.
- 확률 숫자는 `MutantRollService.MutantChance(tier)`/`ShinyChance`/`PityRemaining`
  에서 읽는다. **UI 쪽에 상수를 다시 적지 말 것** — 밸런스를 고치면 둘이 어긋난다.
- **`BootCanvas` 는 월드 스페이스 캔버스다.** `GetWorldCorners()` 가 픽셀이 아니라
  월드 좌표(화면 ±8.9 × ±5)를 돌려준다 — 배치를 확인할 때 픽셀로 착각하지 말 것.
  픽셀 단위로 잡은 RectTransform 값은 캔버스 스케일이 1920px→화면폭으로 접어
  주므로 그대로 써도 된다.
- **슬롯 안에는 글자를 두지 않는다**(2026-08-09). 인벤토리는 격자
  (`GridLayoutGroup`, 6열 140px)라 한 칸에 그림과 글자를 같이 넣으면 포개진다 —
  종 이름·스탯·태그·동행 여부는 `SlimeTooltipUI` 가 hover 로 보여준다.
  `InventorySlotView`/`RosterSlotButton` 이 `IPointerEnterHandler` 로 부른다.
- **`Destroy` 로 `LayoutGroup` 을 지운 자리에 곧바로 다른 것을 붙일 수 없다.**
  삭제가 프레임 끝으로 밀려 그동안 `AddComponent` 가 **에러 없이 거부되고**,
  격자가 안 붙어 슬롯이 한 칸에 전부 겹쳐 쌓인다 —
  `InventoryUI.EnsureGrid` 는 `DestroyImmediate` 를 쓴다(2026-08-09 실측).
- 교배창 로스터 그리드는 **스크롤 뷰포트 안**에 있다(2026-08-09).
  `BreedingUIPanel.EnsureGridScroll` 이 런타임에 `RosterViewport`
  (`ScrollRect`+`RectMask2D`)를 만들어 씬의 `GridContent` 를 그 밑으로 옮기고
  130px 7열로 다시 깐다. 마스크를 격자에 걸면 넘친 줄이 **볼 방법 없이 잘린다** —
  마스크는 뷰포트에, 격자에는 `ContentSizeFitter`.
  격자가 180→300px 로 커진 만큼 알 목록(`PushEggListAbove`)이 위로 물러난다.
- 도감은 `SlimeBestiary`(정적, `SaveSystem` 키 `bestiary`)가 기록한다. 올리는
  자리는 `RunSatchel.Add`(포획 — 죽어서 몰수돼도 남아야 한다)와
  `PlayerRoster.Add`(부화·회수 — 교배 전용 종은 여기가 유일한 경로) 둘이다.

## 미니맵 · 벽이 보이게 (2026-08-09)

**미니맵**(`MinimapUI`, 왼쪽 위): 추출구 화살표만으로는 벽을 낀 길을 못 읽어
탈출이 어려웠다(사용자). 지도는 `StageLayout` 을 텍스처 한 장으로 굽고, 매
프레임 움직이는 건 점 두 개(나·추출구)뿐이다.

- **안개를 반영한다.** 처음엔 전부 "모르는 곳"(짙은 회색)이고,
  `FogOfWarReveal.Revealed` 이벤트를 듣고 방금 밝힌 중심 둘레(반경 6 = 169칸)만
  다시 칠한다 — 지도 전체를 훑지 않는다. 안개는 한 번 걷히면 다시 안 덮이므로
  지우는 경우가 없다.
- `FogOfWarReveal` 에 `Instance` / `IsExplored(cell)` / `Revealed` 를 뚫었다.
  타일맵 셀 좌표와 `StageLayout` 좌표가 같으므로(생성기가 `(x, y, 0)` 을 그대로
  쓴다) 변환이 필요 없다.
- **추출구 표시는 안 밝힌 곳에도 띄운다** — 어디로 나가야 하는지가 미니맵을
  넣은 이유다.
- 창 높이는 지도 비율(120×90)로 맞춘다(`FitFrameToMap`). 정사각 창에 넣으면
  위아래 검은 띠가 남아 "작은 카메라" 처럼 보인다. 배낭 명패는 그 아래
  (y −227) 로 내려가 있다 — 미니맵 높이가 바뀌면 같이 손봐야 한다.

**나침반**(`ExtractionDirectionArrow`, 왼쪽 아래): `Resources/UI/{compass,pointer}`
로 갈고 위치를 옮겼다. **바늘은 회전만 한다** — 가리키는 쪽으로 밀어 봤더니
나침반 밖으로 걸어 나간 것처럼 보였다(사용자, 2026-08-09).
**이 컴포넌트는 씬의 옛 화살표와 같은 GameObject 에 붙어 있다** — 오브젝트를
끄면 자기 `Update` 까지 멈춰 바늘이 안 돈다(각도 0 고정). 그림(`Graphic.enabled`)
만 끈다. 같은 함정이 `SatchelCounterUI` 에도 있다.

**쓰러진 슬라임은 못 민다**(`WildSlimeAgent.MakeCorpseUnpushable`): 약화되면
콜라이더를 **트리거로 바꾸고** 리지드바디를 Static 으로 내린다. 끄지 않고 트리거로
두는 이유는 `CaptureTool` 이 `OverlapCircleAll` 로 잡을 대상을 찾기 때문이다 —
트리거는 그 질의에 잡히면서 밀리지는 않는다(2026-08-09: 시체를 끌고 다닐 수 있었다).

**포획 알림 글자는 없앴다**(`CapturePromptUI`). 그 칸은 플레이어를 따라다니는
월드 캔버스라 무엇을 적어도 인물을 가린다 — 결과는 배낭 명패·도감이 말한다.
이벤트는 `Debug.Log` 로만 남긴다.

**기둥(방 한가운데 홀로 선 벽 칸)은 나무로 그린다.** 어두운 사각형으로 칠하면
잔디 위에 검은 네모가 뚝 떨어진 것처럼 보인다 — 충돌만 투명 타일로 남기고
그림은 `treeProps` 가 맡는다. 나무 프롭 후보도 "8방향이 전부 벽" 에서
"막히는 칸(wang 15)" 으로 넓혔다 — 경계가 어두운 사각형만 남지 않고 숲으로 읽힌다.

**"투명한 벽" 의 진짜 원인**: 팔레트 셋 다 `wallShadow`·`invisibleWall` 이
`Tile_Invisible`(알파 0)이라 막는 칸에 그림이 없었고, 벽 그림은 Ground 의
wang [15] 에 맡겨져 있었다. 그런데 **초원 시트는 [15] 가 긴 풀**이라 바닥
([0] 짧은 풀+꽃)과 색이 거의 같다 — 빈 잔디에서 막히는 것처럼 보였다.

`StageMapGenerator.SolidWallTileAt` 이 이제 콜라이더 있는 같은 그림
(`wangBlendCollidable[15]`)을 깔고 `wallTint`(0.42/0.46/0.42)로 눌러 그늘로
읽히게 한다. **`SetTileFlags(pos, TileFlags.None)` 을 먼저 불러야 색이 먹는다**
(기본 플래그가 색을 잠근다). 그림과 충돌이 같은 칸에서 나오므로 어긋날 수 없다.

## 현상수배 (2026-08-09 신규)

`WantedBoard`(정적) — 다이브 한 번에 강화 개체 한 마리. 스포너가 가장 위험한
방(Nest, 없으면 마지막 방)에 세운다. 안내판은 목장 마차의 `WorldLabel` 에
`BiomeDiveTrigger` 가 같이 찍는다(새 오브젝트를 안 세우려는 선택).

- **스탯 공식을 복사하지 않는다.** `WildSlimeAgent.Initialize(species, tier+2)`
  로 티어 배율·종 편향까지 끝낸 **뒤** `WantedBoard.Promote` 가 배율(HP ×3,
  공격 ×1.6, 방어 ×1.5)을 한 번만 더 얹고 `Initialize(instance)` 로 다시 심어
  체력바·그림을 갱신한다.
- 잡았는지는 `SlimeInstance.bossFlag` 로 안다. `CaptureTool` 이 배낭에 담기
  직전에 `ReportCaptured` 를 부른다 — 도감과 같은 규칙(추출 실패해도 인정).
- 대상은 `slime_lava` 고정. **서식지(잿벌)로 스폰을 묶지 않았다** — 다이브
  목적지가 세 바이옴을 도는 순환이라 묶으면 수배가 세 판에 한 번만 뜬다.

## 다이브 목적지 순환 (2026-08-09)

`BiomeDiveTrigger.forceDefaultBiome` 은 꺼졌다. 목적지는 **표 순서대로 돌아간다**
— 초원 → 잿벌 → 늪지 → 초원 … (사용자 결정). 처음엔 가중치 추첨이었는데 같은
곳이 연달아 나와 "안 바뀌는 것 같다" 는 인상을 줬다.

- 순서는 코드가 아니라 **`Assets/SO/BiomeCatalog.asset` 의 항목 순서**다.
  바꾸려면 그 표를 재배열한다(`BiomeDiveTrigger.BiomeInRotation`).
- 몇 번째인지는 `PlayerPrefs` 키 `dive_rotation` 에 남는다. **다이브가 실제로
  시작될 때만** 오른다(`AdvanceRotation`) — 안내판만 보고 돌아가도 순서가 안 밀린다.
- 목적지는 목장에 돌아올 때마다 한 번만 읽어 캐시한다. 마차 위 `다이브: OO`
  안내판이 이번 목적지를 미리 알려준다.
- 낙인은 이제 목적지를 끌지 않는다(순환이 그 자리를 대신한다). 낙인은 여전히
  오염 티어·돌연변이 확률에 쓰인다.
- 검증: `DiveAndShadowTests` 3건(한 바퀴 순서, 음수 인덱스 접기, 표 없을 때 기본값).

## 전투 수치

플레이어 HP 30 / 공격 8. 야생 슬라임 HP 20(tier0) / 공격 5. 티어 배율
`1 + 0.15×tier`. `defense` 필드는 존재하지만 데미지 계산에서 안 읽힘(미사용).

**공격 궤적은 월드 좌표로 놓는다**(`PlayerMeleeAttack.ShowSwingArc`, 2026-08-09).
`Player.prefab` 루트 스케일이 0.5 라 `localPosition = facing * reachOffset` 은
그림을 판정 원의 **절반 거리**(0.45)에 띄웠다 — 이펙트와 사거리가 안 맞던 원인이다.
크기는 `FitSwingArcToHitbox` 가 이미 월드 기준이라 맞았다(실측 1.2 = 판정 지름).
좌클릭 3타로 약화, 접촉 6회로 플레이어 사망. 낙인 스택 3당 티어 1, 티어 캡 5.

## 슬라임이 프롭에 끼는 문제 (2026-08-09)

`Steering.IsWall` 이 예전엔 **타일맵 콜라이더만** 벽으로 쳤다. 그래서 씬에 손으로
놓은 오브젝트(별 모양 장식, 휴식소, 교배장)에는 접선 회피가 안 걸려 슬라임이
정면으로 밀며 제자리걸음을 했다. 판정을 타입이 아니라 **"움직이지 않는가"**
로 바꿨다 — 리지드바디가 없거나 `Static` 이면 벽이다. 슬라임끼리·플레이어는
Dynamic 이라 그대로 제외된다(같이 피하게 하면 무리가 대상을 놓고 빙빙 돈다).
검증은 `Assets/Tests/PlayMode/SteeringTests.cs` 2건(정적 프롭은 꺾고, 동적
개체는 안 꺾는다).

## 진영 (2026-08-08 신규)

`IDamageable` 이 `IFactionMember` 를 상속하고 `Faction{Player,Wild}` 를 든다.
예전엔 "`IDamageable` 이면 때린다" 였다 — `ApplyDamage(양, source)` 의 source
를 아무도 안 읽어서, 동행 슬라임을 넣는 순간 동행이 플레이어를 때린다.

- **가드는 호출부가 아니라 `ApplyDamage` 구현부에 있다**(`Factions.IsFriendlyFire`).
  새 공격자가 생겨도 못 우회한다.
- **때리는 쪽이 항상 맞는 쪽은 아니다** — `PlayerMeleeAttack` 은 `IDamageable`
  이 아니라 `IFactionMember` 만 구현한다. 그래서 인터페이스를 둘로 나눴다.
- **진영을 안 밝히는 출처는 통과시킨다.** 테스트가 `ApplyDamage(x, this)` 로
  부르고, 환경 피해도 진영이 없다 — 막으면 피해가 조용히 사라져 원인을 못 찾는다.

## `WildSlimeAgent.Initialize` (2026-08-08 신규)

`Awake` 가 HP 20 을 하드코딩해 스탯을 자가 생성하고 있었다 — 스포너와 동행이
각자 `Awake` 를 고치면 충돌한다. 두 갈래로 꺼냈다:

- `Initialize(string species, int tier)` — 티어 배율 → 종 편향 순서로 스탯을
  만든다. **밖에서 다시 걸지 말 것(두 번 곱해진다).**
- `Initialize(SlimeInstance)` — 완성된 개체를 그대로 심는다.

`Awake` 가 전자를 부르므로 손배치 개체는 예전과 같다. 스포너는 `Instantiate`
직후 덮어쓴다 — **비활성 프리팹 순서 트릭이 필요없다**(`Instantiate` 가 `Awake`
를 즉시 돌리므로 "Initialize 가 먼저" 는 성립할 수 없다).

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

**게임 기준 해상도는 960×540 (16:9)** 이다 — `ProjectSettings` 의
`defaultScreenWidth/Height`(및 Web) 가 그 값이고, `resizableWindow: 0` +
`fullscreenMode: 3`(Windowed) 이라 창 크기가 고정된다(2026-08-07).

캔버스는 그와 별개로 **1920×1080 기준**이다: `CanvasScaler` 가 `ScaleWithScreenSize`,
reference resolution 1920×1080, match 0.5. UI 좌표는 전부 이 해상도 기준의
픽셀값으로 넣는다 — 예전에 800×600 짜리 `ConstantPixelSize` 였던 걸 바꿨다.
기준 해상도가 960×540 이어도 `ScaleWithScreenSize` 라 비율이 그대로 나오므로
둘이 달라도 된다. **다만 Boot 씬의 `PersistentUICanvas` 만 아직 800×600 기준이다**
— 같은 화면의 다른 캔버스보다 2.4배 크게 잡히므로 `BreedingUIPanel` 크기가
어긋나 보이면 여기다.

카메라 orthographic size 는 5 그대로다. **픽셀 퍼펙트는 지금 구조로 불가능하다** —
타일이 PPU 32, 슬라임 스트립이 PPU 60~172 로 섞여 있어 어떤 ortho 값도 전부를
정수 배율로 못 맞춘다. 맞추려면 PPU 를 하나로 통일하는 게 먼저다.

소리는 위 「애니메이션·효과음」 절에 몰아 적었다 — 클립이 어느 자산에 꽂혀
있는지 표가 거기 있다.

**EventSystem 은 Boot 에 하나뿐이다**(2026-08-08 정리). `PersistentRoot` 가
붙어 씬을 넘어가도 살아남으므로 다른 씬은 갖지 않는다 — 각자 하나씩 두면
런타임에 둘이 되고, uGUI 는 "정확히 하나" 를 요구해 입력을 한쪽만 처리한다
(버튼이 씹힌다). 모듈은 `InputSystemUIInputModule` 이다(레거시 모듈은 새
Input System 과 같이 쓰면 예외를 던진다).

**새 씬에는 EventSystem 을 넣지 말 것.** Boot 을 거치지 않고 바로 Play 하면
없지만, 그 경로는 `GameManager` 싱글턴도 없어 어차피 성립하지 않는다.

세이브 파일: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Slime\Saves\
{biome_stigma,player_roster}.json`. 런타임 검증하다 낙인/로스터에 테스트
데이터 섞이기 쉽다 — 실제 플레이 전에 확인.

**WebGL 에서는 파일 IO 를 안 탄다**(2026-08-08). 브라우저에 쓸 수 있는
파일시스템이 없다. `#if UNITY_WEBGL && !UNITY_EDITOR` 로 갈랐다 —
`SaveSystem` 은 `PlayerPrefs`(키 `save_{key}`, 쓸 때마다 `PlayerPrefs.Save`
로 즉시 내려쓴다. 탭이 예고 없이 닫힌다), `RunLogWriter` 는 `Debug.Log` 로
흘린다. 로그는 **이벤트마다** 부르는 자리라 예외가 한 번 나면 런이 통째로
멈춘다.

## 에셋 파이프라인 (PixelLab 경유)

`mcp__asset__generate_*` 계열 도구는 **`gameId` 를 반드시 명시**해야 한다
(`slime-rancher-roguelite`) — 안 주면 `default` 게임/팔레트로 새서 스타일이
어긋난다. 생성 후 `mcp__unity__import_asset` 으로 Unity 로 가져오면
**`Assets/Generated/` 에 떨어지는데 이 폴더는 `.gitignore` 대상**이다 — 그대로
두면 커밋에서 빠져 다른 사람 클론에서 참조가 깨진다. 반드시
`AssetDatabase.MoveAsset` 으로 `Assets/Resources/{Icons,Tiles,Decor,UI}/` 로
옮긴 뒤 커밋한다(GUID 보존되어 참조 안 깨짐).

## 바이옴 바닥 타일 (wang 4-corner, 2026-08-07 교체)

바닥은 **16장짜리 wang 타일셋**이다. 인덱스 규칙은
`index = NW*8 + NE*4 + SW*2 + SE*1`, 비트 1 = `upper`(두 지형 중 두 번째).
그래서 0 = 전부 lower(바닥), 15 = 전부 upper(얼룩). 근거는
`Assets/Resources/Tiles/marsh/tileset.json` 의 `corners` 필드다.

| 폴더 | 씬 | 씬이 실제로 쓰는 타일 |
|---|---|---|
| `hub` | Hub (로비) | `Tile_GroundBlend_*` |
| `biome` | Biome (초원) | `Tile_GroundBlend_*` |
| `ashfall` | BiomeAshfall (화산재) | `Tile_GroundBlend_*` |
| `marsh` | BiomeMarsh (늪지대) | **`MarshTile_*` → `wang_*.png`** |

**늪지대만 파일 이름이 다르다.** `GroundBlend_*` 만 갈면 늪지대는 안 바뀐다 —
Ground 타일맵 384칸이 전부 `MarshTile_0` 이고 그건 `wang_0.png` 를 가리킨다.

**어느 지형이 upper 인지는 시트마다 다르고, 그게 벽이 어떻게 보이는지를
정한다**(2026-08-08). 시트 파일 이름이 `A ↗ B` 꼴로 그 둘을 적어 둔다
(`Assets/Art/TileSheets/`) — `B` 가 upper 다.

| 폴더 | lower | upper |
|---|---|---|
| `hub` | 어두운 잡초밭 | **꽃 핀 밝은 초지** |
| `biome` | 밝은 풀밭 | **나무 캐노피** — 교체 대기(아래) |
| `marsh` | 흙탕물 | 진흙 둔덕 |
| `ashfall` | 용암 | 잿빛 바위 (그래서 이 시트만 `invert=True`) |

로비는 **안마당을 upper 로** 칠한다 — 반대로 하면 못 가는 바깥이 환하고
정작 목장이 어두침침해진다. 바이옴 셋은 벽이 upper 다.

**`SHEETS` 접두사에는 `↗` 뒤(upper)까지 넣는다.** lower 만으로는 못 가른다 —
로비와 초원은 upper 문장이 같고, 초원은 새 시트가 `lush grassy field ↗ lush
grassy field` 라 옛 시트(`↗ 나무`)와 lower 가 같다. 접두사가 겹치면 둘 다
매칭돼 정렬 순서에 따라 아무 시트나 적용된다.

**`-wang` 파일로는 못 만든다.** 같은 이름으로 두 장이 나오는데 인덱스 시트는
`-wang` **없는** 256×256(64px 4×4) 쪽이고, `-wang` 은 320×256 데모 배치다.
데모에서 16장을 역산해 보려 했지만 안 된다 — 5×4 중 빈 칸 3개를 뺀 17칸에서
모서리 색으로 인덱스를 뽑으면 **14종만 나오고 6·12 가 빠진다**(2026-08-08
실측). 자연스러운 지도라 0·15 가 중복되기 때문이다.

새 시트를 받았으면:
```bash
python Tools/apply_tile_sheets.py          # 256×256 시트 → 각 폴더의 16장
python Tools/apply_tile_sheets.py --demo   # 크기·PPU·0/15 구분 자체 검사
```

- 시트는 64px 칸 4×4 이고 **칸 순서가 `[13,10,4,12,6,8,0,1,11,3,2,5,15,14,9,7]`
  로 고정**돼 있다(스크립트의 `SHEET_ORDER`). 늪지대 시트의 모서리 색을
  클러스터링해 뽑은 인덱스가 위 json 의 `tiles` 배열 순서와 16/16 일치해서
  알아냈다. 색으로 자동 판정하려 들지 말 것 — 용암 시트는 바위 위로 용암이
  쏟아지는 칸이 있어 모서리 색이 거짓말을 한다.
- **PPU 는 반드시 셀 크기(64)로 고친다.** 예전 타일이 32px/PPU 32 였는데 PPU 를
  안 고치고 64px 그림만 덮으면 타일 하나가 Grid 의 2×2 칸을 먹어 지도가 깨진다.
- **화산재 시트만 인덱스를 뒤집어 넣는다**(`invert=True`). 네 씬 모두 인덱스 0 을
  바닥으로 깔았는데(384칸 중 250~308칸) 화산재 시트의 인덱스 0 만 용암이라,
  그대로 넣으면 바닥 전체가 용암이 된다.
- 같이 오는 `*-wang*.png`(320×256, 빈 칸 3개)는 데모 배치라 안 쓴다. 원본 시트는
  `Assets/Art/TileSheets/` 로 옮긴다 — Resources 에 두면 잘라낸 타일과 함께
  빌드에 두 번 들어간다.
- 충돌은 바닥이 아니라 **별도 `Walls`/`Collision` 타일맵**이 담당한다. 그래서
  인덱스를 뒤집어도 걸어다닐 수 있는 범위는 안 변한다.

Import 직후 `.meta` 기본값이 프로젝트 관례와 다르다 — 고쳐야 하는 필드:
`filterMode: 1`→`0`(Point), `spriteMode: 2`→`1`(Single),
`spriteMeshType: 1`→`0`(Full Rect, world sprite 타일링 쓸 때만 중요),
`spritePixelsToUnits: 100`→ 생성 시 넘긴 `size` 값과 동일하게(예: 32px 요청
→ PPU 32, 128px 아이콘 → PPU 128) — 안 맞추면 씬에서 크기가 어긋난다.
고친 뒤 `Unity_ManageAsset Action=Import` 로 강제 재임포트.

## "보이지 않는 벽" — Hub 안마당 경계 (2026-08-09 처리)

Hub 는 카메라가 빌지 않게 Ground 를 안마당보다 훨씬 넓게 칠하는데(아래 참고),
그 바깥이 **인덱스 0 = 평범한 풀밭 그림**이라 걸어갈 수 있어 보였다. 실제
콜라이더는 안마당 경계에서 끝난다 — 이것이 팀 QA 가 말한 보이지 않는 벽이다
(실측: 안마당 셀 `x ∈ [-8, 7]`, `y ∈ [-5, 4]`, 그 밖은 전부 `blocked=True`).

경계 한 겹에 **울타리 그림을 깔아** 눈에 보이게 했다:

- 타일 자산 `Tiles/hub/Tile_HubFence.asset` — 그림은
  `Props/trees/Structure_Fence.png`(64px/PPU 64 = 정확히 한 칸),
  `colliderType = Grid`. **Grid 여야 한다** — `Tile_Fence`(콜라이더 None)를
  그대로 쓰면 그 56칸의 충돌이 사라져 밖으로 걸어 나가진다.
- `Walls` 타일맵의 링 셀 56칸에 깔고 `TilemapRenderer.enabled = true` 로 켰다.
  같은 타일맵의 나머지 칸은 그림 없는 `Tile_HubCollision` 이라 안 보인다.
- 검증: `CompositeCollider2D.pathCount` 가 그대로 2, 안마당 안은 통과,
  바깥은 차단.
- **좌우 줄(`x = -9`, `x = 8`)에는 울타리 그림이 없다**(2026-08-09). 정면 그림을
  90° 돌려 봤지만 눕힌 것처럼 보여서, 사용자 결정으로 그림을 아예 뺐다. 대신
  **그림 없는 `Tile_HubCollision` 으로 갈아 끼워 충돌은 남겼다** — 빈 칸으로
  지우면 그 24칸으로 걸어 나간다. 남은 울타리는 위아래 줄 32칸.
  검증: `pathCount` 2, 좌·우·위 경계 `OverlapPoint` 참, 안마당 거짓.

## Hub 안마당 (2026-08-08 재시공)

바이옴 셋은 `StageMapGenerator` 가 런타임에 그리지만 **Hub 는 손배치다** —
생성기가 안 붙어 있으니 씬을 직접 고쳐야 한다.

- 걸어다니는 안마당은 셀 `x ∈ [-11, 10]`, `y ∈ [-6, 5]` (22×12).
- **Ground 는 그보다 훨씬 넓게 칠한다**: `x ∈ [-21, 20]`, `y ∈ [-12, 11]`.
  카메라가 ortho 5 / 16:9 라 반폭 8.9 인데, 안마당 끝에 서면 그만큼 더
  보인다 — 안 넓히면 화면에 아무것도 없는 회색이 든다.
- `Walls` 타일맵은 **충돌 전용**이다: `TilemapRenderer.enabled = false`,
  타일은 그림 없는 `Tiles/hub/Tile_HubCollision.asset`(`colliderType = Grid`).
  경계 그림은 Ground 의 wang 이 그린다. 확인은 `CompositeCollider2D.pathCount`
  가 2(바깥 테두리 + 안마당 구멍)인지와 `OverlapPoint` 몇 점으로 한다.
- **예전에 콜라이더가 아예 없었던 이유**: `Tile_HubWall_*` 자산이 지워졌는데
  타일맵이 그걸 계속 가리켜 76칸이 빈 참조가 됐다. 빈 참조는 그림도
  콜라이더도 안 낸다 — 씬은 멀쩡해 보이고 밖으로 걸어 나가진다.
- 휴식소(`RanchFacility`) 4개가 `Props/hub_decor/{1,2,3,4}.png` 를 쓴다
  (잿벌·늪지대·정비수조·초원 순서로 골랐다). 늘릴 때는 **씬의 것을 복제**할 것
  — `outputTable` 인스펙터 참조를 다시 꽂지 않아도 된다. `facilityId` 는
  로그에만 쓰이지만 겹치면 어느 웅덩이인지 못 읽는다.
- 바닥에 눕는 그림(웅덩이·포털)은 `sortingOrder = -5`. Y 정렬이 커스텀 축
  이라 같은 order 끼리만 겨루므로, 0 에 두면 플레이어가 웅덩이 뒤로 들어간다.

**장식은 벽 덩어리 안쪽에만 놓는다**(`StageMapGenerator`). 나무(`PaintTreeProps`)
와 돌·풀(`PaintDecor`) 둘 다 `SurroundedByWall`(8방향 전부 벽) 을 통과해야 한다
— 걸어다니는 바닥에 그림이 얹히면 밟고 지나갈 수 있는지 막힌 곳인지 구분이
안 된다. 벽 안쪽은 칸 수가 적어 `decorDensity` 를 5배 해서 쓴다.

## GitHub Pages 배포

`https://ys143112.github.io/Slime/` — `gh-pages` 브랜치 루트를 그대로 서빙한다.

**Brotli 를 켠 채로 Pages 에서 도는 이유는 `decompressionFallback` 이다.**
Pages 는 `Content-Encoding: br` 헤더를 못 붙이는데, 이 옵션을 켜면 로더가
브라우저에서 직접 푼다(파일 확장자가 `.br` 이 아니라 `.unityweb` 인 것이
그 흔적). 끄면 Pages 에서 로딩 중에 멈춘다.

```bash
# Unity 에서 Build/WebGL 로 빌드한 뒤
git worktree add --orphan -B gh-pages ../slime-pages
cp -r Build/WebGL/. ../slime-pages/ && touch ../slime-pages/.nojekyll
cd ../slime-pages && git add -A && git commit -m "chore: WebGL 빌드" && git push -f origin gh-pages
```

`Build/` 는 `.gitignore` 대상이라 본체 브랜치는 안 더러워진다. 매번 orphan
으로 다시 만들어 force push 하면 15MB 짜리 빌드가 히스토리에 쌓이지 않는다.

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
- **`Unity_ReadConsole Types=["Error"]` 는 컴파일 에러를 못 잡는다.** 스크립트
  컴파일 에러가 `Type: "Log"` 로 분류돼 나온다(2026-08-08 실측: `error CS0165`
  가 Log 로 왔다). 에러만 걸러 읽으면 "콘솔 깨끗한데 dll 이 안 갱신된다" 는
  헛다리를 짚게 된다 — **`Types=["All"]` 로 읽을 것.** 컴파일 여부는
  `Library/ScriptAssemblies/Game.Gameplay.dll` 의 수정 시각으로 확인하는 게
  가장 확실하다.
- `RunCommand` 코드는 `Unity.AI.Assistant.Agent.Dynamic.Extension.Editor`
  네임스페이스 안에 감싸여 컴파일된다. 그래서 `CompilationPipeline` 같은
  짧은 이름이 `Unity.CompilationPipeline` 로 잘못 붙는다 —
  `UnityEditor.Compilation.CompilationPipeline` 처럼 완전한 이름을 쓸 것.
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

테스트 프레임워크(`com.unity.test-framework`) 설치됨. PlayMode 테스트
`CombatTests` `PursuitTests` `BreedingPenTests` `RunSatchelTests`
`SteeringTests` `SpeciesBiasTests` `StageLayoutTests` `DiveAndShadowTests`
+ 헬퍼 3. **asmdef 는 2개 있다** —
`Assets/Scripts/Game.Gameplay.asmdef`(런타임 전부)와
`Assets/Tests/PlayMode/Game.Gameplay.PlayModeTests.asmdef`. 테스트 러너에
정상으로 뜬다. (`playModeTestRunnerEnabled: 0` 은 별개 설정이고, asmdef 가
있으므로 켤 필요 없다 — 켜면 nunit 이 플레이어 빌드에 섞인다.)

**테스트 6건이 밸런스 변경에 밀려 빨간불이다**(2026-08-09 실측, 이번 세션 변경과
무관). 기대값이 옛 수치에 박혀 있다: 플레이어 공격 8→12, 그리고 **`defense` 가
피해에서 실제로 차감된다**(위 「전투 수치」의 "미사용" 은 틀렸다 — 5 피해가 3으로
들어간다). 대상: `Test_Same_Faction_Damage_Is_Ignored`
`Test_Slime_Weakened_On_Zero_HP` `Test_Attack_Respects_Facing`
`Test_Weakened_Slime_Stops` `Test_Capture_Goes_To_Satchel`
`Test_Spawn_Points_On_Floor`(시드 0 스폰 후보가 벽에 붙음).

**런타임 스크립트는 `Assembly-CSharp` 이 아니라 `Game.Gameplay` 어셈블리다.**
오래된 프리팹의 `m_EditorClassIdentifier` 에 `Assembly-CSharp::` 가 남아
있지만 Unity 는 `m_Script` GUID 로 찾으므로 실동작에는 영향 없다.

## 이 파일 갱신 규칙

세션 끝에 구조가 바뀌었으면(새 spec 완료, 새 씬, 새 영속 싱글턴, 새로 겪은
RunCommand 함정 등) 이 파일을 그 자리에서 고친다. 오래된 정보를 남겨두는
것보다 지우는 게 낫다 — 틀린 캐시는 안 읽느니만 못하다.
