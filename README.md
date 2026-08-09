# Slime Stigma Ranch

로그라이트 몬스터 목장 시뮬레이션 (Unity 2D, URP).
플레이 → https://ys143112.github.io/Slime/

**루프**: 목장에서 준비 → 바이옴 다이브 → 야생 슬라임 포획(E) → 추출구로 귀환
→ 교배·시설 배치. 죽으면 그 런에 잡은 것은 전부 몰수된다.

## 처음 오는 사람

| 알고 싶은 것 | 볼 곳 |
|---|---|
| 게임이 뭔지 | [GAME_OVERVIEW_SHORT.md](GAME_OVERVIEW_SHORT.md) (1장) |
| 어디에 뭐가 있는지 | **[MAP.md](MAP.md)** — 저장소 표지판 |
| 코드 구조 | [Assets/Scripts/INDEX.md](Assets/Scripts/INDEX.md) |
| 자산 구조 | [Assets/INDEX.md](Assets/INDEX.md) |
| 규칙·함정 | [CLAUDE.md](CLAUDE.md) (긴 파일, 절 단위로) |

## 실행

Unity 로 열고 **`Assets/Scenes/Boot.unity` 에서 Play** 한다. 다른 씬에서 바로
Play 하면 싱글턴이 없어 동작하지 않는다. 시작 화면의 "시작하기" 를 눌러야 목장으로 들어간다.

조작: WASD 이동 · 좌클릭 공격 · E 포획 · I 보유목록 · U 교배 · B 배낭 ·
Z/X/C 목장 시설.

## 자산 파이프라인 (그림을 새로 넣었을 때)

```bash
python Tools/extract_slime_strips.py   # GIF → 스트립 PNG
python Tools/apply_tile_sheets.py      # 타일 시트 → wang 16장
```
그다음 Unity 메뉴 `SlimeRanch/Build Actor Animations`. 두 단계는 세트다 —
자세한 이유는 MAP.md 의 해당 행이 가리키는 절에 있다.

## 브랜치

기능별 `feat-*` → `dev` → `main`. 씬(`.unity`)을 건드린 브랜치를 머지한 뒤에는
오브젝트가 통째로 사라지지 않았는지 확인한다(CLAUDE.md 「씬 병합 함정」).
