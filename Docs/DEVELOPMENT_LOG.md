# Development Log

All entries below occurred on 2026-08-19. This log records implementation and scope decisions; it is not a gameplay validation report.

## Prototype foundation

- Established a stationary first-person camera with mouse look only.
- Deliberately postponed both free movement and the rail system.
- Added simple modular zombies that acquire the main camera and move toward it.
- Split zombie behavior into targeting, Rigidbody movement, health, attack, and damage-feedback components.
- Created the reusable base zombie prefab.

## Initial firearm and feedback

- Added a hitscan pistol to the camera.
- Added ammunition, reload behavior, muzzle flash, tracers, blood particles, viewmodel recoil, and shot knockback.
- Added HUD ammunition/reload information, a crosshair, hitmarker, and red crosshair hit confirmation.
- Raised the camera sightline to approximately zombie-head height.
- Added a damage flash and brief movement interruption when zombies are shot.

## Zombie placement workflow

- Added the **Zombie Painter** editor window.
- Added click/drag painting, prefab-linked placement, Scene-view brush preview, brush radius, spacing, density, minimum separation, surface projection, random yaw, and grouped Undo.
- Added fast erase mode and Ctrl/Cmd temporary erase.

## Zombie archetypes

- Added shared `ZombieArchetype` ScriptableObject data and `ZombieArchetypeBinding`.
- Added selectable Normal, Runner, and Tank tiles to the painter.
- Added a shared zombie balance table to the painter.
- Created Runner and Tank prefab variants with distinct scale and materials.
- Runner starts fast and fragile; Tank starts slow and durable.
- Added Tank death damage, knockback, particles, and light flash.

## Expanded combat

- Added an expanding translucent Tank blast-radius sphere matched to the actual 4 m damage radius.
- Added inherited zombie head colliders and `ZombieHitbox` markers.
- Refactored the original pistol controller into a profile-driven multi-weapon hitscan controller while retaining the `HitscanPistol` class name for Unity reference compatibility.
- Added shared `WeaponProfile` assets.
- Added per-weapon damage, fire rate, range, knockback, pellet count, spread, headshot multiplier, magazine, reload, recoil, and firing-mode settings.
- Added a shotgun with eight pellets, wider spread, shorter range, and substantially stronger aggregate knockback.
- Added a prefab-backed shotgun viewmodel and switching via `1`, `2`, and mouse wheel.
- Updated the HUD to show the active weapon.
- Added the **Weapon Tuning** editor table.

## Handoff documentation

- Added `README.md`, `AGENTS.md`, `Docs/DESIGN_DIRECTION.md`, and `Docs/PROJECT_HANDOFF.md`.
- Recorded controls, architecture, starting balance, tools, asset paths, known caveats, continuation rules, and the user-owned playtesting constraint.

## AK-47 loadout expansion

- Added an AK-47 `WeaponProfile` with automatic fire, a 30-round magazine, moderate spread, and intermediate knockback.
- Added a prefab-backed AK-47 viewmodel with metal and wood materials plus muzzle-flash feedback.
- Expanded the camera loadout from two weapons to three while retaining separate magazine state for every profile.
- Added selection through the `3` key and preserved mouse-wheel cycling.
- Added the AK-47 to the existing Weapon Tuning table automatically through profile discovery.

## Floating damage feedback

- Added lightweight world-space damage numbers that face the main camera, rise, scale, and fade automatically.
- Body hits display a white aggregated damage value.
- Headshots display a larger yellow/orange `CRIT!` value while retaining each weapon profile's headshot multiplier.
- Shotgun pellet damage is combined into one number per target instead of spawning one label per pellet.

## Weapon-specific crosshair

- Added a procedural circular UI crosshair for the shotgun.
- Pistol and AK-47 retain the standard line crosshair.
- Crosshair shape changes automatically when the equipped weapon changes, including mouse-wheel switching.
- The circular shotgun ring uses the existing red hit-confirmation color feedback.
- Replaced the ring's fixed radius with a projection of the configured weapon spread, camera FOV, viewport height, and Canvas scale so it matches the maximum pellet cone at different resolutions.

## Shotgun cone coverage

- Added a coverage pass after random shotgun pellet traces.
- Every visible zombie collider intersecting the circular spread cone is guaranteed at least one pellet of damage and one aggregate knockback impulse.
- Random pellet hits remain responsible for additional damage and possible headshot multipliers.
- Zombies inside the cone do not shield zombies behind them; solid environment continues to block the shot.

## Dynamic NavMesh test

- Replaced direct zombie pursuit with `NavMeshAgent` pathfinding while preserving archetype speed, stopping distance, low-quality avoidance, and temporary knockback interruption.
- Retained a simple direct-movement fallback for zombies placed outside the baked surface.
- Added a baked `NavMeshSurface` to `SampleScene`; structural inspection returned 369 triangulation vertices.
- Added a blue carving obstacle named `[B] Dynamic NavMesh Block` across the central approach.
- Added `B` toggling so the wall moves between blocked and parked positions for user testing.
- Configured stationary carving with a 0.2-second settling delay; other zombies do not require a full surface rebake.
- Confirmed the Normal, Runner, and Tank prefab hierarchy inherits `NavMeshAgent`.

## Explosive barrel and environment catalog

- Added a reusable shootable explosive-barrel prefab using the existing `IDamageable` contract.
- Added deduplicated 5 m AoE damage, zombie knockback, barrel chain reactions, fiery particles, a point-light flash, and an expanding radius sphere.
- Limited weapon blood effects to zombie hits so shooting an environment object does not emit blood.
- Added `IExplosionBreakable` and a focused breakable NavMesh-obstacle component.
- Updated the blue dynamic wall's saved controller to implement the explosion-break contract; a nearby barrel blast attaches the focused breakable behavior, immediately disables carving and movement, throws the wall away, and removes it after a short delay.
- Placed one test barrel between the stationary camera and the dynamic wall in `SampleScene`.
- Added the **Environment Interaction Painter** with a catalog tile grid, prefab-linked paint/erase strokes, minimum separation, surface projection, random yaw, automatic parenting, and grouped Undo.
- Created an extensible `EnvironmentInteractionCatalogItem` asset and registered the Explosive Barrel as the first catalog entry.

## Current verification status

- Scripts compiled without C# errors after the latest implementation pass.
- Zombie archetype references, the three-weapon loadout, prefab variants, inherited head hitboxes, weapon viewmodel prefabs, explosive-barrel prefab, environment catalog, and breakable wall components were structurally inspected.
- The scene was saved after environment interaction configuration.
- Play Mode was intentionally not entered. Gameplay feel and balance await user testing.
