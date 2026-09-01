# CLAUDE.md

## 1. Project Overview

This project is a 2D side-scrolling action roguelike game.

The player character is a hamburger.

The hamburger initially consists of only a bun. During a run, the player discovers and collects different ingredients. Ingredients function as magical tools that provide different attacks or effects.

The core gameplay loop is:

Explore → Discover ingredients and buns → Build an ingredient loadout → Fight enemies → Clear rooms → Gain temporary upgrades → Reach the final area → Defeat the boss → Victory

Each run is self-contained. There is no mid-run save system and all run-specific progress is lost when the player dies or completes the run.

---

## 2. Core Design Philosophy

Prioritize the following:

1. Build variety through ingredient selection and ordering.
2. Make the order of ingredients meaningful.
3. Make resource management meaningful.
4. Make exploration and procedural generation create different situations every run.
5. Keep controls simple.
6. Make the game understandable without requiring complicated tutorials.
7. Create interesting interactions through individual ingredient effects, not through elemental chemistry systems.
8. Prefer emergent gameplay caused by combinations of simple systems.
9. Keep systems modular so additional ingredients, enemies, buns, rooms, and upgrades can be added later.
10. Prioritize a playable and fun core loop over excessive content.

---

## 3. Controls

### Movement

* A / D or Left / Right Arrow: Horizontal movement
* Space: Jump

### Combat

* Left Mouse Button: Activate the next equipped ingredient

### Menu

* Tab: Open ingredient setup / inventory menu

The control scheme should remain simple unless explicitly requested otherwise.

---

## 4. Ingredient System

Ingredients are magical tools.

Each ingredient has its own:

* Attack or effect
* Mana cost
* Activation delay
* Optional additional effect
* Optional projectile or attack behavior

Examples:

* Tomato → Fireball
* Lettuce → Ice attack
* Cheese → Increase the power of the next attack

The examples above are conceptual examples and should be implemented as data-driven systems rather than hard-coded special cases.

---

## 5. Ingredient Slot System

The player's current bun determines how many ingredients can be equipped.

Example:

* Small bun → 3 ingredient slots
* Normal bun → 5 ingredient slots
* Large bun → 7 ingredient slots

Exact values may be adjusted during balancing.

Ingredients are equipped in an ordered vertical list.

Example:

```text
[ Tomato ]
[ Cheese ]
[ Lettuce ]
```

When the player presses Left Mouse Button:

```text
First click
→ Tomato activates

Second click
→ Cheese activates

Third click
→ Lettuce activates

Fourth click
→ Return to Tomato
```

The system should clearly communicate which ingredient will activate next.

---

## 6. Ingredient Ordering

Ingredient order is a fundamental gameplay mechanic.

The same ingredients can produce different combat behavior depending on their order.

For example:

```text
Tomato
Cheese
Lettuce
```

is different from:

```text
Cheese
Tomato
Lettuce
```

because effects are resolved sequentially.

Ingredients may modify subsequent attacks.

Examples:

* Increase the damage of the next attack
* Cause the next attack to fire two projectiles
* Increase projectile size for the next attack
* Cause the next attack to pierce enemies
* Increase the range of the next attack
* Modify the next attack's attack speed

These effects should operate on the next attack or a clearly defined number of subsequent attacks.

---

## 7. No Elemental Chemistry System

Do NOT implement systems where ingredients combine into new elements or reactions.

Do not implement mechanics such as:

* Fire + Water = Steam
* Fire + Ice = Explosion
* Elemental fusion
* Chemical reactions between ingredients

Instead, interactions should come from each ingredient's individual effect and its position in the sequence.

For example:

```text
Power-up ingredient
→ Next attack becomes stronger

Double-shot ingredient
→ Next attack fires twice

Piercing ingredient
→ Next attack pierces enemies
```

This keeps the system predictable while still allowing interesting builds.

---

## 8. Bun System

Buns are equipment that determine the player's available ingredient capacity and may also provide other characteristics.

Buns can be discovered and collected during a run.

The player can replace the current bun during a run.

Different buns may have:

* Different ingredient slot counts
* Different maximum mana
* Different mana regeneration speed
* Different movement characteristics
* Different defensive characteristics
* Other clearly communicated passive properties

The player must be able to switch to a newly acquired bun during exploration.

Changing buns should be a meaningful strategic decision.

---

## 9. Mana System

The player has mana.

Each ingredient has a mana cost.

Using an ingredient consumes mana.

The current bun determines important mana characteristics such as:

* Maximum mana
* Mana regeneration rate

The player should be able to understand:

* Current mana
* Maximum mana
* Mana cost of the next ingredient

Mana management should prevent unlimited use of the strongest attacks.

---

## 10. Attack Delay

Ingredients may have an activation delay.

After an ingredient activates, the player may need to wait before activating the next ingredient.

This creates a rhythm to combat.

The delay should be visible or otherwise understandable to the player.

Do not make the system unnecessarily complicated.

---

## 11. Inventory and Setup Menu

Pressing Tab opens the inventory / ingredient setup screen.

The menu should allow the player to:

* View collected ingredients
* View the current bun
* Equip ingredients
* Unequip ingredients
* Change ingredient order
* Change the equipped bun
* View relevant ingredient information

The player should be able to understand the current attack sequence from this screen.

Example:

```text
Current Bun
Large Bun

Ingredient Sequence

1. Cheese
   Next attack damage +50%

2. Tomato
   Fireball

3. Lettuce
   Ice projectile

4. Double Shot
   Next attack fires twice
```

---

## 12. Procedural Generation

The game must generate each run procedurally.

Randomize as appropriate:

* Room layout
* Terrain
* Room connections
* Enemy placement
* Ingredient placement
* Bun placement
* Upgrade placement
* Enemy composition
* Rewards
* Optional rooms
* Environmental hazards

The generated map must remain playable.

Procedural generation should create meaningful variation rather than simply randomizing object positions.

Prefer a room-based procedural structure combined with procedurally generated terrain.

---

## 13. Environmental Design

The game should have a physically readable 2D environment.

Where practical, implement:

* Platforms
* Gaps
* Walls
* Elevation differences
* Narrow passages
* Open combat spaces
* Environmental hazards
* Destructible or breakable environmental elements where technically appropriate

The purpose is to make movement and positioning relevant to combat.

Do not add complicated environmental systems unless they improve the core gameplay.

---

## 14. Enemy System

Implement multiple enemy types with clearly different behaviors.

At minimum, provide several archetypes such as:

* Basic melee enemy
* Fast enemy
* Ranged enemy
* Heavy enemy
* Flying or mobile enemy

Each enemy should have:

* Health
* Damage
* Movement behavior
* Attack behavior where appropriate
* Death behavior
* Appropriate difficulty scaling

Enemies should be modular so new enemy types can be added without rewriting the entire combat system.

---

## 15. Run Progression

The run is divided into multiple areas / stages.

A typical structure:

```text
Starting Area
↓
Early Areas
↓
Mid Areas
↓
Late Areas
↓
Final Area
↓
Boss
↓
Victory
```

The exact number of areas can be adjusted during implementation.

Difficulty should gradually increase.

---

## 16. Room Clear Rewards

Clearing rooms provides temporary, run-specific rewards.

Possible rewards include:

* Increased maximum HP
* Increased movement speed
* Increased attack damage
* Increased attack speed
* Increased mana
* Increased mana regeneration
* Reduced activation delay
* Increased projectile speed
* Increased projectile size
* Additional defensive effects
* Other temporary modifiers

These upgrades last only for the current run.

They disappear after Game Over or Victory.

---

## 17. Roguelike Structure

The game has no permanent character progression in the initial version.

When the player dies:

```text
Game Over
↓
Run ends
↓
Start a new run
```

When the player defeats the final boss:

```text
Boss defeated
↓
Victory
↓
Run ends
↓
Start a new run
```

No mid-run save system is required.

---

## 18. Boss

The final area contains a boss.

The boss must be substantially stronger and more complex than normal enemies.

The player wins by defeating the boss.

After victory, clearly display a Victory state.

A restart / new run option should then be available.

---

## 19. Architecture

Prefer a modular, data-driven architecture.

Recommended structure:

```text
Assets/
├── Scenes/
├── Scripts/
│   ├── Player/
│   ├── Combat/
│   ├── Ingredients/
│   ├── Buns/
│   ├── Enemies/
│   ├── ProceduralGeneration/
│   ├── Rooms/
│   ├── Upgrades/
│   ├── UI/
│   └── Game/
├── Prefabs/
├── ScriptableObjects/
│   ├── Ingredients/
│   ├── Buns/
│   ├── Enemies/
│   └── Upgrades/
├── Art/
├── Audio/
└── Materials/
```

This is a recommended organization. Adapt it if the existing project structure makes another organization more appropriate.

Use ScriptableObjects for data that should be easily configurable, such as:

* Ingredient definitions
* Bun definitions
* Enemy definitions
* Upgrade definitions

Avoid hard-coding individual ingredient behavior into a single giant script.

---

## 20. Code Quality

Code must:

* Be understandable to a beginner/intermediate Unity developer.
* Use clear class and variable names.
* Follow single-responsibility principles where practical.
* Avoid unnecessary abstraction.
* Avoid giant manager classes.
* Avoid duplicated logic.
* Use `[SerializeField]` for Inspector-configurable private fields.
* Handle missing references safely.
* Avoid unnecessary external dependencies.
* Prefer Unity's built-in systems where appropriate.

Do not over-engineer the project.

---

## 21. Git Rules

Never directly modify the `main` branch unless explicitly instructed.

Work on the currently assigned feature/development branch.

Before starting significant work:

```bash
git status
```

Check the current branch and working tree.

Commit changes in logical units.

Examples:

```text
Add player movement
Add ingredient data system
Add ingredient activation system
Add bun system
Add enemy AI
Add procedural room generation
Add room reward system
Add boss
Fix ingredient ordering
Fix procedural generation
```

Do not create meaningless commits.

Do not commit generated Unity directories excluded by `.gitignore`, especially:

* Library/
* Logs/
* UserSettings/

Do not rewrite Git history or force-push unless explicitly instructed.

Do not merge into `main`.

---

## 22. Development Process

Follow this general order:

### Phase 1 — Project Analysis

Inspect the existing project before modifying it.

Understand:

* Unity version
* Existing scenes
* Existing scripts
* Existing assets
* Current Git branch
* Existing project architecture

Do not unnecessarily replace existing work.

### Phase 2 — Core Player

Implement:

* Movement
* Jumping
* Basic physics
* Player health
* Death

### Phase 3 — Ingredient Framework

Implement:

* Ingredient data
* Inventory
* Ingredient slots
* Ingredient ordering
* Mana
* Activation delay
* Sequential activation
* Temporary attack modifiers

### Phase 4 — Combat

Implement:

* Projectiles
* Damage
* Enemy health
* Enemy death
* Multiple enemy types

### Phase 5 — Bun System

Implement:

* Bun data
* Ingredient capacity
* Mana characteristics
* Bun swapping

### Phase 6 — Procedural Generation

Implement:

* Room generation
* Terrain generation
* Room connections
* Enemy spawning
* Item spawning
* Reward placement

### Phase 7 — Run Progression

Implement:

* Room clearing
* Temporary upgrades
* Increasing difficulty
* Area progression

### Phase 8 — Boss

Implement:

* Final area
* Boss
* Boss behavior
* Victory condition

### Phase 9 — UI

Implement:

* HP
* Mana
* Current ingredient
* Ingredient sequence
* Inventory/setup menu
* Game Over
* Victory
* Basic run information

### Phase 10 — Testing and Polish

Test the complete gameplay loop.

Fix:

* Compile errors
* NullReferenceExceptions
* Broken references
* Procedural generation failures
* Soft locks
* Unreachable rooms
* Incorrect ingredient ordering
* Mana bugs
* Death/restart bugs
* UI problems

Prioritize functionality and stability over visual polish.

---

## 23. Autonomous Development Rules

When working autonomously:

1. Inspect before modifying.
2. Prefer small, testable changes.
3. Test each major system before building on it.
4. If an implementation fails, investigate the cause before adding workarounds.
5. Do not hide errors.
6. Do not remove functionality simply to make an error disappear.
7. Keep the game playable throughout development whenever practical.
8. If a feature becomes too complex, implement a simpler functional version first.
9. Do not spend excessive time on art polish before the gameplay loop works.
10. Prioritize the core gameplay loop.

The final result must be a playable Unity game, not merely a collection of scripts.

---

## 24. Definition of Done

The project is considered a successful prototype when:

* The game launches without compile errors.
* The player can move and jump.
* The player can enter rooms.
* Enemies spawn.
* Enemies can damage the player.
* The player can damage and defeat enemies.
* Ingredients can be collected.
* Ingredients can be equipped.
* Ingredient order can be changed.
* Left click activates ingredients sequentially.
* Ingredient effects work.
* Temporary next-attack modifiers work.
* Mana is consumed and regenerated.
* Activation delays work.
* Buns can be collected and changed.
* Bun capacity affects ingredient slots.
* The map changes between runs.
* Rooms can be cleared.
* Temporary upgrades can be obtained.
* Difficulty increases.
* A final boss exists.
* Defeating the boss produces Victory.
* Dying produces Game Over.
* A new run can be started.
* No mid-run save is required.
* The game can be played from beginning to end without manual scene setup.

---

## 25. Important Constraint

Do not expand the scope indefinitely.

The first objective is a complete, playable vertical slice.

It is better to have:

* 5–10 well-designed ingredients
* 3–5 enemy types
* Several bun types
* A small number of upgrade types
* A functional procedural system
* One functional boss

than to have dozens of incomplete systems.

After the core loop is stable, content can be expanded.
