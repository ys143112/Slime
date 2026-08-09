# MAP — 저장소 표지판

**이 파일부터 읽고, 아래 지도가 가리키는 것만 연다.** 디렉터리를 통째로
훑지 말 것 — 표지판이 낡았으면 그 자리에서 고친다(맨 아래 갱신 규칙).

- 코드 한 줄 요약표: [Assets/Scripts/INDEX.md](Assets/Scripts/INDEX.md)
- 자산(씬·프리팹·SO·그림·소리) 요약표: [Assets/INDEX.md](Assets/INDEX.md)
- 프로젝트 규칙·함정 원본: [CLAUDE.md](CLAUDE.md) — **긴 파일이므로 절 단위로만 읽는다**

## 읽기 지도 (무엇을 할 때 무엇만 읽는가)

| 상황 | 읽을 파일 (이것만) |
|---|---|
| 게임이 뭔지 처음 파악 | GAME_OVERVIEW_SHORT.md (2.5KB). 더 필요하면 GAME_OVERVIEW.md |
| 어느 스크립트가 무슨 일 하는지 | Assets/Scripts/INDEX.md → 후보 파일 최대 2개만 열기 |
| 어느 자산이 어디 있는지 | Assets/INDEX.md |
| spec-00N 기능 손대기 | CLAUDE.md 「Spec → 파일 매핑」 표 → 해당 스크립트 |
| 기획 원본이 필요할 때 | `C:/dev/Game-Developer-AI/Doc/설계/plans/slime-rancher-roguelite.json` — **CLAUDE.md 와 어긋나면 기획서가 맞다** |
| 스테이지 맵 생성·스포너 | STAGE_A_DESIGN.md 해당 절 → `World/Stage*.cs`, `World/SlimeSpawner.cs` |
| 동행 슬라임·전투 수치 | WEEKEND_PLAN.md §4 → `World/CompanionAgent.cs`, `World/SpeciesPassive.cs` |
| 종·이로치 규칙 | CLAUDE.md 「슬라임 종·이로치」 → `Data/SlimeSpecies.cs`, `Systems/MutantRollService.cs` |
| 애니메이션이 안 나옴 | CLAUDE.md 「배우 애니메이션」 → Tools/extract_slime_strips.py → Unity 메뉴 `SlimeRanch/Build Actor Animations` (**이 순서**) |
| 타일·바닥 교체 | CLAUDE.md 「바이옴 바닥 타일」 → `python Tools/apply_tile_sheets.py` |
| 씬 배선·영속 싱글턴 | CLAUDE.md 「씬 · 영속 오브젝트」 → Assets/INDEX.md 씬 표 |
| Unity MCP 도구가 이상하게 굶 | CLAUDE.md 「Unity_RunCommand 함정」/「Unity_ManageGameObject 함정」 — **먼저 읽고 시도할 것** |
| 테스트 | Assets/Tests/PlayMode/ (7파일). 러너는 PlayMode 만 있다 |
| WebGL 배포 | CLAUDE.md 「GitHub Pages 배포」 |
| 브랜치·머지 | CLAUDE.md 「씬 병합 함정」 + WEEKEND_PLAN.md §8 |

## 문서 지도 (루트 md 6개)

| 파일 | 무엇 | 언제 읽나 |
|---|---|---|
| MAP.md | 이 표지판 | 항상 먼저 |
| CLAUDE.md (42KB) | 규칙·함정·구조 캐시 | 절 단위로만 |
| GAME_OVERVIEW_SHORT.md | 게임 요약 1장 | 맥락 파악 |
| GAME_OVERVIEW.md | 게임 개요 전체 | 시스템 설명 필요할 때 |
| STAGE_A_DESIGN.md (52KB) | 스테이지 생성 설계서 | 맵 생성 손댈 때, 해당 절만 |
| WEEKEND_PLAN.md | 작업 분담·부록 A 스크립트 연관 지도 | 분담·의존 확인 |

## 디렉터리 지도

| 경로 | 내용 | 열어도 되나 |
|---|---|---|
| `Assets/Scripts/` | 런타임 코드 70파일, 어셈블리 `Game.Gameplay` | INDEX.md 경유로 |
| `Assets/Editor/` | 애니메이터 생성 메뉴 2개 | 애니 파이프라인 작업 때만 |
| `Assets/Tests/PlayMode/` | PlayMode 테스트 7파일 | 테스트 작업 때 |
| `Assets/Scenes/` | Boot / Hub / Biome×3 (+SampleScene 미사용) | 텍스트로 열지 말 것(수만 줄) — MCP 로 조작 |
| `Assets/Prefabs/`, `Assets/SO/` | 프리팹 7, ScriptableObject 12 | Assets/INDEX.md 경유 |
| `Assets/Resources/` | 런타임 `Resources.Load` 대상 (타일 264, 프롭 106, 아이콘 13 …) | 목록만, 파일 내용 금지 |
| `Assets/Art/` | 원본 GIF·시트·잘라낸 스트립 | 파이프라인 작업 때만 |
| `Assets/Animations/` | 컨트롤러 7 + 클립 114 (**생성물**) | 손으로 고치지 말 것 |
| `Assets/Generated/` | MCP 가 떨군 임시 자산 — **.gitignore 대상** | Resources 로 옮긴 뒤 커밋 |
| `Tools/` | 파이썬 파이프라인 2개 | 실행만 (`--demo` 로 자체 검사) |
| `Library/`, `Temp/`, `Logs/`, `UserSettings/` | Unity 캐시 | **절대 열지 말 것** |

## 읽기 규율 (토큰 예산)

1. 지도에 없는 파일은 열지 않는다. 파일 확인은 INDEX 표 먼저, 본문은 작업당 최대 2개.
2. `.unity` / `.prefab` / `.asset` 을 텍스트로 통째 읽지 않는다 — Unity MCP 도구로 조회한다.
3. `Library/` `Temp/` 는 검색 대상에서도 뺀다.
4. 생성물(`Assets/Animations/`, `Assets/Art/SlimeStrips/`, `Assets/Resources/Tiles/`)은 손으로 고치지 않는다. 원본과 스크립트를 고치고 다시 돌린다.
5. 도구가 실패하면 최대 두 번까지만 진단 시도. 안 되면 멈추고 상황을 보고한다.

## 갱신 규칙

스크립트를 추가·삭제·이름 변경하면 `Assets/Scripts/INDEX.md`, 자산 구조가
바뀌면 `Assets/INDEX.md`, 새 문서·새 파이프라인이 생기면 이 파일을 **같은
커밋에서** 고친다. 틀린 표지판은 없는 것만 못하다.
