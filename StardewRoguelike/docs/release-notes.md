[← back to readme](README.md)

# Release notes

## 1.4.0

Released on 9 November 2026 for SMAPI 4.0.0 or later.

This release gives a run something to build towards. Perks can be taken
more than once, six keystone perks change what you do rather than how
much of it, and the shrine names both the perk and the curse it costs
before you commit. The map tab draws the run you have actually taken.

- Add: Keystone perks. Six of them, and a run with none is offered nothing else at its first shrine. Each answers to something different: killing, a run of kills, being hit, being nearly dead, going untouched, breaking rock.
- Add: Perks have levels. Most can be taken three times, keystones four, and each level of a keystone past the first adds something it did not do before.
- Add: The map tab shows the run. A floor to a line, the forks each exit offered, which one you took, and the perk or curse each floor gave you. Press M.
- Add: Three combat floors. A horde of one-hit slimes against the clock, a quarry of rock that wakes something every few stones, and a countdown of waves you choose when to leave.
- Add: The cauldron floor. Clear it, then choose a curse for the pot to take.
- Add: The exit names what a shrine is holding, a floor before you reach it.
- Add: Each perk at a shrine carries its own price. The two sides are two bargains, not one choice and then a dice roll.
- Adjust: A shrine offers one perk you do not have and one you can take further.
- Adjust: The first exit that offers a choice always offers a shrine. You can still walk past it.
- Adjust: A boss cycle may hold two combat floors. Everything else is still once a cycle.
- Adjust: The chests, the gambling chests and the two doors can be reached again. They are treasure floors now rather than combat ones.
- Adjust: A treasure chest holds something worth having and charges for it — a curse, a third of your health, or gold. The exit names the price a floor ahead.
- Adjust: The cauldron must have its floor cleared, and stands in the middle of it. The pot is full when it will take a curse and empty when it will not.
- Adjust: The merchant floors no longer sell curses.
- Adjust: Hard mode is worth more the deeper a run goes, instead of fading out by the end.
- Adjust: A boss has its own health curve. The eighth boss of a run is no longer easier than the first.
- Adjust: The Hivemother and the Queen Bee cap their swarms, by player. Falling behind no longer makes you fall behind faster.
- Adjust: The combat floors only spawn monsters that come to you.
- Adjust: Cave bugs no longer appear. They walk in a line, never notice you, and an armoured one cannot be hurt without the right enchantment.
- Adjust: Monsters on a timed floor are spread over the whole map rather than gathered at one edge.
- Adjust: A wave on the race floor arrives near each player.
- Adjust: Flurry is drawn as a dagger.
- Adjust: The statistics screen is laid out in two columns, with icons and a seed.
- Fix: The shop charged for one and handed over the whole shelf. Buying one iridium milk bought every milk the run would sell you. Seeds, food and the special foods were all wrong the same way.
- Fix: A combat floor could never end. A duggy spawned on the prairie king floor stays underground, cannot be hurt, and still counts as alive.
- Fix: Journey of the Prairie King and the race floor sent you walking. Monsters spawned at one edge of the map, and some of them never moved at all.
- Fix: The Dragon and the Shadow King became harder to kill as they died, by holding the windows where they cannot be hit for longer.
- Fix: A bug egg can be squashed before it hatches.
- Fix: In split-screen, a monster's mercy window was shared, so two players at one monster lost most of their damage.
- Fix: In split-screen, the shrine, the cauldron and the fortune teller could each be used once between both players rather than once each.
- Fix: In split-screen, run statistics counted both players as one, the minigame winnings went to whoever quit first, and a keystone's state was shared.
- Fix: Lightning from Telesto the Full Moon put one player's hit through another player's armour.
- Fix: Healing from hitting and killing is capped. A vampire ring and a sweeping keystone together healed more than a crowd could take off you.
- Fix: A critical hit could be worth nine times a normal one. Precision and Desperado multiplied each other.
- Fix: Spite's banked damage is emptied on a new floor. It could be filled on a quiet floor and spent on a boss.
- Fix: Momentum gave five speed and five attack at its first level, which is five times the strongest buff in the game.

## 1.3.0

Released on 8 November 2026 for SMAPI 4.0.0 or later.

This release makes the way down a decision. The ladder asks where to go
next, and the floors it offers are different from each other. It also
corrects faults that stopped the buff bar, the boss rewards and the
forge.

- Add: The exit asks where to go next. It offers three kinds of floor and tells you what each one gives. You can close it and keep playing the floor.
- Add: The shrine floor. Clear the floor, then give a curse to choose a perk.
- Add: The Dark Mineshaft floor. It has no light, more stone, much more of the stone holds gems, and more monsters.
- Add: The command `rdebug forcefloor`. It makes every floor one kind.
- Adjust: A boss cycle does not offer the same kind of floor twice. It also holds one quiet floor only: a hot spring or an unguarded chest, not both.
- Adjust: The eight combat floors come in a random order. All eight come before any of them comes again.
- Adjust: The merchant floors no longer sell perks, and Gil no longer speaks. Perks come from the shrine.
- Adjust: A shrine floor must be cleared before you can leave it. It holds more monsters and grows no ore.
- Adjust: The slingshot floor moves the camera when you step up to the firing line, and moves it back when you step away.
- Fix: The buff bar showed nothing. The mod put the icons off the right-hand edge of the screen. Curses, and all other buffs, were not visible.
- Fix: A boss gave no music, no reward chest and a late ladder. The mod gave an error when the boss died.
- Fix: The forge did not work if you had no cinder shards. A forge floor gives none.
- Fix: The forge menu showed the cinder shard cost.
- Fix: Arc the Skeleton Mage stopped an attack to start another one, then finished the first one.
- Fix: Nadith the Extinct could start two attacks at once after a meteor strike.
- Fix: Telesto the Full Moon delayed a lightning strike after its warning mark.
- Fix: Ozul the Shadow King delayed an arrow volley after its warning sound.
- Fix: The perk list drew its lines in the wrong place and hid most of the list.
- Fix: The ladder sound played when you clicked the ladder, not when you chose a floor.
- Fix: A shrine took its curse when it opened. If you closed it without a choice, you kept the curse and got nothing.
- Fix: The command `rdebug forcechallenge` gave a combat floor of the wrong kind.

## 1.2.2

Released on 8 November 2026 for SMAPI 4.0.0 or later.

This release corrects a fault that stopped a boss fight, and more split-screen
faults.

- Fix: The fight against Telesto the Full Moon failed. The mod gave an error on the first game tick of the fight. Versions 1.2.0 and 1.2.1 have this fault.
- Fix: The mod gave an error when it loaded a save and Mister Qi was not available. The run could not start.
- Fix: In split-screen, two players shared one shop, one seed shop, one hat board, one curse offer, one perk choice and one forge. The second player to arrive replaced the offers of the first player, and purchases of one player could go to the other player.
- Fix: In split-screen, the pick-a-path floor also shared its curse offer, its shop and its wheel.
- Fix: In split-screen, the second screen kept the data of a completed run and used it in a normal save.
- Fix: Monsters on the Journey of the Prairie King floor spawned one tile outside their side. The lava of Nadith the Extinct reached one row and one column outside its area.
- Fix: On the egg hunt floor, the music stopped one second before the end of the floor.

## 1.2.1

Released on 7 November 2026 for SMAPI 4.0.0 or later.

This release corrects faults in multiplayer games and split-screen games.

- Fix: The mod removed the water maps from the map pool after the first dark floor. The maps did not come back until you started the game again.
- Fix: Ozul the Shadow King kept the arena warps and the blocked tiles after his death.
- Fix: Only the host could make Ozul the Shadow King explode. An attack from a farmhand did nothing.
- Fix: Ozul the Shadow King pushed each player in the wrong direction. The push did not move a farmhand.
- Fix: A farmhand did too much damage to Odys the Hivemother. The mod did not decrease the damage during the egg stages.
- Fix: A farmhand did not see the egg animation of Odys the Hivemother.
- Fix: A farmhand read the wrong completion state on some challenge floors.
- Fix: The host did not send the generation seed to a farmhand. Perks, merchant stock and curses used a different seed on each machine.
- Fix: Two split-screen players shared their perks, their spectator inventory and their curse timers.
- Adjust: The seed controls two sequences: one for the floors, one for the offers to each player. A run from an earlier version does not repeat with the same seed.

## 1.2.0

Released on 3 November 2026 for SMAPI 4.0.0 or later.

This release adds support for Stardew Valley 1.6. The mod no longer takes over
the game. You can play a normal save with the mod installed.

- Add: Support for Stardew Valley 1.6 and SMAPI 4.0.
- Add: A mode switch. The mod applies its changes at the start of a run, and removes them when you go back to the title screen.
- Remove: The run upload button and the `rdebug uploadstats` command. The online leaderboard is no longer available.
- Fix: The mod replaced four vanilla tilesheets with art from version 1.5.6. The clock and the money counter showed the wrong image. The mod now overlays only the areas that it uses.
- Fix: Players spawned outside the map in the boss arenas. The mod could not find the ladder.
- Fix: Arc the Skeleton Mage did too much projectile damage. The mod applied the difficulty multiplier two times.
- Fix: Some monster, egg and boulder spawn loops stopped the game.
- Fix: A boss could stop its attacks. One boss attack never occurred.
- Fix: A co-op death could remove your inventory or cause a loop.
- Fix: The mod kept textures and run data in memory after the end of a run.
- Fix: You could not sell ore at two of the three merchants.
- Fix: The lobby ladder, the dialogue of Mr Qi and the dwarf gates did not work correctly.

## 1.1.1

Released on 8 October 2023 for SMAPI 3.15.0 or later.

- Fix: Possible crash when fishing

## 1.1.0

Released on 8 October 2023 for SMAPI 3.15.0 or later.

This release contains the full original game soundtrack created by [Therm](https://www.youtube.com/c/ThermChannel).
Check out the soundtrack on [Spotify](https://open.spotify.com/album/52D2KWYt3HkRcVrWvQQ9Wb?si=XshgYqK2RLGDKGRiILOC-Q) / [YouTube Music](https://music.youtube.com/playlist?list=OLAK5uy_kjGCpe72qCPi_PsKRvihbNU7G2CaAOvY4&si=bJ6E7OqJmnH0JtTP)

- Add: Ability to purchase backpack from merchant floors
- Add: Growable crops on merchant floors
  - Each crop takes a different amount of floors to grow
  - Crops purchasable from Volcano Dwarf
  - Harvest can be sold to Volcano Dwarf
- Add: Fishing capabilities (fishing rod drops from barrels, has limited uses)
- Add: Timed chest that spawns on floor 8 and must be reached before 5 minutes
- Add: Galaxy Soul reward for defeating bosses without being damaged
- Add: Computer in lobby for setting the generation seed
- Add: Save game options in-between game launches
- Add: Hat Board
  - Board spawns on floor 18
  - Choose from 3 random hats, perform quest, receive hat
  - Each hat gives a unique benefit
- Add: Additional cases to the in-game `/stuck` command
  - Remove map obstacles if monsters cannot path to player
- Add: New perks
  - Fisherman: Fishing rod has +2 uses
  - Deconstructor: Deconstructor can spawn on merchant floors and deconstructs items for gold
  - Gamer: Acquire gold from good performances in the arcade machines
  - Tiller: Crops sell for more
- Add: New curses
  - (De)Buff Monsters: Monsters have a 50% chance to apply a random buff/debuff on hit
- Add: New weapon enchantments
  - Star Shooter: Shoot star projectiles on attack
- Add: Sign to inform players of healing spa on merchant floors
- Add: Additional Maps
  - General Gameplay: 2 new custom maps
  - King of the Hill: 1 new custom map, egg hunt map
- Add: Translation capability (please help translate if you can!)
- Add: Visual attack cue indicator in some cases where it was only audio
- Add: Announce music credits on boss floors
- Add: New music track for Goobins ([thanks Therm!](https://www.youtube.com/c/ThermChannel))
- Add: New music track for Telesto ([thanks Therm!](https://www.youtube.com/c/ThermChannel))
- Add: New music track for Odys ([thanks Therm!](https://www.youtube.com/c/ThermChannel))
- Add: New music track for Ozul ([thanks Therm!](https://www.youtube.com/c/ThermChannel))
- Add: New music track for Nadith ([thanks Therm!](https://www.youtube.com/c/ThermChannel))
- Add: New music track for Arc ([thanks Therm!](https://www.youtube.com/c/ThermChannel))
- Add: Configuration option to automatically face the direction your mouse is relative to the player
- Adjust: Limit player to having 12 perks
- Adjust: Floor style will cycle again when looping
- Adjust: Boss HP bar will slowly change color based on their remaining HP
- Adjust: Weapons can be enchanted 999 times (as opposed to 3 times)
- Adjust: The forge can only be used 3 times per appearance
- Adjust: Scale monsters health and damage when looping
- Adjust: Spawn red bats after looping (on hard skull caverns)
- Adjust: Pause time if looting a chest
- Adjust: Increase food prices in shops
- Adjust: Disable chest floor between floors 7 and 11
- Adjust: Nerf Goobins base stats
- Adjust: Load button takes player directly into the game
- Fix: Split screen caused floors to skip, making it unplayable
- Fix: Reset player resilience when resetting
- Fix: Draw location desync for Cavrag on multiplayer
- Fix: Draw location desync for Telesto on multiplayer
- Fix: Stop multiple slingshots from spawning on multiplayer
- Fix: Escape key can now close the gamble wheel menu

## 1.0.17
Released on 11 September 2022 for SMAPI 3.15.0 or later.

- Fix: Possible softlock on speedrun challenge floor in multiplayer
- Fix: Having Indecisive perk would prevent players from opening the shop

## 1.0.16
Released on 10 September 2022 for SMAPI 3.15.0 or later.

- Add: New custom theme for 4th boss
- Add: New speedrun challenge floor
- Add: Whitelist [this zoom mod](https://www.nexusmods.com/stardewvalley/mods/7363)
- Add: Enables the `/stuck` command on boss floors
- Adjust: Item reward chests will attempt to avoid giving items you already have
- Adjust: Pick a Path outcome will change depending on player status
- Adjust: Hot Spring will no longer spawn if player is at or above 90% HP
- Adjust: 4th boss attack pattern
- Adjust: Change chest location of 4th boss
- Adjust: Remove merchant buy backs after menu is closed
- Fix: Possible infinite loop with `/stuck` command on non-normal mines floors
- Fix: Disables hard mode upon save load
- Fix: Bug Killer enchantment would not apply to 5th boss
- Fix: Layering issue with custom floor layout
- Fix: Incorrect rewards on some challenge floors
- Fix: Turtle Shell cooldown would decrease even if paused

## 1.0.15
Released on 7 September 2022 for SMAPI 3.15.0 or later.

- Add: New floor layout
- Adjust: Challenge floors have guaranteed rewards
- Adjust: Stats menu wording
- Adjust: Time will still pass if eating on wave kill challenge floor
- Adjust: Multiplayer clients cannot join mid-run
- Adjust: Multiplayer games cannot continue until all players reach merchant
- Fix: Luck would carry over from previous runs in certain conditions
- Fix: Cobwebs would drop items
- Fix: Various small fixes to custom floor layouts

## 1.0.14
Released on 7 September 2022 for SMAPI 3.15.0 or later.

- Adjust: Lower enemy spawn count on JOTPK floor
- Fix: Player wasn't receiving invincibility on death in singleplayer
- Fix: Heal over Time curse continued indefinitely
- Fix: Typo in Mister Qi dialogue
- Fix: Respawning in multiplayer didn't give items back
- Fix: It was possible to get an extra egg on hard mode
- Fix: Potent buffs curse wasn't applying to existing buffs
- Fix: Possible soft lock on 7th boss in multiplayer
- Fix: Multiplayer clients were able to enter before hosts

## 1.0.13
Released on 6 September 2022 for SMAPI 3.15.0 or later.

- Fix: /stuck command was able to be used in invalid locations

## 1.0.12
Released on 6 September 2022 for SMAPI 3.15.0 or later.

- Fix: Null reference exception with Refreshable Shop

## 1.0.11
Released on 6 September 2022 for SMAPI 3.15.0 or later.

- Add: Two new custom floor layouts
- Add: /stuck in-game command to prevent soft locks
- Add: Display missed items on Dwarf challenge floors
- Adjust: Buff tick rate of Damage over Time curse
- Adjust: Buff tick rate of Healing over Time curse
- Adjust: Enable Wooden Fences to be destroyed by pickaxe
- Adjust: 2nd boss hard mode difficulty adjustments
- Adjust: 5th boss difficulty adjustments
- Adjust: Hidden boss hard mode difficulty adjustments
- Adjust: Remove all crafting recipes except for Wooden Fence
- Fix: 7th boss attack on hard mode had wrong angle
- Fix: Possible exception with Indecisive perk
- Fix: Small visual bug in custom floor layout
- Fix: Dark floor is now chosen with floor generation random
- Fix: Disable monster slayer goals
- Fix: Disable spiders from jumping into water
- Fix: Deallocate MineShaft instances as they become inactive

## 1.0.10
Released on 5 September 2022 for SMAPI 3.15.0 or later.

- Fix: Stop 4th boss from despawning on multiplayer
- Fix: Softlock on new floor layout

## 1.0.9
Released on 5 September 2022 for SMAPI 3.15.0 or later.

- Add: Gamepad support on perk picking menu
- Add: Gamepad support on disclaimer menu
- Add: Gamepad support on stats menu
- Add: Gamepad support on refreshable shop menu
- Add: Gamepad support on wheel menu
- Add: Two new floor layouts
- Adjust: Mention hard mode in Qi's dialogue
- Adjust: Damage over Time curse now scales with total damage to be ticked
- Adjust: Phoenix Ring will now reset Damage over Time ticks (thanks AlexT)
- Fix: Fortune teller wasn't taking max HP (thanks AlexT)
- Fix: Damage over Time curse wasn't activating Phoenix Ring (thanks AlexT)
- Fix: Stop players from buying more than 4 Iridium Milks (thanks AlexT)
- Fix: Remove upload button from stats menu mid-run
- Fix: Stop spiders from jumping out of bounds
- Fix: Remove Refined Quartz from being dropped
- Fix: Various memory leak fixes

## 1.0.8
Released on 4 September 2022 for SMAPI 3.15.0 or later.

- Add: Display current version on game HUD
- Add: Sound effect on Dwarf floor
- Adjust: Rework max health system
- Adjust: Nerf leech to 25% chance (down from 50% chance)
- Fix: Life Elixir ignored Heal Over Time curse
- Fix: Phoenix Ring would not reset each run
- Fix: Fortune teller would still take hp/money even if player has all curses
- Fix: Slingshot challenge wouldn't give reward at 3 waves cleared
- Fix: Slime texture error when spawning slimes off of looped slime

## 1.0.7
Released on 3 September 2022 for SMAPI 3.15.0 or later.

- Add: Display in-game when an update is available
- Adjust: Set maximum cap on knockback
- Adjust: Double aggro range of blue squids
- Adjust: Change food selections with Cheaper Merchant curse
- Adjust: Lower fifth boss movement speed and attack durations
- Adjust: Stop Damage over Time curse from ticking when paused
- Adjust: Lower damage increase of Desperado perk
- Fix: Dwarf floor was taking more health than intended
- Fix: Challenge floors repeating
- Fix: Debug command for adding a specific curse was broken

## 1.0.6
Released on 3 September 2022 for SMAPI 3.15.0 or later.

- Adjust: Skull in top left by floor counter now indicates hard mode
- Adjust: Heal over time curse description
- Adjust: Heal over time will no longer heal while game is paused
- Fix: Cinder shards would be gained on unforging
- Fix: Layers on some trees on egg hunt floor were incorrect

## 1.0.5
Released on 2 September 2022 for SMAPI 3.15.0 or later.

- Add: Gamepad compatibility for the perk viewing menu
- Add: Debug command for adjusting player stamina
- Add: Debug command for forcing forge floors
- Add: Debug command for logging stats payload
- Adjust: Hard mode is now a toggle
- Adjust: Lower first boss max health
- Adjust: Lower projectile count on third boss
- Adjust: Mob cap if player has the more enemies curse
- Adjust: Names and descriptions for various curses and perks
- Adjust: Lower reward tiers on slingshot floors
- Adjust: Gold removal rate on Brittle Crown curse
- Adjust: lower duration on Putrid Ghost debuff
- Remove: Gesture of the Drowned curse
- Fix: Drinking snake milk would heal to max health
- Fix: Challenge floors would ignore history
- Fix: Items would spawn off exploded dirt
- Fix: Exhausting would follow default game behavior

## 1.0.4
Released on 2 September 2022 for SMAPI 3.15.0 or later.

- Fix: Calculation when checking monsters spawned count

## 1.0.3
Released on 2 September 2022 for SMAPI 3.15.0 or later.

- Adjust: Nerf the Arc boss fight
- Remove: Remove Amethyst Ring from the shop
- Fix: Small typos in curse descriptions

## 1.0.2
Released on 2 September 2022 for SMAPI 3.15.0 or later.

- Fix: Bug where floors could be skipped

## 1.0.1
Released on 2 September 2022 for SMAPI 3.15.0 or later.

- Fix: Infinite loop when loading challenge floor

## 1.0.0
Released on 2 September 2022 for SMAPI 3.15.0 or later.

- Initial release