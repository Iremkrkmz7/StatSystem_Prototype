# Wizard Survival (StatSystem_Prototype)

[🇬🇧 **English**](README.md) · [🇹🇷 Türkçe](README.tr.md)

A top-down, wave-based survivor-like game built in Unity. You fight to stay
alive in an endlessly generated world against waves of enemies that keep
getting stronger. Collect XP to level up, pick one of three upgrades each
level, use abilities, and complete quests and achievements. Your progress is
saved permanently, and every new run continues from where you left off.

**Unity 2022.3 · URP · C#**

**▶ Play it in your browser: [Magic Survival on itch.io](https://7iremkrkmz.itch.io/magic-survival)**

---

## What's in this repository?

This repository contains **only my own code and the data I designed**.
The licensed Unity Asset Store packages used in the full game (3D models, VFX,
icon sets, animations, music) and the main scene file are **not included** —
those licenses do not permit redistributing the raw asset files.

So cloning this and opening it in Unity will leave scene and prefab references
missing. **The goal here is to read the code**; to actually play, use the
itch.io link above.

---

## Gameplay

| Input | Action |
|---|---|
| **WASD** | Move |
| **Mouse** | Aim (the character turns toward the cursor) |
| **Left click (hold)** | Shoot |
| **Q / ability keys** | Dash, Shield, AoE, Heal |
| **Ability icons** | Can also be clicked to activate |

**The loop:** a wave starts → enemies spawn → kill them, collect XP and
diamonds → level up and pick one of three upgrades → the wave ends, short
breather → the next wave is bigger and stronger. When you die (or quit to the
menu), your best wave and level are saved, and the next run starts from there.

---

## Systems

### Stat system (the core of the project)

A modifier-based RPG-style stat system — the project's name
(`StatSystem_Prototype`) comes from here.

- **`CharacterStat`** — a base value plus a list of modifiers stacked on top.
  The result is **cached** and only recomputed when the list or the base value
  changes (`_isDirty`).
- **`StatModifier`** — three types: `Flat` (additive), `PercentAdd` (percentages
  are summed), `PercentMult` (percentages are multiplied). Application order is
  guaranteed via `Order`: Flat first, then PercentAdd, then PercentMult.
- **Source tracking (`Source`)** — every modifier remembers the object that
  added it, so `RemoveModifiersFromSource(this)` can cleanly revert everything
  from that source. This is what prevents buffs from stacking up on pooled
  enemies that get reused.
- **`StatType`** — MaxHealth, Speed, Damage, Defense, AttackSpeed, CritChance,
  PickupRadius.
- **`SCharacterStats`** — the MonoBehaviour that carries this system. Used by
  both the player and enemies; handles health, taking and dealing damage,
  healing, death, and level-up reactions.

### XP, levels and upgrade picks

- **`XPSystem`** — XP accumulation, level threshold
  (`baseXPRequired × xpMultiplier^level`), level-up events.
  `SetLevelSilently()` sets a level without firing the UI (used when restoring
  a saved run).
- **`LevelUpUI`** — on level up the game pauses and offers **three random
  upgrade cards** (Vampire Survivors style). There's a **time limit** on the
  choice; the countdown bar is synced exactly to the length of its audio clip
  and shifts from yellow to red as it drains.
- **`UpgradeOption`** — an upgrade definition (which stat, which modifier type,
  how much). The picked upgrade is added as a permanent modifier.
- **On top of that**, each level also applies an automatic health/damage
  increase (`healthIncreasePerLevel`, `damageIncreasePerLevel`).

### Enemies and waves

Each enemy type is defined as data through **`EnemyDataSO`**:

| Field | Purpose |
|---|---|
| `MinWave` | Never spawns before this wave |
| `MinPlayerLevel` | Never spawns before the player reaches this level (independent of MinWave — both must pass) |
| `SpawnWeight` | Weighted random selection — low values for rare types |
| `MaxAlive` | How many of this type may be alive at once |
| `BaseHealth/Speed/Damage/AttackRange/AttackCooldown` | Core combat stats |
| `HealthScalePerWave`, `DamageScalePerWave` | Exponential scaling per wave |
| `BaseXP`, `XPScalePerWave`, `ScoreValue` | Rewards |
| `ExplodesOnDeath` + radius/damage/VFX | Death explosion for bomber-type enemies |

**`EnemySpawner`** drives the wave flow: enemy count per wave
(`baseEnemyCount + countIncrement × wave`, capped by `maxWaveEnemyCount`),
the eligibility filter (MinWave / MinPlayerLevel / MaxAlive), weighted type
selection, and spawn position generation.

**Spawn distribution** — to stop enemies from piling up in one spot: a rotating
angle cursor using the golden angle (137.5°), ±25° jitter, a distance between
85–115% of `spawnRadius`, a minimum-separation check against the last 20 spawn
points, and a NavMesh snap. If no candidate is valid, it falls back to a raw
but angularly spread position.

### Enemy AI

**`EnemyAI`** — NavMesh-based chasing and attacking, with several layers of
resilience on top:

- **Stuck detection** — if the distance to the target hasn't meaningfully
  decreased for `StuckSeconds`, the enemy is considered stuck.
- **Direct-move fallback** — when stuck, it leaves the NavMesh and moves
  straight at the target, sampling `NavMesh.SamplePosition` each step to correct
  its Y against the ground (without this, enemies sank into the terrain and only
  their shadows were visible).
- **Rebinding after pool reuse** — `NavMeshAgent.Warp()` guarantees the agent
  actually lands on the navmesh.
- **Crowd behaviour** — each enemy gets a random `avoidancePriority` (30–70);
  with everyone at the same priority Unity's crowd simulation never yields and
  enemies clipped through each other.
- **Visibility** — `updateWhenOffscreen = true` on every `SkinnedMeshRenderer`;
  stale render bounds after a sudden position change let enemies attack while
  being culled and therefore invisible.

### Procedural world

- **`ChunkManager`** — endless chunk loading/unloading around the player
  (`renderDistance`, `chunkSize`), seed-based deterministic generation, and a
  per-frame work budget (`maxOpsPerFrame`) so streaming never hitches.
- **`WorldChunk`** — each chunk places its own props and ground decorations.
  Hand-placed spawn points scale proportionally against `referenceChunkSize`
  when the chunk size changes; with no spawn points defined it falls back to
  density-based random placement.
- **Runtime NavMesh** — since the world is generated dynamically, the NavMesh is
  baked asynchronously at runtime, with staged re-bakes during the first
  seconds of play.

### Object pooling

**`PoolManager`** — key-based pooling for projectiles, enemies, effects and
chunks. The `IPoolable` interface lets objects reset themselves on the way back
in. The pool survives scene reloads (`DontDestroyOnLoad`), and
`AutoReturnToPool` returns timed effects on its own.

### Abilities

Defined as data via **`AbilitySO`**: type (**Dash / Shield / AoE / Heal**),
activation key, cooldown, effect value and duration, VFX prefab.
**`AbilityController`** manages cooldowns, which shrink as the player levels up
(`Cooldown / (1 + abilityCDLevelFactor × level)`). **`AbilitySlotUI`** renders
each slot with a fill animation that drains over the cooldown.

### Weapons and projectiles

**`WeaponSO`** (damage, fire rate, projectile prefab) + **`WeaponHandler`**
(fire timing — the player's `AttackSpeed` stat divides the interval) +
**`Projectile`** (pooled projectile, hit detection, `HitEffect`).

### Inventory, items and diamonds

**`ItemSO`** types: HealthPack, WeaponPart, ArmorPart, BuffItem, Collectible,
Diamond, AoEItem. **`ItemPickup`** handles world pickups and
**`InventorySystem`** the collection/use logic. Diamonds live in two separate
counters: a per-run count (for the end screen and the record) and a
**persistent total** that is saved the moment one is picked up, so it survives
even if the game is closed mid-run.

### Quests and achievements

- **`QuestSO` / `QuestManager`** — five quest types: `KillEnemies`,
  `CollectDiamonds`, `SurviveSeconds`, `KillWithoutDamage`, `MultiKill`.
  Progress is tracked through events; completion grants XP and diamonds.
- **`AchievementSO` / `AchievementManager`** — achievements persist; on a new
  run previously unlocked ones are restored silently (no repeated popup), while
  newly unlocked ones announce themselves through a notification panel.

### Save system and "continue where you left off"

**`SaveSystem`** (PlayerPrefs-based) persists: best wave, best player level,
best kill/diamond records, accumulated total diamonds, unlocked achievements,
and whether the how-to-play screen has been shown.

**A key design decision:** best wave and best player level are stored
**independently**. Waves advance by surviving, levels by earning XP, and the two
do not move at the same rate — deriving the level from the wave produced
inconsistencies like *"I died at level 11 and restarted at 14."*

When a new run begins, the saved wave and level are restored, and the
health/damage growth those missing levels would have granted is applied
**silently, in one step** (`ApplyCatchUpLevels`) — no cascade of level-up
screens.

Progress is saved both on death **and when quitting to the menu while still
alive**: this is a record system rather than a session save (values only ever go
up), so not recording a wave you genuinely reached felt like a bug.

### Intro cinematic

**`PlayerIntroDrop`** — the character falls from the sky, the camera opens on a
bird's-eye framing, orbits, then eases into the in-game camera. Landing dust,
sound and camera shake accompany it. On restart the cinematic is skipped, but
the character settling onto the ground is still masked. The moment control
actually returns to the player, the how-to-play panel appears (once per
lifetime), and the enemy spawn timer only starts after that panel closes.

### User interface

`HUDController` (health, XP, wave, kill counter) · `HealthBar` (world-space
enemy health bars) · `DamageVignette` (red flash on hit plus a persistent edge
effect at low health) · `MultiKillUI` (Double/Triple/Quadra/Penta Kill banner
plus bonus XP) · `FloatingText` (damage, XP, heal numbers) · `QuestUI` ·
`InventoryUI` · `AchievementNotificationUI` · `LevelUpUI` · `PauseUI` ·
`GameOverUI` · `SettingsUI` (audio settings, persisted via PlayerPrefs) ·
`LoadingUI` / `StartMenuUI` · `HowToPlayOverlay` · `GameTimer` ·
`GlobalButtonSfx` (automatic click sound on every button).

### Game feel

`CameraShake` (damped shake) · `HitStop` (brief time slowdown on impact) ·
`EnemyFlash` (white flash on damage) · `EnemyShake` · `DeathExplosion` (area
damage plus VFX when a bomber dies) · `GroundFireDamageZone` (lingering damage
area after an explosion) · `PlayerLevelUpEffect` · `TwinkleEffect`.

### Event system

`GameEvent` / `TypedGameEvent` / `GameEventListener` — ScriptableObject-based,
loosely coupled event channels. Systems communicate without referencing each
other directly (an enemy death, for instance, is heard by the quest, achievement
and multi-kill systems alike).

---

## Data-driven balance pipeline

Balance values aren't hardcoded; they flow from a spreadsheet into the game:

```
GameBalance.xlsx   →   balance_global.csv    →   BalanceConfig.asset
   (edit by hand)       balance_enemies.csv       EnemyDataSO assets
                         (save as CSV)          (import from Unity menu)
```

- **`GameBalance.xlsx`** — an 8-sheet workbook: Config, XP & Levels, Player
  Progression, Enemies, Waves, Abilities, Economy & Meta, Balance Issues.
  Formulas let you see the level and wave curves before touching the game.
- **`BalanceConfig`** — a single ScriptableObject under `Resources`; the
  **single source of truth** for the XP curve, per-level health/damage growth,
  wave sizes, spawn radius and interval, wave cooldown, ability cooldown factor
  and more. `EnemySpawner`, `XPSystem`, `SCharacterStats` and
  `AbilityController` all read from it.
- **`BalanceImporter`** (Editor) — a `Tools ▸ Balance` menu:
  `Import ALL from CSV`, `Import Global only`, `Import Enemies only`,
  `Export CURRENT values to CSV`. It matches fields by name via reflection and
  silently skips anything it doesn't recognise.

The result: changing balance requires no code edits and no hunting through the
Inspector — edit the sheet, save as CSV, import from the menu.

---

## Repository layout

```
Assets/
  Scripts/
    Core/
      Ability/       Ability definitions and controller
      Achievement/   Achievement system
      Events/        ScriptableObject-based event channels
      Inventory/     Items, pickups, inventory
      Pool/          Object pooling
      Quest/         Quest system
      Stats/         Stat/modifier core (the heart of the project)
      BalanceConfig.cs, SaveSystem.cs, XPSystem.cs, GameFlow.cs, Sfx.cs
    Enemy/           Enemy AI, spawner, data, death effects
    Player/          Control, combat, camera, intro cinematic
    Weapon/          Weapons, projectiles, hit effects
    World/           Chunk-based procedural world
    UI/              All interface and game-feel layers
  Editor/
    BalanceImporter.cs   Excel/CSV → game data import tool
  ScriptableObjects/     Enemy, ability, quest, item, achievement definitions
  Resources/
    BalanceConfig.asset  Central balance settings
GameBalance.xlsx
balance_global.csv, balance_enemies.csv
```

---

## Notable problems solved along the way

This section documents the bugs that were genuinely hard to diagnose:

- **The NavMesh silently did nothing at runtime.**
  `NavMeshSurface.UpdateNavMesh()` bakes geometry into a `NavMeshData` object
  but does **not** register it with the runtime navigation system. The automatic
  `AddData()` call inside `OnEnable()` registered nothing, because `navMeshData`
  was still `null` at that point. The result was a navmesh that threw no errors
  and was completely inert — enemies simply never moved. Fix: explicitly call
  `RemoveData()` + `AddData()` after every bake.

- **The pool broke on scene reload.**
  Returned objects weren't reparented back under the persistent pool object, so
  they were destroyed along with the scene while the pool kept dead references.
  The next `Get()` threw a `MissingReferenceException` and aborted the chunk
  spawn loop midway, leaving holes in the world.

- **Invisible enemies that still attacked.**
  After a sudden position change (Warp/teleport), the `SkinnedMeshRenderer`'s
  cached render bounds went stale and Unity's frustum culling skipped drawing
  the mesh — while physics and AI kept running, so an unseen enemy could still
  land hits.

- **Infinite camera shake.** The shake loop never advanced its counter
  (`elapsed` stayed constant inside `while (elapsed < duration)`). The bug stayed
  invisible until enemies could actually land a hit.

- **Variable collisions in coroutines.** Methods containing `yield return`
  compile into a state-machine class, so identically named locals in completely
  separate blocks still raise `CS0136`.

- **The "white screen" mystery.** Unity draws a `RawImage` with no texture as
  **opaque white**, not as transparent. That empty full-screen layer was
  covering the menu background underneath it.

- **Video can't play in WebGL.** Unity's `VideoPlayer` component isn't supported
  in WebGL builds; it worked in the Editor and went black in the build. The menu
  background was switched to a static image so it behaves identically everywhere.

- **Resolution mismatch.** Because the UI is laid out in absolute pixels
  ("Constant Pixel Size"), elements spilled off-screen whenever the build
  resolution differed from the resolution the layout was designed against.

- **VFX Graph doesn't run in WebGL — and took the whole game down with it.**
  The player's projectile was a VFX Graph effect. VFX Graph needs compute
  shaders, which WebGL 2.0 does not have, so the browser build logged
  `Invalid VFX Particle System` once per pooled projectile and then died at
  startup with `RuntimeError: memory access out of bounds`. Telling detail: on
  one machine the same build merely skipped the effect and ran fine, on another
  it crashed — GPU-dependent behaviour, which made it look unreproducible at
  first. Replaced with an additive unlit mesh that renders on every platform.

- **Sounds arrived late in the browser.** Every audio clip had *Preload Audio
  Data* disabled, so a clip was decoded the first time it played. On a local
  disk that is invisible; in a browser the decode is noticeable, and doing it
  mid-combat also caused hitches. Preloading moves the work to the loading
  screen.

- **Enemy hits landed a second after the swing.** The attack animation started
  the instant an enemy entered range, but damage and the hit sound waited for a
  full `AttackCooldown` countdown — so the first swing was silent and harmless,
  and later hits fired at the *start* of a swing rather than on contact. Damage
  and sound are now scheduled at the animation's impact moment, and the hit is
  skipped entirely if the player escaped the range during the wind-up.

- **Rebaking the NavMesh froze the browser.** Every chunk load or unload marked
  the NavMesh dirty, triggering a full rebake of a 300×300 m volume at 0.17 m
  resolution — roughly 3.2 million voxels. WebGL builds have no worker threads,
  so that ran on the main thread and stalled the frame every time the player
  crossed into new terrain. Shrinking the volume to 160×160 m (enemies spawn at
  most ~46 m away) and coarsening the voxel size to 0.3 m cut the work ~11×.

- **The build shipped 60 MB uncompressed.** Compression was set to Disabled;
  switching to Gzip — with Decompression Fallback, which itch.io needs since it
  does not send the matching `Content-Encoding` header — brought the download to
  ~33 MB. WebGL initial memory was also raised from its 32 MB default.

---

## Building (WebGL)

- Target resolution **1920×1080**
- **Decompression Fallback** enabled (so the browser can decompress when the
  server doesn't send the right headers)
- WebGL content cannot run over `file://`; it needs a local HTTP server or real
  hosting (itch.io)

---

## License

My own code and data are **MIT** licensed — see [LICENSE](LICENSE).
Third-party Unity Asset Store content is not included in this repository and
remains subject to its own licenses.
