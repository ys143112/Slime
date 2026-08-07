"""배우(슬라임·플레이어) GIF 를 Unity 가 읽을 수 있는 가로 스프라이트 스트립으로 편다.

Unity 의 GIF 임포터는 **첫 프레임만** 가져온다 — `.gif.meta` 의 spriteSheet 에
`..._0` 스프라이트 하나만 들어 있는 것이 그 증거다. 그래서 애니메이션을 쓰려면
프레임을 밖에서 펼쳐 줘야 한다.

출력 규칙(에디터 쪽 슬라이서가 이 규칙만 알면 된다):
  - 파일 하나 = 가로로 이어붙인 정사각 셀 N 개.
  - 셀 크기 = 이미지 높이. 프레임 수 = 너비 / 높이.
    원본 GIF 가 전부 정사각(60/92/120/172)이라 별도 메타데이터가 필요 없다.
  - 이름 = `{speciesId}__{Action}__{dir}.png`

동작 매핑은 아래 ACTION_MAP 한 곳에서만 정한다. GIF 이름이 전부
`Idle_custom-` 으로 시작해 동작 구분이 프롬프트 문구에만 남아 있기 때문에,
그 문구를 동작으로 옮기는 표가 필요하다.
"""

from __future__ import annotations

import json
import shutil
import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parent.parent
CHARACTERS = REPO / "Assets" / "Resources" / "Characters"
SRC = CHARACTERS / "slimes"
OUT = REPO / "Assets" / "Art" / "SlimeStrips"

# GIF 이름 접두사 → (speciesId, 동작). 사용자 지정 종 매핑:
#   파란=blue_slime / 방어=blue_star-filled / 무지개=colorful
#   늪지대=creature·swamp / 용암=red_cyclops
ACTION_MAP: dict[str, tuple[str, str]] = {
    "Idle_custom-The_blue_slime_compresses_its": ("slime_basic", "Idle"),
    "Idle_custom-The_blue_slime_settles_slightl": ("slime_basic", "Hit"),
    "Idle_custom-The_blue_star-filled_slime_cr": ("slime_guard", "Idle"),
    "Idle_custom-The_colorful_slime_shifts_its": ("slime_rainbow", "Idle"),
    # 120px 오렌지·초록 개체. 이름의 "creature" 만 보면 늪지대 같지만 그림은
    # colorful 과 같은 캐릭터다 — 늪지대(92px 이끼 녹색)와 다르다.
    "Idle_custom-The_creature_abruptly_leans_fo": ("slime_rainbow", "Attack"),
    "Idle_custom-The_creature_shifts_its_weight": ("slime_bog", "Idle"),
    "Idle_custom-The_swamp_creature_compresses": ("slime_bog", "Hit"),
    "Idle_custom-The_red_cyclops_slime_lunges_f": ("slime_lava", "Attack"),
    "Idle_custom-The_red_one-eyed_slime_creatu": ("slime_lava", "Idle"),
}

# GIF 이 없는 칸은 PixelLab 이 같이 내보낸 정지 8방향 PNG 로 메운다.
# 용암 슬라임 GIF 은 동서남북 4방향뿐이라 대각선 4칸이 빈다.
# 같은 칸에 GIF 이 이미 있으면 건드리지 않는다 — 움직이는 그림이 낫다.
# 경로는 Assets/Resources/Characters 기준이다.
STATIC_MAP: list[tuple[str, str, str]] = [
    ("slime_lava", "Idle", "slimes/angry_fire_red_slime_big_eye/Idle/rotations"),
    ("player", "Hit", "cute_anime_girl_farmer_b-state_of_being_attac/state_of_being_attac/rotations"),
]

# 플레이어 GIF 도 이름이 전부 `Idle_` 로 시작한다 — 슬라임과 같은 함정이다.
# `Idle_running-4-frames` 는 이동, `Idle_cross-punch` 는 공격이다.
PLAYER_MAP: dict[str, tuple[str, str]] = {
    "Idle_running-4-frames": ("player", "Move"),
    "Idle_cross-punch": ("player", "Attack"),
}

# 플레이어 대기는 64×64 8프레임 GIF 한 장이고, 프레임 하나가 방향 하나다.
# 파일명에 순서가 없어서 **그림에서 알아냈다**: 머리 영역의 피부색 비율과 좌우
# 치우침을 재면 앞모습(피부 많고 중앙) → 옆모습(중간, 한쪽으로 쏠림) →
# 뒷모습(거의 없음) 이 단조롭게 이어진다. 방향이 이름에 박힌 달리기 GIF 로
# 같은 값을 재서 대조했더니 아래 시계방향 순서와 8/8 일치했다.
PLAYER_IDLE_SHEET = "pixellab-cute-anime-girl--farmer--brown-1785933401495"
PLAYER_IDLE_ORDER = (
    "south", "south-east", "east", "north-east", "north", "north-west", "west", "south-west",
)

# 대기 시트만 캔버스가 64×64 이고 나머지 플레이어 그림은 104×104 이다.
# **인물의 픽셀 크기는 둘이 같고(높이 50 안팎) 둘 다 캔버스 중앙에 놓여 있다** —
# 그래서 확대하지 않고 104 캔버스 가운데에 그대로 붙이면 크기도 발 위치도 맞는다.
# 확대했다면 1.625배라 픽셀이 뭉갠다.
PLAYER_CANVAS = 104

# 약화(포획 가능) 상태용 "녹아내리는" 프레임을 그림에서 만들어 낸다.
# 원본은 피격 그림이고, 위에서 아래로 눌리면서 검게 탄다. 방향은 없다 —
# 컨트롤러의 Dead 는 블렌드트리가 아니라 단일 상태다.
# 배우가 월드에서 차지할 높이(유닛). PPU 는 이 값과 **보이는 실루엣 높이**로
# 정한다 — 캔버스 크기로 정하면 안 된다.
#
# 예전 규칙은 "PPU = 셀 크기" 라 캔버스가 곧 1×1 유닛이었는데, 캔버스 대비
# 실루엣 비율이 종마다 달라(파랑 30/60, 무지개 38/120) 무지개가 다른 종보다
# 36% 작게 나왔다. 실루엣을 기준으로 잡아야 실제로 크기가 맞는다.
#
# 두 값 모두 **예전 그림의 실루엣 높이 그대로**다(프리팹 스케일은 둘 다 1):
# 슬라임 0.75 = spec-002_monster 의 96px / PPU 128,
# 플레이어 1.875 = spec-007_character 의 240px / PPU 128.
TARGET_UNITS: dict[str, float] = {"player": 1.875}
TARGET_UNITS_DEFAULT = 0.75

MELT_FRAMES = 5
MELT_MIN_SCALE = 0.15
MELT_MIN_BRIGHTNESS = 0.05

DIRECTIONS = ("south", "south-west", "west", "north-west", "north", "north-east", "east", "south-east")


def frames_of(path: Path) -> tuple[list[Image.Image], list[int]]:
    """GIF 의 모든 프레임을 RGBA 로, 프레임별 지속시간(ms)과 함께 돌려준다."""
    images: list[Image.Image] = []
    durations: list[int] = []
    with Image.open(path) as gif:
        for index in range(gif.n_frames):
            gif.seek(index)
            images.append(gif.convert("RGBA"))
            durations.append(int(gif.info.get("duration", 100)))
    return images, durations


def melt_frames(source: Image.Image) -> list[Image.Image]:
    """위에서 아래로 눌리며 검게 타는 프레임들. 바닥은 원본 실루엣의 바닥에 고정한다.

    셀 아래쪽이 아니라 **알파 경계의 아래쪽**에 맞춰야 한다 — 셀 바닥에 맞추면
    그림이 원래 떠 있던 만큼 아래로 뚝 떨어진 뒤 눌리기 시작한다.
    """
    box = source.getbbox()
    if box is None:
        return [source]

    left, top, right, bottom = box
    body = source.crop(box)
    width, height = body.size
    frames: list[Image.Image] = []

    for step in range(MELT_FRAMES):
        ratio = step / max(1, MELT_FRAMES - 1)
        scale = 1.0 - (1.0 - MELT_MIN_SCALE) * ratio
        brightness = 1.0 - (1.0 - MELT_MIN_BRIGHTNESS) * ratio

        squashed = body.resize((width, max(1, round(height * scale))), Image.NEAREST)
        pixels = [
            (round(r * brightness), round(g * brightness), round(b * brightness), a)
            for (r, g, b, a) in squashed.getdata()
        ]
        squashed.putdata(pixels)

        frame = Image.new("RGBA", source.size, (0, 0, 0, 0))
        frame.paste(squashed, (left, bottom - squashed.size[1]))
        frames.append(frame)

    return frames


def write_strip(images: list[Image.Image], target: Path) -> None:
    cell = images[0].size[0]
    strip = Image.new("RGBA", (cell * len(images), cell), (0, 0, 0, 0))
    for index, frame in enumerate(images):
        strip.paste(frame, (index * cell, 0))
    target.parent.mkdir(parents=True, exist_ok=True)
    strip.save(target)


def main() -> int:
    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir(parents=True)

    manifest: dict[str, dict] = {}
    unmatched: list[str] = []

    for gif in sorted(SRC.glob("*.gif")):
        stem = gif.stem
        hit = next((k for k in ACTION_MAP if stem.startswith(k)), None)
        if hit is None:
            unmatched.append(gif.name)
            continue
        species, action = ACTION_MAP[hit]
        direction = stem[len(hit) :].lstrip("_")
        if direction not in DIRECTIONS:
            unmatched.append(gif.name)
            continue

        images, durations = frames_of(gif)
        name = f"{species}__{action}__{direction}.png"
        write_strip(images, OUT / name)
        manifest[name] = {
            "species": species,
            "action": action,
            "direction": direction,
            "frames": len(images),
            "cell": images[0].size[0],
            "frameMs": round(sum(durations) / len(durations)),
        }

    for gif in sorted(CHARACTERS.glob("*.gif")):
        stem = gif.stem
        hit = next((k for k in PLAYER_MAP if stem.startswith(k)), None)
        if hit is None:
            unmatched.append(gif.name)
            continue
        actor, action = PLAYER_MAP[hit]
        direction = stem[len(hit) :].lstrip("_")
        if direction not in DIRECTIONS:
            unmatched.append(gif.name)
            continue

        images, durations = frames_of(gif)
        name = f"{actor}__{action}__{direction}.png"
        write_strip(images, OUT / name)
        manifest[name] = {
            "species": actor,
            "action": action,
            "direction": direction,
            "frames": len(images),
            "cell": images[0].size[0],
            "frameMs": round(sum(durations) / len(durations)),
        }

    idle_sheet = CHARACTERS / f"{PLAYER_IDLE_SHEET}.gif"
    if idle_sheet.exists():
        images, _ = frames_of(idle_sheet)
        if len(images) != len(PLAYER_IDLE_ORDER):
            print(f"idle_sheet_frame_count_unexpected: {len(images)}")
            return 1
        for frame, direction in zip(images, PLAYER_IDLE_ORDER):
            canvas = Image.new("RGBA", (PLAYER_CANVAS, PLAYER_CANVAS), (0, 0, 0, 0))
            offset = (PLAYER_CANVAS - frame.size[0]) // 2
            canvas.paste(frame, (offset, offset))
            name = f"player__Idle__{direction}.png"
            write_strip([canvas], OUT / name)
            manifest[name] = {
                "species": "player",
                "action": "Idle",
                "direction": direction,
                "frames": 1,
                "cell": PLAYER_CANVAS,
                "frameMs": 100,
            }
        if idle_sheet.name in unmatched:
            unmatched.remove(idle_sheet.name)

    for species, action, relative in STATIC_MAP:
        folder = CHARACTERS / relative
        for direction in DIRECTIONS:
            source = folder / f"{direction}.png"
            name = f"{species}__{action}__{direction}.png"
            if not source.exists() or name in manifest:
                continue
            with Image.open(source) as png:
                image = png.convert("RGBA")
            write_strip([image], OUT / name)
            manifest[name] = {
                "species": species,
                "action": action,
                "direction": direction,
                "frames": 1,
                "cell": image.size[0],
                "frameMs": 100,
            }

    # 약화(=Dead 상태) 프레임. 피격 그림의 첫 프레임을 눌러 검게 태운다.
    # 피격이 없는 종은 대기 그림을 쓴다.
    for actor in sorted({entry["species"] for entry in manifest.values()}):
        source_name = next(
            (
                f"{actor}__{action}__south.png"
                for action in ("Hit", "Idle")
                if f"{actor}__{action}__south.png" in manifest
            ),
            None,
        )
        if source_name is None:
            continue

        cell = manifest[source_name]["cell"]
        with Image.open(OUT / source_name) as strip:
            first = strip.convert("RGBA").crop((0, 0, cell, cell))

        name = f"{actor}__Dead__south.png"
        frames = melt_frames(first)
        write_strip(frames, OUT / name)
        manifest[name] = {
            "species": actor,
            "action": "Dead",
            "direction": "south",
            "frames": len(frames),
            "cell": cell,
            "frameMs": 120,
            "meltedFrom": source_name,
        }

    (OUT / "strips.json").write_text(
        json.dumps(manifest, indent=2, sort_keys=True), encoding="utf-8"
    )

    # 배우별 PPU. 에디터 쪽이 읽어야 하는데 JsonUtility 는 사전을 못 다뤄서
    # `배우,ppu` 두 칸짜리 csv 로 낸다.
    lines = []
    for actor in sorted({entry["species"] for entry in manifest.values()}):
        reference = f"{actor}__Idle__south.png"
        if reference not in manifest:
            continue
        cell = manifest[reference]["cell"]
        with Image.open(OUT / reference) as strip:
            box = strip.convert("RGBA").crop((0, 0, cell, cell)).getbbox()
        if box is None:
            continue
        target = TARGET_UNITS.get(actor, TARGET_UNITS_DEFAULT)
        lines.append(f"{actor},{round((box[3] - box[1]) / target)}")
    (OUT / "ppu.csv").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("ppu: " + "  ".join(lines))

    covered = sorted({(v["species"], v["action"]) for v in manifest.values()})
    print(f"strips_written={len(manifest)} out={OUT}")
    for species, action in covered:
        count = sum(1 for v in manifest.values() if (v["species"], v["action"]) == (species, action))
        print(f"  {species:14s} {action:7s} dirs={count}")
    if unmatched:
        print("unmatched(무시됨, showcase/rotations 시트):")
        for name in unmatched:
            print(f"  {name}")
    return 0


def demo() -> None:
    """스트립 규칙(셀=높이, 프레임=너비/높이)이 실제 산출물에서 성립하는지 본다."""
    manifest = json.loads((OUT / "strips.json").read_text(encoding="utf-8"))
    assert manifest, "manifest 가 비었다"
    for name, entry in manifest.items():
        with Image.open(OUT / name) as image:
            width, height = image.size
        assert height == entry["cell"], f"{name}: 높이 {height} != 셀 {entry['cell']}"
        assert width == entry["cell"] * entry["frames"], f"{name}: 너비 규칙 위반 {width}"
        assert width % height == 0, f"{name}: 너비가 높이의 배수가 아니다"

    # 한 배우의 셀 크기는 하나여야 한다 — 섞이면 PPU 하나로는 크기를 못 맞춰
    # 상태가 바뀔 때마다 인물이 커졌다 작아진다. 대기 시트(64px)를 104 캔버스에
    # 붙이는 것이 바로 이 조건을 지키려는 것이다.
    cells: dict[str, set[int]] = {}
    for entry in manifest.values():
        cells.setdefault(entry["species"], set()).add(entry["cell"])
    for actor, sizes in sorted(cells.items()):
        assert len(sizes) == 1, f"{actor}: 셀 크기가 섞였다 {sorted(sizes)}"

    # 대기 8방향이 다 있고 서로 다른 그림인지 — 같으면 방향 매핑이 무너진 것이다.
    # 배우들의 실루엣이 실제로 같은 높이로 나오는가 — PPU 표의 존재 이유다.
    ppu = dict(
        (line.split(",")[0], int(line.split(",")[1]))
        for line in (OUT / "ppu.csv").read_text(encoding="utf-8").split()
        if line
    )
    for actor, value in sorted(ppu.items()):
        cell = next(e["cell"] for e in manifest.values() if e["species"] == actor)
        with Image.open(OUT / f"{actor}__Idle__south.png") as strip:
            box = strip.convert("RGBA").crop((0, 0, cell, cell)).getbbox()
        units = (box[3] - box[1]) / value
        target = TARGET_UNITS.get(actor, TARGET_UNITS_DEFAULT)
        assert abs(units - target) < 0.03, f"{actor}: 실루엣 {units:.3f} 유닛, 목표 {target}"
        print(f"  ppu_ok {actor:14s} ppu={value:3d} 실루엣={units:.3f} 유닛")

    idle = sorted(n for n, e in manifest.items() if e["species"] == "player" and e["action"] == "Idle")
    assert len(idle) == 8, f"플레이어 대기 방향이 8개가 아니다: {len(idle)}"
    digests = set()
    for name in idle:
        with Image.open(OUT / name) as image:
            digests.add(image.convert("RGBA").tobytes())
    assert len(digests) == 8, f"플레이어 대기 그림이 겹친다: {len(digests)}/8"

    print(f"demo_ok strips={len(manifest)} cells={ {k: sorted(v)[0] for k, v in sorted(cells.items())} }")


if __name__ == "__main__":
    if "--demo" in sys.argv:
        demo()
    else:
        raise SystemExit(main())
