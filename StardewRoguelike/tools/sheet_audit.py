#!/usr/bin/env -S uv run --quiet --with pillow python
"""Check that every region the mod draws from a vanilla tilesheet is accounted for.

The mod overlays its own art onto a handful of vanilla sheets. If it draws from a
region it has changed but does not overlay, the game renders vanilla art there and
nothing reports a problem: that is how a vanilla clock ended up in the roguelike's
HUD, and how the gamba machine and the shop refresh icon were about to.

A draw is fine when it is either covered by an overlay, or identical to vanilla so
the overlay is irrelevant. Anything else is a bug waiting to be seen.

Usage, with the game's own art dumped from a session with the mode OFF
("rdebug dumpsheet <asset>" writes dump_off_<asset>.png into the mod folder):

    tools/sheet_audit.py <folder containing dump_off_*.png>

Re-run it after changing a sheet, an overlay rectangle, or any draw call.
"""
import sys
import pathlib
from PIL import Image

# Must mirror ContentManager.OverlaidAssets.
OVERLAYS = {
    "LooseSprites/Cursors": [
        (0, 608, 96, 96),       # shop refresh icon, then the perk icons below it
        (340, 432, 65, 17),     # the run's money box
        (128, 1184, 160, 112),  # the gamba machine
        (255, 1436, 12, 14),    # merchant backpack
    ],
    "LooseSprites/ForgeMenu": [(136, 16, 24, 24)],
    "TileSheets/Projectiles": [(0, 0, 16, 16), (48, 0, 16, 16)],
    "Maps/Festivals": [
        (32, 32, 80, 16), (464, 0, 32, 32), (320, 128, 16, 32), (400, 224, 32, 32),
    ],
}

MOD_SHEETS = {
    "LooseSprites/Cursors": "assets/TileSheets/Cursors.png",
    "LooseSprites/ForgeMenu": "assets/TileSheets/ForgeMenu.png",
    "TileSheets/Projectiles": "assets/TileSheets/Projectiles.png",
    "Maps/Festivals": "assets/Maps/Festivals.png",
}


def tile(index, sheet_width, size=16):
    cols = sheet_width // size
    return (index % cols) * size, (index // cols) * size, size, size


def perk_rects():
    """Perks.GetPerkSourceRect, for all 28 perks."""
    return [((i % 6) * 16, 624 + (i // 6) * 16, 16, 16) for i in range(28)]


# Every source rectangle the mod draws, including the computed ones. Keep in step
# with the code; grep for the sheet names to find draws this list is missing.
DRAWS = [
    ("LooseSprites/Cursors", "Merchant.cs:556 backpack", (255, 1436, 12, 14)),
    ("LooseSprites/Cursors", "DayTimeMoneyBoxPatches:53 money box", (340, 432, 65, 17)),
    ("LooseSprites/Cursors", "DrawLevelPatch.cs:38", (192, 324, 7, 10)),
    ("LooseSprites/Cursors", "GambaMenu.cs:327 machine", (128, 1184, 160, 112)),
    ("LooseSprites/Cursors", "GambaMenu.cs:328", (120, 1234, 8, 16)),
    ("LooseSprites/Cursors", "HatBoard.cs:180", (403, 373, 9, 9)),
    ("LooseSprites/Cursors", "PerkDisplayMenu.cs:113", (421, 459, 11, 12)),
    ("LooseSprites/Cursors", "PerkDisplayMenu.cs:114", (421, 472, 11, 12)),
    ("LooseSprites/Cursors", "PerkMenu.cs:271", (363, 87, 58, 22)),
    ("LooseSprites/Cursors", "SeedMenu.cs:58", (381, 361, 10, 10)),
    ("LooseSprites/Cursors", "SeedMenu.cs:69", (227, 425, 9, 9)),
    ("LooseSprites/Cursors", "StatsMenu.cs:25", (337, 494, 12, 12)),
    ("LooseSprites/Cursors", "PerkMenu:74/SeedMenu:64 ok (std tile 46)", (32, 16, 16, 16)),
    ("LooseSprites/Cursors", "RefreshableShopMenu.cs:15 refresh", (0, 608, 16, 16)),
    ("LooseSprites/ForgeMenu", "vanilla ForgeMenu draws the sheet", (136, 16, 24, 24)),
    ("TileSheets/Projectiles", "ThunderKid.cs:310 strike marker", (0, 0, 16, 16)),
    ("TileSheets/Projectiles", "Modulosaurus.cs:289 strike marker", (48, 0, 16, 16)),
]
DRAWS += [("LooseSprites/Cursors", f"Perks.GetPerkSourceRect[{i}]", r)
          for i, r in enumerate(perk_rects())]
DRAWS += [("Maps/Festivals", f"EggHunt variant {v}", tile(v, 512)) for v in range(66, 71)]
DRAWS += [("Maps/Festivals", f"PickAPath tile {t}", tile(t, 512))
          for t in (29, 30, 61, 62, 276, 308, 473, 474, 505, 506)]


def covered(sheet, x, y, w, h):
    return any(x >= ox and y >= oy and x + w <= ox + ow and y + h <= oy + oh
               for ox, oy, ow, oh in OVERLAYS.get(sheet, []))


def main(dump_dir):
    dump_dir = pathlib.Path(dump_dir)
    problems = 0
    checked = 0
    unverified = []

    for sheet, label, (x, y, w, h) in DRAWS:
        if covered(sheet, x, y, w, h):
            checked += 1
            continue

        van_path = dump_dir / ("dump_off_" + sheet.replace("/", "_") + ".png")
        mod_path = pathlib.Path(MOD_SHEETS[sheet])
        if not van_path.exists() or not mod_path.exists():
            unverified.append((sheet, label))
            continue

        van = Image.open(van_path).convert("RGBA")
        mod = Image.open(mod_path).convert("RGBA")
        diff = sum(1 for yy in range(y, min(y + h, van.height, mod.height))
                     for xx in range(x, min(x + w, van.width, mod.width))
                     if mod.getpixel((xx, yy)) != van.getpixel((xx, yy)))
        checked += 1
        if diff:
            pct = 100.0 * diff / (w * h)
            print(f"GAP  {sheet} {label} ({x},{y},{w},{h}): "
                  f"{pct:.1f}% differs from vanilla and nothing overlays it")
            problems += 1

    print(f"\n{checked} draw regions checked, {problems} gap(s)")
    if unverified:
        print(f"{len(unverified)} could not be verified (missing dump or mod sheet):")
        for sheet, label in unverified:
            print(f"   {sheet} {label}")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else "."))
