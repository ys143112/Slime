"""새 wang 타일 시트(256×256)를 잘라 각 바이옴의 GroundBlend_0..15.png 를 갈아 끼운다.

## 시트 배치를 어떻게 알아냈나

시트는 64px 칸 4×4 = 16장이고, 칸 순서는 `SHEET_ORDER` 로 고정돼 있다.
근거는 `Assets/Resources/Tiles/marsh/tileset.json` 이다 — 늪지대 시트의 각 칸에서
네 모서리 색을 클러스터링해 wang 인덱스를 뽑았더니 그 json 의 `tiles` 배열
순서와 16/16 일치했다(16! 분의 1이라 우연이 아니다). 네 시트가 같은 생성기의
같은 크기 산출물이므로 배치도 같다.

인덱스 규칙(같은 json 에서 읽음): `index = NW*8 + NE*4 + SW*2 + SE*1`,
비트 1 = `upper`(두 지형 중 두 번째). 그래서 인덱스 0 은 전부 lower,
15 는 전부 upper 다.

## 화산재만 뒤집는 이유

씬에 실제로 칠해진 인덱스를 세어 보면 네 씬 모두 **인덱스 0 이 압도적**이다
(384칸 중 250~308칸) — 0 이 바닥이고 15 는 얼룩이다. 새 시트의 인덱스 0 은
로비=풀, 초원=풀, 늪지대=흙탕물로 바닥에 맞지만 **화산재만 용암**이다.
그대로 끼우면 바닥 308칸이 전부 용암이 된다. 그래서 화산재만 15-i 로 뒤집어
기존에 칠해 둔 지도의 의미를 지킨다.

## PPU

기존 타일은 32px/PPU 32 = 1유닛. 새 타일은 64px 이라 PPU 도 64 로 고쳐야
Grid 의 1×1 칸에 맞는다 — 안 고치면 타일 하나가 2×2 유닛이 되어 지도가 깨진다.
"""

from __future__ import annotations

import re
import shutil
import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parent.parent
TILES = REPO / "Assets" / "Resources" / "Tiles"
SHEET_ARCHIVE = REPO / "Assets" / "Art" / "TileSheets"

CELL = 64
SHEET_ORDER = [13, 10, 4, 12, 6, 8, 0, 1, 11, 3, 2, 5, 15, 14, 9, 7]

# 시트 파일명 접두사 → (바이옴 폴더, 인덱스를 뒤집을지, 덮어쓸 파일 이름들)
#
# 늪지대만 파일 이름이 둘인 이유: BiomeMarsh 의 Ground 타일맵은 384칸 전부를
# `MarshTile_0` 으로 칠했고 그건 `GroundBlend_*` 가 아니라 `wang_*` 를 가리킨다.
# 나머지 세 씬은 `Tile_GroundBlend_*` 를 쓴다. 한쪽만 갈면 늪지대는 안 바뀐다.
SHEETS: dict[str, tuple[str, bool, tuple[str, ...]]] = {
    "A backdrop of cool grasslands": ("hub", False, ("GroundBlend_{index}.png",)),  # 로비
    "A space where the ash of blazing lava": ("ashfall", True, ("GroundBlend_{index}.png",)),  # 화산재
    "lush grassy field": ("biome", False, ("GroundBlend_{index}.png",)),  # 초원
    "Swampy, muddy water": ("marsh", False, ("GroundBlend_{index}.png", "wang_{index}.png")),  # 늪지대
}


def sheet_for(path: Path) -> tuple[str, bool, tuple[str, ...]] | None:
    # "-wang" 은 같이 나온 데모 배치(320×256, 빈 칸 3개)라 인덱스 시트가 아니다.
    if "-wang" in path.stem:
        return None
    for prefix, target in SHEETS.items():
        if path.stem.startswith(prefix):
            return target
    return None


def find_sheets() -> list[Path]:
    """원본 시트를 찾는다. 한 번 돌리면 Resources 밖으로 옮겨두므로 두 곳을 본다."""
    for folder in (TILES, SHEET_ARCHIVE):
        found = [p for p in sorted(folder.glob("*.png")) if sheet_for(p) is not None]
        if found:
            return found
    return []


def set_ppu(meta: Path, ppu: int) -> bool:
    text = meta.read_text(encoding="utf-8")
    patched, count = re.subn(r"(\n  spritePixelsToUnits: )\d+", rf"\g<1>{ppu}", text, count=1)
    if count and patched != text:
        meta.write_text(patched, encoding="utf-8", newline="")
        return True
    return False


def main() -> int:
    sheets = find_sheets()
    if not sheets:
        print(f"sheets_missing: 0장 찾음, {len(SHEETS)}장 중 매칭되는 게 없다")
        return 1

    SHEET_ARCHIVE.mkdir(parents=True, exist_ok=True)

    for sheet in sheets:
        folder_name, invert, patterns = sheet_for(sheet)
        folder = TILES / folder_name
        if not folder.is_dir():
            print(f"folder_missing: {folder}")
            return 1

        image = Image.open(sheet).convert("RGBA")
        if image.size != (CELL * 4, CELL * 4):
            print(f"sheet_size_unexpected: {sheet.name} {image.size}")
            return 1

        written = 0
        for position, index in enumerate(SHEET_ORDER):
            row, column = divmod(position, 4)
            target_index = 15 - index if invert else index
            tile = image.crop(
                (column * CELL, row * CELL, (column + 1) * CELL, (row + 1) * CELL)
            )
            for pattern in patterns:
                target = folder / pattern.format(index=target_index)
                if not target.exists():
                    print(f"tile_missing: {target}")
                    return 1
                tile.save(target)
                set_ppu(target.with_suffix(".png.meta"), CELL)
                written += 1

        # 원본 시트는 Resources 밖으로 뺀다 — 그대로 두면 잘라낸 타일과 함께
        # 빌드에 두 번 들어간다. .meta 도 같이 옮겨야 GUID 가 안 바뀐다.
        for suffix in (".png", ".png.meta"):
            source = sheet.with_suffix(suffix)
            if source.exists():
                shutil.move(str(source), str(SHEET_ARCHIVE / source.name))

        print(f"{folder_name:8s} tiles={written} invert={invert} <- {sheet.name[:44]}")

    # 같이 온 데모 배치(-wang)도 Resources 밖으로.
    for extra in sorted(TILES.glob("*-wang*.png")):
        for suffix in (".png", ".png.meta"):
            source = extra.with_suffix(suffix)
            if source.exists():
                shutil.move(str(source), str(SHEET_ARCHIVE / source.name))

    print(f"sheets_archived -> {SHEET_ARCHIVE}")
    return 0


def demo() -> None:
    """끼운 결과가 규칙을 지키는지 본다: 64×64 이고 meta PPU 가 64 이고,
    인덱스 0 과 15 는 (모서리가 전부 같은 지형이라) 서로 확연히 다른 그림이다."""
    for folder_name, _, patterns in SHEETS.values():
        folder = TILES / folder_name
        for index in range(16):
            for pattern in patterns:
                tile = folder / pattern.format(index=index)
                assert tile.exists(), f"{tile} 없음"
                with Image.open(tile) as image:
                    assert image.size == (CELL, CELL), f"{tile}: {image.size}"
                meta = tile.with_suffix(".png.meta").read_text(encoding="utf-8")
                assert f"spritePixelsToUnits: {CELL}" in meta, f"{tile}: PPU 안 고쳐짐"

        def mean(index: int) -> tuple[float, float, float]:
            with Image.open(folder / f"GroundBlend_{index}.png") as image:
                data = list(image.convert("RGB").getdata())
            return tuple(sum(p[i] for p in data) / len(data) for i in range(3))

        low, high = mean(0), mean(15)
        distance = sum((low[i] - high[i]) ** 2 for i in range(3)) ** 0.5
        assert distance > 20, f"{folder_name}: 인덱스 0/15 가 너무 비슷하다 ({distance:.1f})"
        print(f"demo_ok {folder_name} idx0={tuple(round(v) for v in low)} "
              f"idx15={tuple(round(v) for v in high)} dist={distance:.1f}")


if __name__ == "__main__":
    if "--demo" in sys.argv:
        demo()
    else:
        raise SystemExit(main())
