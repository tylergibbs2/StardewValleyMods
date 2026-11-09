#!/usr/bin/env -S uv run --quiet python
"""How long each boss takes to kill, for a bad, an average and a good run.

A tick-level Monte Carlo of the eight main boss fights. Everything that can be read
off the code is read off the code and cited in a comment beside it; everything that
cannot - how often a player actually has their weapon on the boss rather than
dodging, how often they get hit - is a named parameter in ENGAGE and PRESSURE, in
one place, so the guesswork is visible and tunable instead of smeared through the
arithmetic.

What is taken from source:
  * boss health, contact damage, floor difficulty   BossManager, BossFloor
  * armour                                          Content/Data/Monsters.json
  * which floors hold which boss                    BossFloor.GetBossIndexForFloor
  * what the shop sells at each tier                Merchant.PopulateMerchantFloors
  * weapon stats                                    Content/Data/weapons.json
  * swing cadence and hit-invincibility             MeleeWeapon, GameLocation.damageMonster
  * the damage pipeline                             GameLocation.damageMonster + DamageMonsterPatch
  * perk, keystone and curse effects                Perks, Keystones, KeystoneTraits, Curse
  * each boss's invulnerable windows and phases     Bosses/*.cs
  * the sustain budget                              Sustain.cs

The absolute times this prints are NOT validated against real play - observed
kills run far longer than the model's, and that gap is unresolved. What it is good
for is relative structure: which boss is the wall, which keystone leads, how the
fight is lost, how multiplayer scales. Settle the absolute scale by timing real
fights and fitting ENGAGE_SCALE with --calibrate before trusting a number in
seconds.

Usage:
    tools/boss_ttk.py                       # the three-tier table, 1 player
    tools/boss_ttk.py --players 1 2 3 4     # and multiplayer
    tools/boss_ttk.py --keystones            # per-keystone breakdown
    tools/boss_ttk.py --hard                 # hard mode difficulty modifier
    tools/boss_ttk.py --trials 4000
"""
from __future__ import annotations

import argparse
import math
import random
import statistics
from dataclasses import dataclass, field, replace

TICK_MS = 1000.0 / 60.0  # the game's own tick; every duration below is in ticks


# ---------------------------------------------------------------------------
# Weapons.  Content/Data/weapons.json, restricted to what Merchant ever sells.
# Type: 0 sword, 1 dagger, 2 club, 3 defense sword.
# ---------------------------------------------------------------------------
@dataclass(frozen=True)
class Weapon:
    id: str
    name: str
    type: int
    min_dmg: int
    max_dmg: int
    knock: float
    speed: int
    precision: int
    defense: int
    crit: float
    crit_mult: float


WEAPONS: dict[str, Weapon] = {
    w.id: w
    for w in [
        Weapon("12", "Wooden Blade", 0, 3, 7, 1.0, 0, 0, 0, 0.02, 3.0),
        Weapon("11", "Steel Smallsword", 0, 4, 8, 1.0, 4, 0, 0, 0.02, 3.0),
        Weapon("21", "Crystal Dagger", 1, 4, 10, 1.0, 0, 10, 0, 0.03, 4.0),
        Weapon("43", "Pirate's Sword", 3, 8, 14, 1.0, 4, 0, 0, 0.02, 3.0),
        Weapon("1", "Silver Saber", 0, 8, 15, 1.0, 0, 1, 1, 0.02, 3.0),
        Weapon("15", "Forest Sword", 0, 8, 18, 1.0, 4, 5, 1, 0.02, 3.0),
        Weapon("6", "Iron Edge", 3, 12, 25, 1.2, -4, 0, 1, 0.02, 3.0),
        Weapon("19", "Shadow Dagger", 1, 10, 20, 0.5, 0, 0, 0, 0.04, 3.0),
        Weapon("51", "Broken Trident", 1, 15, 26, 0.5, 0, 8, 0, 0.02, 3.0),
        Weapon("5", "Bone Sword", 0, 20, 30, 0.8, 8, 5, 0, 0.02, 3.0),
        Weapon("14", "Neptune's Glaive", 3, 18, 35, 1.4, -2, 6, 2, 0.02, 3.0),
        Weapon("8", "Obsidian Edge", 0, 30, 45, 1.0, -2, 0, 0, 0.02, 3.2),
        Weapon("60", "Ossified Blade", 0, 26, 42, 1.1, -4, 0, 1, 0.02, 3.0),
        Weapon("45", "Wicked Kris", 1, 24, 30, 0.5, 0, 8, 0, 0.06, 3.0),
        Weapon("24", "Wood Club", 2, 9, 16, 1.5, -8, 0, 0, 0.02, 3.0),
        Weapon("48", "Yeti Tooth", 0, 26, 42, 1.0, 0, 0, 4, 0.02, 3.2),
        Weapon("50", "Steel Falchion", 0, 28, 46, 1.0, 8, 5, 0, 0.02, 3.4),
        Weapon("2", "Dark Sword", 0, 30, 45, 1.5, -10, 0, 0, 0.04, 3.0),
        Weapon("9", "Lava Katana", 0, 55, 64, 1.2, 0, 0, 3, 0.015, 3.5),
        Weapon("23", "Galaxy Dagger", 1, 30, 40, 1.0, 3, 0, 0, 0.02, 3.0),
        Weapon("56", "Dwarf Dagger", 1, 32, 38, 1.0, 3, 0, 6, 0.03, 3.0),
        Weapon("27", "Wood Mallet", 2, 15, 24, 1.3, -4, 1, 0, 0.02, 3.0),
        Weapon("4", "Galaxy Sword", 0, 60, 80, 1.0, 8, 0, 0, 0.02, 3.0),
        Weapon("54", "Dwarf Sword", 0, 65, 75, 1.0, 4, 0, 4, 0.02, 3.0),
        Weapon("59", "Dragontooth Shiv", 1, 40, 50, 1.0, 0, 0, 0, 0.05, 5.0),
        Weapon("61", "Iridium Needle", 1, 20, 35, 0.5, 0, 0, 0, 0.10, 7.0),
        Weapon("46", "Kudgel", 2, 27, 40, 1.6, -10, 0, 0, 0.02, 4.0),
        Weapon("28", "The Slammer", 2, 40, 55, 1.5, -12, 0, 0, 0.02, 3.0),
        Weapon("57", "Dragontooth Cutlass", 0, 75, 90, 1.0, 0, 0, 0, 0.02, 4.0),
        Weapon("29", "Galaxy Hammer", 2, 70, 90, 1.0, -4, 0, 0, 0.02, 3.0),
        Weapon("58", "Dragontooth Club", 2, 80, 100, 1.3, -8, 0, 0, 0.02, 4.0),
    ]
}

# Merchant.PopulateMerchantFloors, in order.  Index i is the shop stocked while
# Roguelike.CurrentLevel < Constants.ScalingOrder[i], so the shop the player last
# visited before the boss on floor 6i+5 is index i.  Each visit offers
# SwordsToPick = 3 of these, drawn at random.
SHOP_SWORDS: list[list[str]] = [
    ["12", "11", "21"],
    ["43", "1", "15", "6", "19", "51"],
    ["5", "14", "8", "60", "45", "24"],
    ["48", "50", "2", "9", "23", "56", "27"],
    ["2", "9", "4", "54", "59", "61", "46"],
    ["2", "4", "54", "59", "28"],
    ["4", "54", "57", "59", "61", "28"],
    ["4", "54", "57", "59", "61", "29"],
]


# ---------------------------------------------------------------------------
# Bosses.  BossManager.GetBaseHealth / GetBaseDamageToFarmer, and the armour each
# one inherits from its base monster's row in Content/Data/Monsters.json.
# ---------------------------------------------------------------------------
@dataclass(frozen=True)
class BossSpec:
    key: str
    name: str
    floor: int
    base_hp: int
    base_dmg: int
    resilience: int
    crusader: bool = False  # CrusaderEnchantment's monster list


BOSSES: list[BossSpec] = [
    # floor, hp, contact damage, armour
    BossSpec("slime", "Goobins the Gelatinous Sludge", 5, 400, 6, 0),
    BossSpec("skelly", "Arc the Skeleton Mage", 11, 900, 10, 1, crusader=True),
    BossSpec("thunder", "Thunder Kid", 17, 1150, 10, 2),
    BossSpec("dragon", "Cavrag the Dragon Prince", 23, 1200, 25, 0),
    BossSpec("bigbug", "Big Bug", 29, 2100, 25, 1),
    BossSpec("queenbee", "Queen Bee", 35, 2300, 25, 1),
    BossSpec("shadowking", "Shadow King", 41, 4200, 30, 2, crusader=True),
    BossSpec("modulo", "Modulosaurus", 47, 6000, 25, 0),  # ctor sets resilience 0
]


def hard_mode_scale(level: int, hard: bool) -> float:
    """BossFloor.HardModeScale - a quarter, ramped in over a first pass."""
    if not hard:
        return 1.0
    return 1.0 + 0.25 * min(1.0, level / 48.0)


def health_difficulty(level: int, hard: bool) -> float:
    """BossFloor.GetHealthDifficulty - health only, on a steeper curve."""
    difficulty = round(level / 24.0, 2) + 1.0
    if level > 48:
        difficulty += 0.25 * ((level - 48) / 6.0)
    return difficulty * hard_mode_scale(level, hard)


def level_difficulty(level: int, hard: bool) -> float:
    """BossFloor.GetLevelDifficulty - everything except health."""
    difficulty = round(level / 48.0, 2) + 1.0
    if level > 48:
        difficulty += 0.25 * ((level - 48) / 6.0)
    return difficulty * hard_mode_scale(level, hard)


# ---------------------------------------------------------------------------
# The two things the code cannot tell us.
#
# ENGAGE: the share of a vulnerable window in which the player's weapon is
# actually reaching the boss, rather than the player being mid-dodge, mid-reposition
# or chasing.  Ranked by how much of each fight is spent not swinging: how mobile
# the boss is, how dense its projectiles are, and how far it drags the player.
#
# PRESSURE: contact and projectile hits the boss lands per second on one player.
# Both are scaled by player skill below.
# ---------------------------------------------------------------------------
# How much of a vulnerable window the player's weapon is actually on the boss.
#
# Ranked by movement style, because that is what decides whether staying in melee
# range is work.  A walker with low Slipperiness is knocked back a couple of ticks
# and then walks back into you; a glider with high Slipperiness coasts, and
# Monster.MovePosition decays a glider's velocity at xVelocity/(Slipperiness*4) per
# tick against xVelocity/Slipperiness for everything else - so the same knockback
# carries a serpent about forty times as far as a skeleton.
#
#   boss        base monster   isGlider  Slipperiness  walks toward player
#   slime       Big Slime      no        3             yes, and a 32x32 sprite at
#                                                      Scale 3 is hard to miss
#   skelly      Skeleton       no        3             yes (PathFindController), and
#                                                      Halt()s while it throws -
#                                                      which is a vulnerable phase
#   shadowking  Shadow Shaman  no        2 (default)   yes, speed 4 then 7
#   modulo      Pepper Rex     no        2 (default)   yes, Speed + 1
#   thunder     Squid Kid      yes       8             glides, and teleports
#   bigbug      Bat            yes       24 +/- 10     glides, and leaves to lay
#   dragon      Serpent        yes       24..33        no - IsWalkingTowardPlayer is
#                                                      false; it flies a sinuous
#                                                      course at up to 7px a tick
#   queenbee    Fly            yes       12 (mod)      glides, 1-5x MovePosition
ENGAGE: dict[str, float] = {
    "slime": 0.84,
    "skelly": 0.76,
    "shadowking": 0.70,
    "modulo": 0.68,
    "thunder": 0.62,
    "bigbug": 0.56,
    "dragon": 0.50,
    "queenbee": 0.44,
}

# How often a landed blow knocks the boss clear and ends the stretch the player was
# having on it.  Read off the same Slipperiness column: a walker stops inside two
# ticks, a glider at Slipperiness 24 is still drifting a second later.  The Dragon
# is worst of all because Serpent.takeDamage also sets wasHitCounter = 500, so for
# half a second after each hit it does not re-aim at the player at all - it keeps
# whatever heading the knockback gave it.
KNOCK_DRIFT: dict[str, float] = {
    "slime": 0.05,
    "skelly": 0.05,
    "shadowking": 0.04,
    "modulo": 0.04,
    "thunder": 0.15,
    "queenbee": 0.22,
    "bigbug": 0.35,
    "dragon": 0.45,
}

PRESSURE: dict[str, float] = {  # boss hits landed per second on one player
    "slime": 0.08,
    "skelly": 0.085,
    "thunder": 0.085,
    "dragon": 0.075,
    "bigbug": 0.075,
    "queenbee": 0.08,
    "shadowking": 0.07,
    "modulo": 0.075,
}

# Farmer.takeDamage ends with currentTemporaryInvincibilityDuration = 1200ms, so
# nothing can land twice inside that however badly the player is positioned.
MERCY_TICKS = 1200.0 / TICK_MS

# Kiting.  A hurt player stops trading and backs off: they give up some of their
# damage and take a larger share off what lands on them.  Better players give up
# less damage for more relief.  (dps kept, pressure kept)
KITE = {
    "bad": (0.80, 0.80),
    "avg": (0.70, 0.55),
    "good": (0.60, 0.35),
}

# Below this share of health the player starts backing off.
KITE_THRESHOLD = 0.55

# Minions are what stops kiting working: you can walk away from one boss, not from
# a room of fliers that all track you.  This is the share of the kiting relief that
# still helps against adds.
ADD_RELIEF = 0.35

# How often one live add lands a hit, per second.  BigBugMinion carries
# moveTowardPlayerThreshold 35, so they come at the player from most of the arena.
ADD_PRESSURE = 0.016

# How long one uninterrupted stretch on the boss lasts, on average - about three
# sword swings.  The matching out-of-position stretch is derived from it and the
# engagement share, so only this one number is a guess about tempo.
ENGAGED_TICKS = 1.2 * 60

# One global multiplier on every ENGAGE value, and the only free parameter in the
# model that is not pinned to something in the source.
#
# ENGAGE is a share of *time* spent in reach, and within that time this model lands
# every swing.  Real play does not: the hitboxes are small, the bosses move, swings
# whiff and get mistimed.  That shortfall, plus anything else the engagement
# numbers are collectively wrong about, lives here - so it can be measured against
# observed kill times with --calibrate rather than guessed at again.
ENGAGE_SCALE = 1.0

# How far the weapon reaches, which decides how much of ENGAGE it can use: a
# dagger has to be touching, a club's swipe is twice the arc, and the weapons with
# AreaOfEffect 2 in weapons.json cover more ground than their type suggests.
REACH = {0: 1.00, 1: 0.86, 2: 1.06, 3: 1.00}
WIDE = {"51", "45", "2", "9", "28"}  # AreaOfEffect 2

# How much of ENGAGE a tier of player realises, and how much of PRESSURE they eat.
SKILL = {
    "bad": (0.78, 1.5),
    "avg": (1.00, 1.0),
    "good": (1.15, 0.5),
}

# Life Elixir restores the lot and the merchant stocks three; food and the spring
# make up the rest.  A heal costs about a second and a half of not swinging.
HEALS = {"bad": 2, "avg": 3, "good": 4}
HEAL_TICKS = 90


# ---------------------------------------------------------------------------
# The player
# ---------------------------------------------------------------------------
KEYSTONES = ("Volatile", "Momentum", "Spite", "LastStand", "Flurry", "Prospector")

# Curse.CurseType, less the ones with no bearing on a boss fight.
CURSES = (
    "GlassCannon", "DamageOverTime", "PlayerKnockback", "BrittleCrown",
    "MoreEnemiesLessHealth", "BombsAroundPlayer", "DoubleSpeedEveryone",
    "HealOverTime", "BuffsMorePotent", "BootsBetterImmunity",
    "BootsBetterDefense", "MoreCritsLessDamage", "CheaperMerchant", "MonsterBuffs",
)


@dataclass
class Build:
    weapon: Weapon
    forge_ruby: int = 0          # RubyEnchantment levels: +10% of base damage each
    forge_emerald: int = 0       # EmeraldEnchantment: +5 weapon speed each
    cursed_mult: float = 1.0     # Heavy 1.7 / Hollow 2.0 / Brittle 1.6
    cursed_speed: int = 0        # Heavy costs 3 speed

    keystone: str = "Momentum"
    keystone_level: int = 1
    # ordinary perks, by level
    fighter: int = 0
    brute: int = 0
    scout: int = 0
    precision: int = 0
    desperado: int = 0
    shield: int = 0
    turtle: int = 0
    leech: int = 0
    swift: int = 0
    defender: int = 0
    special_charm: int = 0
    rabbits_foot: int = 0
    reflect: int = 0

    warrior_helm: bool = False   # HatQuest WARRIOR_HELM: x1.2 damage
    squire_helm: bool = False    # x0.75 damage taken
    roots_platter: bool = False  # +3 Attack
    espresso: bool = False       # +1 Speed
    lucky_lunch: bool = False    # +3 Luck
    warrior_ring: bool = False   # Attack +10 for 5s on a kill
    vampire_ring: bool = False
    crusader: bool = False       # prismatic forge roll

    curses: frozenset[str] = frozenset()
    max_hp: int = 50
    armour: int = 0              # boots/rings defense, before the weapon's own

    # --- derived -----------------------------------------------------------
    @property
    def weapon_min(self) -> int:
        base = self.weapon.min_dmg
        return base + max(1, int(base * 0.1)) * self.forge_ruby

    @property
    def weapon_max(self) -> int:
        base = self.weapon.max_dmg
        return base + max(1, int(base * 0.1)) * self.forge_ruby

    @property
    def weapon_speed(self) -> int:
        return self.weapon.speed + 5 * self.forge_emerald - self.cursed_speed

    @property
    def luck(self) -> int:
        return self.special_charm + 3 * self.rabbits_foot + (3 if self.lucky_lunch else 0)

    @property
    def effective_max_hp(self) -> int:
        hp = self.max_hp + 25 * self.defender
        if "GlassCannon" in self.curses:
            return hp // 2
        return hp

    def ks(self, name: str) -> int:
        """The level of this keystone, or zero if the run took a different one."""
        return self.keystone_level if self.keystone == name else 0

    def trait(self, name: str) -> bool:
        """KeystoneTraits.Has - the tier that grants each trait."""
        tiers = {
            "ChainKills": ("Volatile", 2), "BlastStuns": ("Volatile", 3),
            "WiderBlast": ("Volatile", 4), "BlastBreaksRock": ("Volatile", 4),
            "StackOnHit": ("Momentum", 2), "StackedSustain": ("Momentum", 3),
            "DeeperStacks": ("Momentum", 4), "HeldStacks": ("Momentum", 4),
            "BankBlocked": ("Spite", 2), "SpiteSplash": ("Spite", 3),
            "BiggerBank": ("Spite", 4),
            "Braced": ("LastStand", 2), "Leeching": ("LastStand", 3),
            "SecondWind": ("LastStand", 4),
            "StreakGrace": ("Flurry", 2), "FlurrySweep": ("Flurry", 3),
            "LongerStreak": ("Flurry", 4), "FlurryHaste": ("Flurry", 4),
            "FreeMining": ("Prospector", 2), "BreakAdjacent": ("Prospector", 3),
            "WiderBurst": ("Prospector", 4), "BreakBoulders": ("Prospector", 4),
        }
        perk, tier = tiers[name]
        return self.ks(perk) >= tier


# ---------------------------------------------------------------------------
# Build generation: what a run of each quality is holding at each boss
# ---------------------------------------------------------------------------
def _weapon_dps_rank(w: Weapon) -> float:
    """Rough ordering only, for picking 'the best of what the shop offered'.

    Average damage over the swing interval, with the 450/n hit-invincibility floor
    applied, which is what actually decides whether a fast weapon is worth taking.
    """
    swing = swing_ticks(w.speed, w.type, 0, 0.0)
    floor_ticks = (450 / (3 if w.type == 1 else 2)) / TICK_MS
    interval = max(swing, floor_ticks)
    return ((w.min_dmg + w.max_dmg) / 2) / (interval * TICK_MS / 1000.0)


def pick_weapon(shop_tier: int, quality: str, keystone: str, rng: random.Random) -> Weapon:
    """Three swords off the shelf (MerchantFloor.SwordsToPick = 3), then a choice.

    A bad run is a floor behind - it could not afford the upgrade, or spent the gold
    on health - and takes the worst of what it saw; an average run takes the middle;
    a good run takes the best, and prefers a dagger if it is running Flurry, which is
    what FlurryDaggerStep pays it to do.
    """
    # A bad run is a shop behind from the second tier on; the floor-1 shelf is
    # 50g a sword, so nothing there is out of anybody's reach.
    tier = max(0, shop_tier - 1) if (quality == "bad" and shop_tier > 1) else shop_tier
    pool = SHOP_SWORDS[tier]
    offered = rng.sample(pool, min(3, len(pool)))
    offered.sort(key=lambda i: _weapon_dps_rank(WEAPONS[i]))

    if quality == "bad":
        # the middle of a shelf one tier behind - a run that is short of gold, not
        # one that picked the smallest number on purpose
        return WEAPONS[offered[len(offered) // 2]]
    if quality == "avg":
        return WEAPONS[offered[-1] if rng.random() < 0.4 else offered[len(offered) // 2]]

    if keystone == "Flurry":
        daggers = [i for i in offered if WEAPONS[i].type == 1]
        if daggers:
            return WEAPONS[max(daggers, key=lambda i: _weapon_dps_rank(WEAPONS[i]))]
    return WEAPONS[offered[-1]]


# Perks worth spending a shrine on for a boss fight, in the order a player who
# knows what they are doing would take them.  Perks.MaxLevel caps each at 3, or at
# 1 for the ones granted as a vanilla profession.
DAMAGE_ORDER = [
    ("brute", 1), ("fighter", 1), ("scout", 1),
    ("desperado", 3), ("precision", 3),
    ("shield", 3), ("turtle", 3), ("defender", 3), ("leech", 3),
    ("swift", 3), ("special_charm", 3), ("rabbits_foot", 3), ("reflect", 3),
]

# Everything a shrine can offer, for the runs that do not steer.
ANY_ORDER = DAMAGE_ORDER + [("scout", 1)]


def make_build(boss_index: int, quality: str, rng: random.Random,
               keystone: str | None = None) -> Build:
    """A plausible run of the given quality, as it stands at boss `boss_index`.

    boss_index counts from zero, so the boss on floor 6*i+5 has had i boss cycles
    behind it and therefore at most i shrines: RunRoute.PerCycleLimit allows one
    Shrine floor per cycle, and the first cycle is only three floors long.
    """
    shrines_available = boss_index
    taken = {"bad": 0.6, "avg": 0.8, "good": 1.0}[quality]
    shrines = int(round(shrines_available * taken))

    keystone = keystone or rng.choice(KEYSTONES)
    b = Build(weapon=WEAPONS["12"], keystone=keystone, keystone_level=0)

    # Perks.GetShrineOffer hands a keystone and nothing else until the run holds
    # one, so the first shrine is always the keystone.
    spend = shrines
    if spend > 0:
        b.keystone_level = 1
        spend -= 1

    # A good run deepens its keystone and then takes the damage perks; an average
    # one splits; a bad one is handed whatever the order put in front of it, which
    # is mostly the common perks.
    if quality == "good":
        deepen = min(3, spend)  # keystones reach 4
        b.keystone_level += deepen
        spend -= deepen
        order = DAMAGE_ORDER
    elif quality == "avg":
        deepen = min(1, spend)
        b.keystone_level += deepen
        spend -= deepen
        order = DAMAGE_ORDER
    else:
        order = ANY_ORDER[:]
        rng.shuffle(order)

    for name, cap in order:
        while spend > 0 and getattr(b, name) < cap:
            setattr(b, name, getattr(b, name) + 1)
            spend -= 1
        if spend == 0:
            break

    # One curse per perk taken (PerkShrine.TakePayment), less whatever a cauldron
    # cleansed - and a cauldron removes a random one, so it cannot be aimed.
    cleanses = {"bad": 0, "avg": 1, "good": 2}[quality]
    n_curses = max(0, min(len(CURSES), shrines) - rng.randint(0, cleanses))
    b.curses = frozenset(rng.sample(CURSES, n_curses))

    b.weapon = pick_weapon(boss_index, quality, b.keystone, rng)

    # Forges: ForgeFloor.ForgeFloorMinimumLevel 10, one per cycle, and
    # RunRoute.ForgeInterval 10 levels between the ones taken - so about one every
    # other cycle, capped at MeleeWeapon.MAX_FORGES, and lost whenever the weapon is.
    level = BOSSES[boss_index].floor
    windows = max(0, (level - 10) // 10)
    forges = {"bad": 0.35, "avg": 0.6, "good": 0.9}[quality]
    got = min(3, sum(1 for _ in range(windows) if rng.random() < forges))
    if quality == "good":
        b.forge_ruby = got  # +10% of base per level is the only one that scales
    else:
        for _ in range(got):
            if rng.random() < 0.5:
                b.forge_ruby += 1
            else:
                b.forge_emerald += 1

    # A cursed weapon is the single biggest damage multiplier in the pool, so how
    # often a run is actually holding one matters more than anything else here.
    #
    # It is not ChestFloor.CurseChance.  To hold one the run has to take a Vault
    # (one per boss cycle, and Spring shares that slot), pay the chest, have
    # PickAnyRandomAvoiding land on a sword rather than a ring, boots or food -
    # which at the late tiers is 5 of 27 items, not a coin flip - roll the 40%
    # curse, and then prefer it to what is already in hand.  Compounded over the
    # cycles so far that comes out around one run in six by the last boss, against
    # the flat 0.4 this used to assume.
    weapon_share = {1: 0.50, 2: 0.35, 3: 0.33, 4: 0.32,
                    5: 0.19, 6: 0.21, 7: 0.21, 8: 0.24}
    p_vault = {"bad": 0.25, "avg": 0.35, "good": 0.45}[quality]
    p_none = 1.0
    for cycle in range(1, boss_index + 1):
        share = weapon_share.get(min(8, cycle), 0.25)
        p_none *= 1.0 - p_vault * share * 0.4 * 0.6
    if level >= 11 and rng.random() < 1.0 - p_none:
        b.cursed_mult, b.cursed_speed = rng.choice([(1.7, 3), (2.0, 0), (1.6, 0)])
        b.forge_ruby = b.forge_emerald = 0  # a fresh weapon has nothing on it

    # Hat board at Merchant floor 18 (Constants.ScalingOrder[2]); three of seven
    # quests offered, so the +20% damage helm comes up 3/7 of the time.
    if level > 18:
        offered = rng.random() < 3 / 7
        wants = {"bad": 0.3, "avg": 0.7, "good": 1.0}[quality]
        if offered and rng.random() < wants:
            b.warrior_helm = True
        elif rng.random() < 0.3:
            b.squire_helm = True

    # Special food appears in the floor-30 shop and up (MerchantFloor index 5).
    if level >= 35:
        p = {"bad": 0.1, "avg": 0.5, "good": 0.95}[quality]
        b.roots_platter = rng.random() < p
        b.espresso = rng.random() < p
        b.lucky_lunch = rng.random() < p * 0.6
    if level >= 17:
        p = {"bad": 0.25, "avg": 0.6, "good": 0.9}[quality]
        b.warrior_ring = rng.random() < p
        b.vampire_ring = rng.random() < p * 0.7
        b.armour = rng.randint(0, 3) + (2 if quality == "good" else 0)
    if level >= 29 and rng.random() < {"bad": 0.05, "avg": 0.12, "good": 0.25}[quality]:
        b.crusader = True  # one of six prismatic rolls, and only worth it sometimes

    # Iridium Milk: (MaxHP 150 - StartingHP 50) / 25 = four, 500g each.
    milks = {"bad": 1, "avg": 2, "good": 4}[quality]
    if "GlassCannon" in b.curses:
        milks += 2  # half of a bigger number is still worth buying
    milks = min(4, max(0, milks - (1 if boss_index < 3 else 0)))
    b.max_hp = 50 + 25 * milks

    return b


# ---------------------------------------------------------------------------
# Swing cadence.  MeleeWeapon.setFarmerAnimating / doSwipe / leftClick.
# ---------------------------------------------------------------------------
def swing_ticks(weapon_speed: int, weapon_type: int, added_speed: int,
                weapon_speed_mult: float) -> float:
    """Ticks between one swing starting and the next being allowed.

    swipeSpeed = (400 - speed*40) - addedSpeed*40, then x(1 - WeaponSpeedMultiplier).
    doSwipe hands animateOnce swipeSpeed/8 x 1.3 over six frames for a sword and
    swipeSpeed/5 x 1.3 over eight for a club; a dagger animates two frames at
    swipeSpeed/4.  leftClick lets the next swing start once currentAnimationIndex
    passes 5 (0 for a dagger), which is what the fractions below are.
    """
    swipe = (400.0 - weapon_speed * 40.0) - added_speed * 40.0
    swipe *= 1.0 - weapon_speed_mult
    swipe = max(40.0, swipe)  # the animation cannot run at zero

    if weapon_type == 1:
        ms = (swipe / 4.0) * 2.0  # two frames at swipeSpeed/4
    elif weapon_type == 2:
        ms = (swipe / 5.0 * 1.3) * 6.0
    else:
        ms = (swipe / 8.0 * 1.3) * 6.0

    return ms / TICK_MS


def hit_invincible_ticks(weapon_type: int) -> float:
    """GameLocation.damageMonster: setInvincibleCountdown(450 / (isDagger ? 3 : 2)).

    Monster.invincibleCountdown is [XmlIgnore] and not in initNetFields, so it is
    local to whichever machine swung.  Two players therefore each get their own
    window on the same boss, which is why multiplayer damage stacks here rather
    than sharing one cap.
    """
    return (450.0 / (3 if weapon_type == 1 else 2)) / TICK_MS


# ---------------------------------------------------------------------------
# One player's running keystone state
# ---------------------------------------------------------------------------
@dataclass
class PlayerState:
    build: Build
    hp: int
    swing_cd: float = 0.0
    invuln: float = 0.0            # our own window on the boss
    alive: bool = True

    banked: int = 0                # Spite
    streak: int = 0                # Flurry
    grace_spent: bool = False
    momentum_stacks: int = 0
    momentum_left: float = 0.0     # ticks of MomentumDurationMs left
    flurry_haste_left: float = 0.0
    warrior_left: float = 0.0      # buff "20": Attack 10, 5000ms
    shell_cd: float = 0.0          # TurtleShell's 10 seconds
    engaged: bool = True           # weapon on the boss, rather than repositioning
    phase_left: float = 0.0
    sustain_budget: int = 6        # Sustain.BudgetPerSecond
    sustain_ticks: int = 0
    second_wind_spent: bool = False
    damage_dealt: int = 0
    mercy: float = 0.0             # temporaryInvincibilityTimer
    heals_left: int = 0
    heal_cd: float = 0.0

    @property
    def attack_stat(self) -> int:
        """Farmer.Attack is buffs.Attack and nothing else."""
        a = 0
        lvl = self.build.ks("Momentum")
        if lvl:
            a += min(self.momentum_stacks * lvl, 12)  # MomentumAttackCap
        if self.build.roots_platter:
            a += 3
        if self.warrior_left > 0:
            a += 10
        return a

    @property
    def added_speed(self) -> int:
        s = self.build.swift
        lvl = self.build.ks("Momentum")
        if lvl:
            s += min(self.momentum_stacks * lvl, 6)  # MomentumSpeedCap
        if self.build.espresso:
            s += 1
        return s

    @property
    def nearly_dead(self) -> bool:
        return self.hp <= self.build.effective_max_hp * (1 / 3)

    def heal(self, amount: int) -> int:
        """Sustain.Heal - everything hit- and kill-driven goes through the budget."""
        if amount <= 0 or self.sustain_budget <= 0:
            return 0
        allowed = min(amount, self.sustain_budget)
        self.sustain_budget -= allowed
        self.hp = min(self.hp + allowed, self.build.effective_max_hp)
        return allowed

    def tick_timers(self) -> None:
        for name in ("momentum_left", "flurry_haste_left", "warrior_left",
                     "swing_cd", "invuln", "shell_cd", "mercy", "heal_cd"):
            setattr(self, name, max(0.0, getattr(self, name) - 1.0))
        if self.momentum_left == 0.0:
            self.momentum_stacks = 0
        self.sustain_ticks += 1
        if self.sustain_ticks >= 60:  # Sustain.RefillTicks
            self.sustain_ticks = 0
            self.sustain_budget = 6


# ---------------------------------------------------------------------------
# The damage pipeline, in the order the game runs it
# ---------------------------------------------------------------------------
def roll_damage(ps: PlayerState, boss: BossSpec, rng: random.Random) -> int:
    b = ps.build
    w = b.weapon

    mn, mx = b.weapon_min, b.weapon_max

    # --- Keystones.ModifyOutgoingDamage (the Harmony prefix, so first) ---------
    weapon_max = mx  # taken before Last Stand multiplies, as the method does

    ls = b.ks("LastStand")
    if ls > 0 and ps.nearly_dead:
        mult = 1.0 + ls * 0.5  # LastStandPerLevel
        mn, mx = int(mn * mult), int(mx * mult)

    bonus = 0
    if ps.banked > 0:
        bonus += ps.banked
        ps.banked = 0

    flurry = b.ks("Flurry")
    if flurry > 0 and ps.streak > 0:
        step = 0.12 if w.type == 1 else 0.06  # FlurryDaggerStep / FlurryStep
        share = min(step * ps.streak, 1.5)    # FlurryBonusCap
        bonus += int(weapon_max * share)

    mn += bonus
    mx += bonus

    # --- DamageMonsterPatch: one crit pool, then one ceiling ------------------
    crit_power = 0.4 * b.precision + 0.8 * b.desperado
    crit_mult = min(w.crit_mult + crit_power, 6.0)

    # DoDamage's own adjustment for daggers, before the patch sees it
    crit_chance = w.crit
    if w.type == 1:
        crit_chance = (crit_chance + 0.005) * 1.12

    if "MoreCritsLessDamage" in b.curses:
        crit_chance = 0.25
        crit_mult *= 0.5

    # --- back in damageMonster -----------------------------------------------
    if b.scout:  # profession 25
        crit_chance += crit_chance * 0.5

    dmg = rng.randint(mn, mx)
    if rng.random() < crit_chance + b.luck * (crit_chance / 40.0):
        dmg = int(dmg * crit_mult)

    dmg = max(1, dmg + ps.attack_stat * 3)

    if b.fighter:  # profession 24
        dmg = math.ceil(dmg * 1.1)
    if b.brute:    # profession 26
        dmg = math.ceil(dmg * 1.15)

    # --- enchantment OnCalculateDamage hooks ---------------------------------
    if b.warrior_helm:          # WeaponStatTrack
        dmg = round(dmg * 1.2)
    if b.cursed_mult != 1.0:    # Heavy / Hollow / Brittle
        dmg = int(dmg * b.cursed_mult)
    if "GlassCannon" in b.curses:
        dmg *= 2
    if b.crusader and boss.crusader:
        dmg = int(dmg * 1.5)

    return dmg


def on_hit_landed(ps: PlayerState) -> None:
    """Keystones.OnMonsterHit, for the one blow the player actually struck."""
    b = ps.build
    if b.ks("Flurry") > 0:
        ceiling = 16 if b.trait("LongerStreak") else 10
        ps.streak = min(ps.streak + 1, ceiling)
        if b.trait("FlurryHaste") and ps.streak >= ceiling:
            ps.flurry_haste_left = 3000.0 / TICK_MS

    if b.trait("StackOnHit"):
        quicken(ps, from_kill=False)

    if b.ks("LastStand") > 0 and b.trait("Leeching") and ps.nearly_dead:
        ps.heal(2)  # LastStandLeechHeal


def quicken(ps: PlayerState, from_kill: bool) -> None:
    """Keystones.Quicken."""
    b = ps.build
    lvl = b.ks("Momentum")
    if lvl == 0:
        return
    ceiling = 8 if b.trait("DeeperStacks") else 5
    if ps.momentum_left <= 0:
        ps.momentum_stacks = 0
    was_full = ps.momentum_stacks >= ceiling
    ps.momentum_stacks = min(ps.momentum_stacks + 1, ceiling)
    ps.momentum_left = 4000.0 / TICK_MS
    if from_kill and was_full and b.trait("StackedSustain"):
        ps.heal(3)


def on_player_hurt(ps: PlayerState, got_through: int, blocked: int) -> None:
    """Keystones.OnPlayerHurt plus the Flurry streak ending."""
    b = ps.build
    if ps.streak > 0 and not ps.grace_spent and b.trait("StreakGrace"):
        ps.grace_spent = True
    else:
        ps.streak = 0
        ps.grace_spent = False

    spite = b.ks("Spite")
    if spite == 0:
        return
    taken = got_through + blocked if b.trait("BankBlocked") else got_through
    if taken <= 0:
        return
    ps.banked += taken * spite
    cap = 60 * spite  # SpiteCap
    if b.trait("BiggerBank"):
        cap *= 2
    ps.banked = min(ps.banked, cap)


def apply_incoming(ps: PlayerState, raw: int, rng: random.Random) -> None:
    """FarmerTakeDamagePatch, in order."""
    b = ps.build
    damage = raw + rng.randint(min(-1, -raw // 8), max(1, raw // 8))
    resilience = b.armour + b.weapon.defense
    if resilience >= damage * 0.5:
        resilience -= int(resilience * rng.randint(0, 2) / 10.0)

    before = damage
    damage = max(1, damage - resilience)
    blocked = max(0, before - damage)

    if b.shield:
        damage = round(damage * (0.9 ** b.shield))

    if b.ks("LastStand") > 0 and b.trait("Braced") and ps.nearly_dead:
        damage = max(1, round(damage * 0.75))  # LastStandBracedShare

    if b.turtle and ps.shell_cd == 0:
        damage = round(damage * (0.5 ** b.turtle))
        ps.shell_cd = 10 * 60

    if b.squire_helm:
        damage = round(damage * 0.75)

    if b.cursed_mult == 1.6:  # Brittle, read here rather than on the weapon
        damage = round(damage * 1.3)

    on_player_hurt(ps, damage, blocked)

    if (b.trait("SecondWind") and not ps.second_wind_spent
            and ps.hp - damage <= 0):
        ps.second_wind_spent = True
        damage = max(0, ps.hp - 1)

    if "DamageOverTime" in b.curses:
        damage = round(damage * 1.5)

    ps.hp = max(0, ps.hp - damage)
    ps.mercy = MERCY_TICKS
    if ps.hp == 0:
        ps.alive = False


def maybe_heal(ps: PlayerState) -> bool:
    """Drink or eat when it is that or die.  Costs the swings it takes."""
    if ps.heals_left <= 0 or ps.heal_cd > 0:
        return False
    if ps.hp > ps.build.effective_max_hp * 0.4:
        return False
    ps.heals_left -= 1
    ps.heal_cd = HEAL_TICKS * 2
    ps.hp = min(ps.build.effective_max_hp,
                ps.hp + int(ps.build.effective_max_hp * 0.75))
    ps.swing_cd = max(ps.swing_cd, HEAL_TICKS)
    return True


# ---------------------------------------------------------------------------
# Boss behaviour.  One class per boss; `tick` advances it and reports whether a
# blow can land this tick, by what multiplier, and how much of the player's
# attention the floor is taking.
# ---------------------------------------------------------------------------
@dataclass
class BossRuntime:
    spec: BossSpec
    difficulty: float
    hard: bool
    hp: int = 0
    max_hp: int = 0
    adds: list[int] = field(default_factory=list)   # add HP pools
    gate_hp: int = 0                                # HP that must die first
    t: float = 0.0

    def __post_init__(self) -> None:
        self.max_hp = int(self.spec.base_hp * health_difficulty(self.spec.floor, self.hard))
        self.hp = self.max_hp

    @property
    def frac(self) -> float:
        return self.hp / self.max_hp if self.max_hp else 0.0

    def dmg_to_farmer(self) -> int:
        return int(self.spec.base_dmg * self.difficulty)

    def add_damage(self) -> int:
        """What one of this boss's minions hits for.  Zero if it has none."""
        return 0

    def extra_pressure(self) -> tuple[float, int]:
        """Incoming risk that is not a live body - a volley in flight, a blast.

        Returned as (hits per second, damage per hit) for this tick only.
        """
        return 0.0, 0

    def adjust_range(self, lo: float, hi: float) -> float:
        """MonsterExtensions.AdjustRangeForHealth."""
        return min(max(lo, hi - self.frac * (hi - lo)), hi)

    # subclasses override
    def tick(self, rng: random.Random) -> tuple[bool, float]:
        return True, 1.0

    def on_hit(self, rng: random.Random) -> None:
        pass

    def add_distraction(self) -> float:
        """How much of ENGAGE the floor takes away, 0..0.45."""
        return min(0.45, 0.05 * len(self.adds))


class Slime(BossRuntime):
    """TutorialSlime: always open, but splits off a green slime on 22.5% of hits."""

    def on_hit(self, rng: random.Random) -> None:
        chance = 0.225 + (0.10 if self.hard else 0.0)
        if rng.random() < chance:
            self.adds.append(int(24 * self.difficulty))

    def tick(self, rng: random.Random) -> tuple[bool, float]:
        # MovePosition runs twice below half health
        return True, 1.0

    def add_damage(self) -> int:
        return int(6 * self.difficulty)  # split green slimes

    def add_distraction(self) -> float:
        extra = 0.08 if self.frac < 0.5 else 0.0  # double speed
        return min(0.45, 0.04 * len(self.adds) + extra)


class Skelly(BossRuntime):
    """Invincible while the freeze charges, and from half health until the mages die."""

    def __post_init__(self) -> None:
        super().__post_init__()
        self.freeze_timer = 11 * 60
        self.charging = 0.0
        self.mages_spawned = False
        self.invincible = False

    def tick(self, rng: random.Random) -> tuple[bool, float]:
        if not self.invincible:
            self.freeze_timer -= 1
            if self.freeze_timer <= 0 and self.charging == 0:
                self.charging = 80  # ChargeTicks
                self.freeze_timer = 11 * 60

        if self.charging > 0:
            self.charging -= 1
            return False, 1.0

        if not self.mages_spawned and self.hp <= self.max_hp * 0.5:
            self.mages_spawned = True
            self.invincible = True
            # five SkellyMinion mages at 50 * difficulty, at fixed spots
            self.adds = [int(round(50 * self.difficulty)) for _ in range(3)]
            self.gate_hp = sum(self.adds)

        if self.invincible and not self.adds:
            self.invincible = False

        return (not self.invincible), 1.0

    def add_damage(self) -> int:
        return 10  # SkellyMinion.DamageToFarmer, flat

    def add_distraction(self) -> float:
        # the mage phase is all travel: five spawn points around a large arena
        return 0.30 if self.invincible else 0.0


class ThunderKid(BossRuntime):
    """Never immune, but every hit while off cooldown buys a burn and a teleport.

    takeDamage goes straight through, so the boss is open all fight; what it costs
    is the ninety ticks it spends charging the burn and then the chase after
    TeleportToRandomPosition drops it somewhere else in the arena.
    """

    def __post_init__(self) -> None:
        super().__post_init__()
        self.to_burn = 0
        self.cooldown = 0
        self.chase = 0.0

    def tick(self, rng: random.Random) -> tuple[bool, float]:
        self.cooldown = max(0, self.cooldown - 1)
        self.chase = max(0.0, self.chase - 1)

        if self.to_burn > 0:
            self.to_burn -= 1
            if self.to_burn == 0:
                self.cooldown = 7 * 60  # BurnCooldown
                # it teleports away, so the next second or two is spent walking
                self.chase = rng.uniform(1.2, 2.4) * 60
        return True, 1.0

    def on_hit(self, rng: random.Random) -> None:
        if self.cooldown == 0 and self.to_burn == 0:
            self.to_burn = 90  # TicksToBurn

    def add_distraction(self) -> float:
        return 0.75 if self.chase > 0 else 0.0


class Dragon(BossRuntime):
    """Only open in Normal and mid-charge; the fire breath and the wind-up are not.

    Dragon.takeDamage returns 0 during Attacking and ChargingUp, and a hit during
    Charging cancels the charge.  Both windows scale with its health - the breath
    gets longer as it dies, the wind-up shorter - which is AdjustRangeForHealth.
    """

    NORMAL, CHARGEUP, CHARGING, ATTACKING = range(4)

    def __post_init__(self) -> None:
        super().__post_init__()
        self.state = self.NORMAL
        self.timer = 6000.0 / TICK_MS  # TimeUntilNextAttack starts at 6000
        self.prev = None

    def tick(self, rng: random.Random) -> tuple[bool, float]:
        self.timer -= 1
        if self.timer > 0:
            return self.state in (self.NORMAL, self.CHARGING), 1.0

        if self.state == self.NORMAL:
            choices = [a for a in ("fire", "charge", "random") if a != self.prev]
            attack = rng.choice(choices)
            self.prev = attack
            if attack in ("fire", "random"):
                self.state = self.ATTACKING
                self.timer = (2000 + 2500 - self.adjust_range(2000, 2500)) / TICK_MS
            else:
                self.state = self.CHARGEUP
                ms = 4000 - self.adjust_range(0, 3000)
                if self.hard:
                    ms -= 900
                self.timer = max(1.0, ms / TICK_MS)
        elif self.state == self.CHARGEUP:
            self.state = self.CHARGING
            self.timer = 5000.0 / TICK_MS
        else:
            self.state = self.NORMAL
            self.timer = (4000 - self.adjust_range(0, 2500)) / TICK_MS

        return self.state in (self.NORMAL, self.CHARGING), 1.0

    def on_hit(self, rng: random.Random) -> None:
        # takeDamage drops it back to Normal and the blow still lands, so a charge
        # costs the boss its wind-up and buys the player a fresh Normal window:
        # TimeUntilNextAttack was set when the attack launched and does not tick
        # while it is charging.
        if self.state == self.CHARGING:
            self.state = self.NORMAL
            self.timer = (4000 - self.adjust_range(0, 2500)) / TICK_MS


class BigBug(BossRuntime):
    """Halves what it takes while it is laying, and fills the arena while it does.

    The eggs are the fight.  Each one is laid with a 500-tick fuse and then hatches
    two to four BigBugMinion at 100 x difficulty health and 12 x difficulty damage;
    nothing in the mod lets the player break an egg, so the only way to stop one is
    to catch the boss in State.LayingEgg and hit it, which resets the lay.  That
    means following it to the egg spot - it flies there first, and MovingToEgg is
    120 ticks of it being nowhere near the player.

    Up to nine eggs can be down at once (EggSpawnLocations.Count), and the boss
    retries every 60 ticks once the board is full, so a player who stops
    interrupting is facing a compounding swarm rather than a boss.
    """

    NORMAL, MOVING, LAYING = range(3)

    def __post_init__(self) -> None:
        super().__post_init__()
        self.state = self.NORMAL
        self.lay_timer = 300
        self.state_ticks = 0
        self.offscreen = 180
        self.eggs: list[int] = []   # fuses, 500 ticks each
        self.eggs_laid = 0
        self.lays_stopped = 0

    def tick(self, rng: random.Random) -> tuple[bool, float]:
        self.offscreen -= 1
        if self.offscreen <= 0:
            for _ in range(min(rng.randint(1, 2), max(0, 12 - len(self.adds)))):
                self.adds.append(int(65 * self.difficulty))
            self.offscreen = rng.randint(6 * 60, 10 * 60)
            if self.hard:
                self.offscreen -= rng.randint(60, 180)

        # CheckAllEggs: every egg on the board burns down and then hatches
        for i in range(len(self.eggs) - 1, -1, -1):
            self.eggs[i] -= 1
            if self.eggs[i] <= 0:
                self.eggs.pop(i)
                n = rng.randint(2, 3) + (1 if self.hard else 0)  # Next(2, 4)
                n = min(n, 12 - len(self.adds))                  # MinionsPerPlayer
                for _ in range(max(0, n)):
                    self.adds.append(int(65 * self.difficulty))

        if self.state == self.NORMAL:
            self.lay_timer -= 1
            if self.lay_timer <= 0:
                # the board holds EggSpawnLocations.Count, and it retries fast
                if len(self.eggs) >= 9:
                    self.lay_timer = 60
                else:
                    self.state = self.MOVING
                    self.state_ticks = 0
        elif self.state == self.MOVING:
            self.state_ticks += 1
            if self.state_ticks > 120:
                self.state = self.LAYING
                self.state_ticks = 0
        else:
            self.state_ticks += 1
            if self.state_ticks > 120:
                self.eggs.append(500)       # Eggs[spot] = 500
                self.eggs_laid += 1
                self.state = self.NORMAL
                self.lay_timer = 300
                self.state_ticks = 0

        # takeDamage halves the blow whenever the state is not Normal
        mult = 1.0 if self.state == self.NORMAL else 0.5
        return True, mult

    def on_hit(self, rng: random.Random) -> None:
        # OnAttacked cancels a lay in progress - the only way to stop an egg, and
        # it means catching the boss at the egg spot it flew off to
        if self.state == self.LAYING:
            self.state = self.NORMAL
            self.lay_timer = 300
            self.state_ticks = 0
            self.lays_stopped += 1

    def add_damage(self) -> int:
        # Aggressive doubles it; the state rotates, so this is the average
        return int(12 * self.difficulty * 1.25)  # BigBugMinion damage is unchanged

    def add_distraction(self) -> float:
        away = 0.4 if self.state == self.MOVING else 0.0
        return min(0.55, 0.045 * len(self.adds) + away)


class QueenBee(BossRuntime):
    """Open all fight; the trouble is touching it at all, and the bees."""

    IDLE, STINGERS, CHARGE = range(3)

    def __post_init__(self) -> None:
        super().__post_init__()
        self.state = self.IDLE
        self.timer = 300
        self.prev = -1
        self.charges_left = 0

    def tick(self, rng: random.Random) -> tuple[bool, float]:
        if self.state != self.IDLE:
            self.timer -= 1
            if self.timer <= 0:
                if self.state == self.CHARGE and self.charges_left > 0:
                    self.charges_left -= 1
                    self.timer = (60 - self.adjust_range(0, 55)) + (30 if self.hard else 60)
                else:
                    self.state = self.IDLE
                    self.timer = 60 * 3
            return True, 1.0

        self.timer -= 1
        if self.timer > 0:
            return True, 1.0

        attack = self.prev
        while attack == self.prev:
            attack = rng.randint(0, 2)
        self.prev = attack

        if attack == 0:
            fire_rate = 16 - self.adjust_range(0, 8)
            shots = 60 * self.adjust_range(5, 10) / max(1.0, fire_rate)
            self.state = self.STINGERS
            self.timer = shots * fire_rate
        elif attack == 1:
            n = int(self.adjust_range(4, 8)) + (2 if self.hard else 0)
            n = min(n, 10 - len(self.adds))                      # BeesPerPlayer
            for _ in range(max(0, n)):
                self.adds.append(int(round(45 * self.difficulty)))
            self.state = self.IDLE
            self.timer = 60 * 5
        else:
            self.charges_left = int(self.adjust_range(3, 8))
            self.state = self.CHARGE
            self.timer = 60 - self.adjust_range(0, 55)
        return True, 1.0

    def add_damage(self) -> int:
        return int(round(15 * self.difficulty))  # QueenBeeMinion

    def add_distraction(self) -> float:
        # a charge is 13-17 px a tick straight through you: nothing to swing at
        moving = 0.35 if self.state == self.CHARGE else 0.0
        # extra MovePosition calls now fall from five to one as it dies
        moving += 0.10 * self.frac
        stingers = 0.15 if self.state == self.STINGERS else 0.0
        return min(0.55, 0.04 * len(self.adds) + moving + stingers)


class ShadowKing(BossRuntime):
    """Three phases, and a fuse lit by hitting it until the last of them."""

    VOLLEYS, SWARM, FRENZY = range(3)

    def __post_init__(self) -> None:
        super().__post_init__()
        self.phase = self.VOLLEYS
        self.since_explosion = 0
        self.to_explode = 0
        self.jinx = 0
        self.minions = 0
        self.volley_in = 0
        self.volley_left = 0
        self.volley_shots = 0

    def tick(self, rng: random.Random) -> tuple[bool, float]:
        if self.to_explode > 0:
            self.to_explode -= 1
            self.since_explosion = 0 if self.to_explode == 0 else self.since_explosion
        else:
            self.since_explosion += 1

        if self.phase == self.VOLLEYS and self.hp <= self.max_hp * 0.66:
            self.phase = self.SWARM
            self.minions = 180
        elif self.phase == self.SWARM and self.hp <= self.max_hp * 0.25:
            self.phase = self.FRENZY
            self.minions = 120

        self.volley_in = max(0, self.volley_in - 1)
        self.volley_left = max(0, self.volley_left - 1)

        if self.phase != self.VOLLEYS:
            self.minions -= 1
            if self.minions <= 0:
                # ShadowKingMinion aims for aimTime, fires, and then takes itself
                # out of the location - so a wave is a volley to dodge, never a
                # crowd to clear.  Twenty of them fire one cardinal shot each; the
                # three good ones track the player and fire three apiece.
                if self.phase == self.SWARM:
                    self.volley_shots = 20
                    self.minions = 60 * rng.randint(10, 13)
                else:
                    self.volley_shots = 9
                    self.minions = 60 * rng.randint(8, 11)
                self.volley_in = 120  # aimTime 2f

        if self.volley_in == 1:
            self.volley_left = 45  # the shots are in the air for about a second

        return True, 1.0  # never immune; takeDamage only refuses bombs

    def extra_pressure(self) -> tuple[float, int]:
        # the volley, while it is crossing the room
        if self.volley_left > 0:
            rate = self.volley_shots * 0.05
            return rate, int(18 * self.difficulty)  # Shadow Sniper damage
        # and the blast, for anyone who did not walk out of it
        if 0 < self.to_explode <= 2:
            dmg = 25 if self.phase == self.VOLLEYS else 40
            return 30.0, int(dmg * self.difficulty)
        return 0.0, 0

    def on_hit(self, rng: random.Random) -> None:
        if self.phase != self.FRENZY and self.since_explosion > 300 and self.to_explode == 0:
            self.to_explode = 120 if self.phase == self.VOLLEYS else 90

    def add_distraction(self) -> float:
        # a lit fuse is a six-tile explosion on a stationary boss: you leave
        fuse = 0.0
        if self.to_explode > 0:
            fuse = 0.55 if self.phase == self.SWARM else 0.40
        volley = 0.35 if self.volley_left > 0 else 0.0
        return min(0.7, fuse + volley)


class Modulosaurus(BossRuntime):
    """Open all fight, and the fight is the arena rather than the boss."""

    def __post_init__(self) -> None:
        super().__post_init__()
        self.timer = 300
        self.meteor = 0
        self.busy = 0

    def tick(self, rng: random.Random) -> tuple[bool, float]:
        self.meteor = max(0, self.meteor - 1)
        if self.busy > 0:
            self.busy -= 1
            return True, 1.0
        self.timer -= 1
        if self.timer > 0:
            return True, 1.0

        attack = rng.randint(0, 4)
        if attack == 1:  # meteor strike: delays up to 3100ms of explosions
            self.meteor = (3100 - (200 if self.hard else 0)) / TICK_MS
            self.busy = 120
        elif attack == 2:  # fire breath, three seconds of it
            self.busy = 3 * 60
        elif attack == 4:  # lava lurkers
            n = 4 if self.hard else 2
            self.adds.extend([220] * n)
            self.busy = 3 * 60
        else:
            self.busy = 120
        self.timer = 300
        return True, 1.0

    def add_damage(self) -> int:
        return int(15 * self.difficulty)  # LavaLurk

    def add_distraction(self) -> float:
        rain = 0.25 if self.meteor > 0 else 0.0
        return min(0.5, rain + 0.05 * len(self.adds))


RUNTIMES = {
    "slime": Slime, "skelly": Skelly, "thunder": ThunderKid, "dragon": Dragon,
    "bigbug": BigBug, "queenbee": QueenBee, "shadowking": ShadowKing,
    "modulo": Modulosaurus,
}


# ---------------------------------------------------------------------------
# One fight
# ---------------------------------------------------------------------------
# Eight minutes.  A fight that has not ended by then has run the party out of
# elixirs and patience, and in practice ends the run; it is counted as a loss.
MAX_TICKS = 60 * 60 * 8


def simulate_fight(spec: BossSpec, builds: list[Build], quality: str,
                   hard: bool, rng: random.Random) -> tuple[float, bool]:
    """Returns (seconds, lost) - lost covers both a wipe and running out of clock."""
    difficulty = level_difficulty(spec.floor, hard)
    boss = RUNTIMES[spec.key](spec=spec, difficulty=difficulty, hard=hard)

    engage_mult, pressure_mult = SKILL[quality]
    engage_base = ENGAGE[spec.key] * engage_mult * ENGAGE_SCALE
    # more players means the boss can only chase one of them, and the rest get a
    # free back; it also means the adds are split
    n = len(builds)
    engage_base = min(0.95, engage_base + min(0.15, 0.06 * (n - 1)))
    pressure = PRESSURE[spec.key] * pressure_mult / max(1.0, 1.0 + 0.35 * (n - 1))
    # Half the health means half the mistakes allowed, and people notice
    if any("GlassCannon" in b.curses for b in builds):
        pressure *= 0.8

    players = [PlayerState(build=b, hp=b.effective_max_hp,
                           heals_left=HEALS[quality]) for b in builds]
    add_share = {"bad": 0.45, "avg": 0.33, "good": 0.22}[quality]
    # How often a telegraphed burst - a volley, a lit fuse - actually lands.  All
    # of them are announced, so this is almost entirely a question of whether the
    # player reads the tell and walks out.
    dodge_fail = {"bad": 0.30, "avg": 0.12, "good": 0.04}[quality]

    def reach_of(b: Build) -> float:
        r = REACH[b.weapon.type]
        return r * (1.06 if b.weapon.id in WIDE else 1.0)

    def drift_of(b: Build) -> float:
        """How likely this weapon is to shove the boss out of reach.

        damageMonster scales the trajectory by the weapon's knockback, which
        DamageMonsterPatch doubles under the knockback curse and then caps at 2.
        A Hollow weapon has had its knockback set to zero, so it never pushes
        anything away - which on a glider is worth more than it sounds.
        """
        knock = 0.0 if b.cursed_mult == 2.0 else b.weapon.knock
        if "PlayerKnockback" in b.curses:
            knock *= 2
        return KNOCK_DRIFT[spec.key] * min(knock, 2.0)

    for tick in range(MAX_TICKS):
        hittable, mult = boss.tick(rng)
        distraction = boss.add_distraction() / max(1.0, 1.0 + 0.3 * (n - 1))
        # The floor is only here to stop a fully-distracted player stalling
        # forever; it must stay well below any plausible engagement or it clamps
        # --calibrate.
        engage = max(0.01, engage_base * (1.0 - distraction))

        adds = len(boss.adds)
        add_dmg = boss.add_damage()
        burst_rate, burst_dmg = boss.extra_pressure()

        for ps in players:
            if not ps.alive:
                continue
            ps.tick_timers()

            # The human factor.  A hurt player stops trading blows and backs off,
            # which costs them damage and saves them more of it - but only against
            # the boss.  A swarm follows, so the relief barely applies to the adds,
            # and a fight that has filled the arena is one kiting cannot answer.
            if ps.hp < ps.build.effective_max_hp * KITE_THRESHOLD:
                keep_dps, keep_press = KITE[quality]
            else:
                keep_dps = keep_press = 1.0
            relief_adds = 1.0 - (1.0 - keep_press) * ADD_RELIEF

            # incoming, from the boss and from whatever it has spawned
            if ps.mercy == 0:
                if rng.random() < pressure * keep_press / 60.0:
                    apply_incoming(ps, boss.dmg_to_farmer(), rng)
                elif add_dmg and rng.random() < (ADD_PRESSURE * adds * relief_adds
                                                 / max(1, n) / 60.0):
                    apply_incoming(ps, add_dmg, rng)
                elif burst_dmg and rng.random() < (burst_rate * dodge_fail / 60.0):
                    apply_incoming(ps, burst_dmg, rng)
                if not ps.alive:
                    continue

            if maybe_heal(ps):
                continue

            if ps.build.leech and rng.random() < 0.25 * ps.build.leech / 60.0:
                ps.heal(1)

            # Being out of position costs time, not a tick: the player is either
            # on the boss or working their way back to it, and each stretch lasts
            # about as long as it takes to read an attack and answer it.  The two
            # dwell times are set so the share of the fight spent engaged comes
            # out at `engage`.
            ps.phase_left -= 1
            if ps.phase_left <= 0:
                target = min(0.97, engage * keep_dps * reach_of(ps.build))
                ps.engaged = not ps.engaged
                mean = ENGAGED_TICKS if ps.engaged else ENGAGED_TICKS * (1 - target) / target
                ps.phase_left = max(6.0, rng.expovariate(1.0 / max(6.0, mean)))

            if not ps.engaged:
                continue
            if ps.swing_cd > 0 or ps.invuln > 0:
                continue

            haste = 0.3 if ps.flurry_haste_left > 0 else 0.0
            ps.swing_cd = swing_ticks(ps.build.weapon_speed, ps.build.weapon.type,
                                      ps.added_speed, haste)

            dmg = roll_damage(ps, boss.spec, rng)

            # a share of swings goes into the adds rather than the boss, and the
            # mage gate is the one pool that has to be cleared
            must_clear = boss.gate_hp > 0 and boss.adds
            share = min(0.9, add_share * (1.0 + 0.18 * adds))
            into_add = must_clear or (boss.adds and rng.random() < share)

            if into_add:
                ps.invuln = hit_invincible_ticks(ps.build.weapon.type)
                boss.adds[0] -= max(1, dmg - 1)
                if boss.adds[0] <= 0:
                    boss.adds.pop(0)
                    if boss.gate_hp > 0:
                        boss.gate_hp = sum(boss.adds)
                    quicken(ps, from_kill=True)
                    if ps.build.warrior_ring and rng.random() < 0.1 + ps.build.luck / 100.0:
                        ps.warrior_left = 5000.0 / TICK_MS
                    if ps.build.vampire_ring:
                        ps.heal(2)
                on_hit_landed(ps)
                continue

            if not hittable:
                continue

            ps.invuln = hit_invincible_ticks(ps.build.weapon.type)
            applied = max(1, int(dmg * mult) - spec.resilience)
            boss.hp -= applied
            ps.damage_dealt += applied
            on_hit_landed(ps)
            boss.on_hit(rng)

            # and the blow may have sent it somewhere the player now has to follow
            if rng.random() < drift_of(ps.build):
                ps.engaged = False
                ps.phase_left = max(6.0, rng.expovariate(1.0 / 24.0))

            if boss.hp <= 0:
                return (tick + 1) * TICK_MS / 1000.0, False

        if not any(p.alive for p in players):
            return (tick + 1) * TICK_MS / 1000.0, True

    return MAX_TICKS * TICK_MS / 1000.0, True


# ---------------------------------------------------------------------------
# Reporting
# ---------------------------------------------------------------------------
def mmss(seconds: float) -> str:
    return f"{int(seconds) // 60}:{int(seconds) % 60:02d}"


def swarm_report(hard: bool, seed: int) -> None:
    """How fast each spawner fills its arena if nobody answers it.

    Big Bug and the Queen are soft time limits rather than health bars: the boss
    cannot be out-lasted, only out-paced.  Nothing here kills or interrupts
    anything, so it is the worst case the fight allows.
    """
    rng = random.Random(seed)
    print()
    print("--- unopposed minion growth " + "-" * 45)
    print(f"{'boss':14s} {'add HP':>7s} {'add dmg':>8s}  "
          + "  ".join(f"{t}s" for t in (15, 30, 60, 120)))
    for key, idx in (("bigbug", 4), ("queenbee", 5), ("modulo", 7), ("slime", 0)):
        spec = BOSSES[idx]
        boss = RUNTIMES[key](spec=spec, difficulty=level_difficulty(spec.floor, hard),
                            hard=hard)
        counts, t = [], 0
        for mark in (15, 30, 60, 120):
            while t < mark * 60:
                boss.tick(rng)
                t += 1
            counts.append(len(boss.adds))
        hp = max(boss.adds) if boss.adds else 0
        print(f"{spec.name.split()[0]:14s} {hp:>7d} {boss.add_damage():>8d}  "
              + "  ".join(f"{c:>3d}" for c in counts))
    print("  nothing is killed or interrupted here: it is the pace the player"
          " has to beat")


def run(trials: int, player_counts: list[int], hard: bool, seed: int,
        per_keystone: bool, swarm: bool = False) -> None:
    rng = random.Random(seed)

    print()
    print(f"Boss time-to-kill - {trials} runs per cell"
          f"{', hard mode' if hard else ''}")
    print("Floor difficulty from BossFloor.GetLevelDifficulty; health, armour and"
          " every immune window read off the boss classes.")

    for n in player_counts:
        print()
        print(f"--- {n} player{'s' if n > 1 else ''} " + "-" * 58)
        header = (f"{'boss':30s} {'floor':>5s} {'HP':>7s} "
                  f"{'bad':>12s} {'average':>12s} {'good':>12s}   lost")
        print(header)
        print("-" * len(header))

        for i, spec in enumerate(BOSSES):
            hp = int(spec.base_hp * health_difficulty(spec.floor, hard))
            cells, wipes = [], []
            for quality in ("bad", "avg", "good"):
                kills, dead = [], 0
                for _ in range(trials):
                    builds = [make_build(i, quality, rng) for _ in range(n)]
                    t, wiped = simulate_fight(spec, builds, quality, hard, rng)
                    if wiped:
                        dead += 1
                    else:
                        kills.append(t)
                if len(kills) < max(10, trials // 20):
                    cells.append(f"{'--':>12s}")
                else:
                    lo = statistics.quantiles(kills, n=10)[0]
                    hi = statistics.quantiles(kills, n=10)[8]
                    cells.append(f"{mmss(statistics.median(kills)):>6s} "
                                 f"±{mmss((hi - lo) / 2):<5s}")
                wipes.append(dead / trials)

            print(f"{spec.name:30s} {spec.floor:>5d} {hp:>7d} "
                  + " ".join(cells)
                  + "   " + "/".join(f"{w:.0%}" for w in wipes))

        print()
        print("  cell = median kill time over the runs that won, ± half the"
              " 10th-to-90th spread")
        print("  lost = share of runs the party died or could not finish inside"
              " eight minutes")
        print("  -- = too few wins to quote a time")

    if swarm:
        swarm_report(hard, seed)

    if per_keystone:
        print()
        print("--- keystone, at the final boss, average run, 1 player " + "-" * 14)
        spec = BOSSES[-1]
        for ks in KEYSTONES:
            for quality in ("avg", "good"):
                times = []
                for _ in range(trials):
                    b = make_build(len(BOSSES) - 1, quality, rng, keystone=ks)
                    t, _ = simulate_fight(spec, [b], quality, hard, rng)
                    times.append(t)
                lvl = statistics.median(
                    [make_build(len(BOSSES) - 1, quality, rng, keystone=ks).keystone_level
                     for _ in range(50)])
                print(f"  {ks:<11s} {quality:<5s} tier {lvl:>3.0f}   "
                      f"{mmss(statistics.median(times))}")


def calibrate(targets: list[str], trials: int, hard: bool, seed: int) -> None:
    """Solve for the ENGAGE_SCALE that reproduces observed kill times.

    Each target is boss=quality=m:ss, e.g. modulo=avg=1:30.  The model's one free
    parameter is fitted to real play instead of asserted.
    """
    global ENGAGE_SCALE

    wanted = []
    for spec_text in targets:
        key, quality, clock = spec_text.split("=")
        mins, _, secs = clock.partition(":")
        seconds = int(mins) * 60 + int(secs or 0)
        idx = next(i for i, b in enumerate(BOSSES) if b.key == key)
        wanted.append((idx, quality, seconds))

    def median_for(scale: float) -> list[float]:
        global ENGAGE_SCALE
        ENGAGE_SCALE = scale
        rng = random.Random(seed)
        out = []
        for idx, quality, _ in wanted:
            kills = []
            for _ in range(trials):
                b = make_build(idx, quality, rng)
                t, lost = simulate_fight(BOSSES[idx], [b], quality, hard, rng)
                if not lost:
                    kills.append(t)
            out.append(statistics.median(kills) if kills else float("inf"))
        return out

    def error(scale: float) -> float:
        got = median_for(scale)
        return sum(math.log(g / w) for g, (_, _, w) in zip(got, wanted))

    # error is decreasing in scale: a smaller scale means less time on the boss,
    # which means longer kills and a larger residual.
    lo, hi = 0.03, 2.0
    for _ in range(16):
        mid = (lo + hi) / 2
        if error(mid) > 0:
            lo = mid
        else:
            hi = mid
    best = (lo + hi) / 2

    print()
    print(f"ENGAGE_SCALE = {best:.3f}  (fitted to {len(wanted)} observed time"
          f"{'s' if len(wanted) > 1 else ''})")
    print()
    got = median_for(best)
    print(f"{'boss':14s} {'tier':6s} {'observed':>9s} {'model':>9s}   "
          f"effective engagement")
    for (idx, quality, want), g in zip(wanted, got):
        eff = ENGAGE[BOSSES[idx].key] * SKILL[quality][0] * best
        print(f"{BOSSES[idx].key:14s} {quality:6s} {mmss(want):>9s} {mmss(g):>9s}"
              f"   {eff:.0%}")
    print()
    print("  Re-run the tables with --engage-scale "
          f"{best:.3f} to use this everywhere.")


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--trials", type=int, default=800)
    ap.add_argument("--players", type=int, nargs="+", default=[1])
    ap.add_argument("--hard", action="store_true")
    ap.add_argument("--seed", type=int, default=20261108)
    ap.add_argument("--keystones", action="store_true")
    ap.add_argument("--swarm", action="store_true",
                    help="how fast each spawner fills its arena unopposed")
    ap.add_argument("--engage-scale", type=float, default=1.0,
                    help="scale every ENGAGE value (see ENGAGE_SCALE)")
    ap.add_argument("--calibrate", nargs="+", metavar="BOSS=TIER=M:SS",
                    help="fit ENGAGE_SCALE to observed kill times, "
                         "e.g. --calibrate modulo=avg=1:30 skelly=avg=1:10")
    args = ap.parse_args()

    global ENGAGE_SCALE
    ENGAGE_SCALE = args.engage_scale

    if args.calibrate:
        calibrate(args.calibrate, args.trials, args.hard, args.seed)
        return

    run(args.trials, args.players, args.hard, args.seed, args.keystones,
        args.swarm)


if __name__ == "__main__":
    main()
