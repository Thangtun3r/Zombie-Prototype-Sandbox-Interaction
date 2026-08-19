# Project Handoff

Last updated: 2026-08-19

## Current prototype state

The project is a Unity `6000.0.32f1` URP prototype using the Input System. The working scene is `Assets/Scenes/SampleScene.unity`.

Current deliberate scope:

- Stationary first-person camera with mouse look.
- No player translation or free movement.
- No rail system yet.
- Zombies use baked NavMesh paths toward the main camera and can reroute around carved dynamic obstacles.
- Simple hitscan combat, feedback, modular zombie prefabs, and editor painting tools.
- A first modular environment interaction is implemented: shootable explosive barrels that affect zombies and break marked dynamic obstacles.
- The user performs all gameplay playtesting and provides balance/feel feedback.

Do not enter Play Mode unless the user explicitly asks. Compiler checks, asset inspection, and serialized-reference checks are appropriate.

## Player controls

| Input | Action |
|---|---|
| Mouse | Look |
| Left mouse | Fire |
| `R` | Reload |
| `1` | Equip pistol |
| `2` | Equip shotgun |
| `3` | Equip AK-47 |
| Mouse wheel | Switch weapon |
| `B` | Toggle the dynamic NavMesh test wall between blocked and parked positions |
| Escape | Unlock cursor, as handled by the camera-look script |

## Implemented player combat

`Assets/PlayerPrototype/Scripts/HitscanPistol.cs` retains its original class name for scene-reference compatibility, but it is now the general multi-weapon hitscan controller.

Implemented behavior:

- Profile-driven damage, shots per second, range, knockback, pellet count, spread, headshot multiplier, magazine size, reload duration, recoil, and automatic/semi-automatic input.
- Separate ammunition state for every equipped weapon.
- Pistol, shotgun, and AK-47 selection.
- Hold-to-fire automatic input for the AK-47.
- Pellet spread for the shotgun.
- Shotgun damage is accumulated per target and knockback is applied once per shot, preventing eight independent push impulses.
- Every visible zombie whose collider intersects the shotgun cone is guaranteed at least one pellet of damage and one knockback impulse. Random pellets still add damage; zombies do not shield other zombies in the cone, while solid environment does.
- Tracers, muzzle flashes, blood particles, recoil, reload UI, red crosshair, and hitmarker feedback.
- The shotgun replaces the standard line crosshair with a circular ring; the ring also turns red on confirmed hits. Its radius is projected from the active profile's spread angle using the camera FOV, viewport height, and Canvas scale, so it represents the maximum pellet boundary.
- Floating world-space damage numbers: white for body damage and larger yellow/orange `CRIT!` numbers for headshots.
- Multi-pellet damage is aggregated into one floating number per damaged zombie per shot.
- HUD shows the active weapon and magazine ammunition.

Headshots use an inherited `SphereCollider` and `ZombieHitbox` on each zombie's `Head` child. Hitscan evaluation uses `Physics.RaycastAll`: the nearest hit determines the target or obstruction, then colliders belonging to that same target are checked for the head zone.

### Starting weapon balance

| Weapon | Damage | Fire rate | Pellets | Spread | Knockback | Magazine | Reload | Range | Headshot | Recoil |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Pistol | 34 per pellet | 5.0/s | 1 | 0.15° | 1.5 | 12 | 1.25 s | 100 m | 2.0× | 0.05 |
| Shotgun | 13 per pellet | 1.15/s | 8 | 6° | 8.0 | 6 | 1.8 s | 35 m | 1.5× | 0.12 |
| AK-47 | 18 per pellet | 10.0/s | 1 | 1.1° | 2.2 | 30 | 1.9 s | 80 m | 1.8× | 0.035 |

Tune these shared assets through **Tools → Zombie Prototype → Weapon Tuning** or directly in `Assets/PlayerPrototype/Weapons`.

## Implemented zombies

Every zombie prefab shares the same small runtime components:

- `ZombieTarget` — resolves the main camera/player target.
- `ZombieMovement` — NavMeshAgent destination updates, low-quality avoidance, direct fallback, and temporary agent-based knockback interruption.
- `ZombieHealth` — health, damage event, death event, and deactivation on death.
- `ZombieAttack` — basic range/cooldown attack against `IDamageable` targets.
- `ZombieDamageFeedback` — brief material flash when damaged.
- `ZombieArchetypeBinding` — applies shared archetype balance on validation and startup.
- `ZombieDeathExplosion` — optional death explosion configured by the archetype.
- `ZombieHitbox` — marks the inherited head collider for headshot detection.

### Starting zombie balance

| Type | Health | Speed | Damage | Cooldown | Mass | Knockback interruption | Death explosion |
|---|---:|---:|---:|---:|---:|---:|---|
| Normal | 100 | 2.0 | 10 | 1.0 s | 1.0 | 0.16 s | None |
| Runner | 45 | 4.5 | 8 | 0.65 s | 0.75 | 0.20 s | None |
| Tank | 300 | 0.9 | 25 | 1.4 s | 3.5 | 0.08 s | 4 m radius, 45 damage, 4.5 force |

The Tank's death event damages and knocks back nearby `IDamageable` targets. It spawns particles, a point-light flash, and a translucent sphere that expands to the exact damage radius before fading. Damage can synchronously trigger another Tank death, so chain explosions are possible.

## Environment interactions

The first reusable interaction is `ExplosiveBarrel`:

- Implements the existing `IDamageable` contract, so pistol, shotgun, and AK-47 hits work without weapon-specific barrel logic.
- Starts at 20 health; current starting balance is a 5 m radius, 125 damage, and 8 knockback force.
- Deduplicates overlapping colliders so each target is damaged and pushed once per blast.
- Damages and knocks back zombies, chain-reacts with other barrels, and displays particles, a light flash, and an expanding sphere matched to the damage radius.
- Calls `IExplosionBreakable` on marked objects in range.
- Blood particles are now limited to zombie hits; environment hits still use normal hitmarker and floating-damage feedback.

The blue wall's saved `DynamicNavMeshTestBlock` implements `IExplosionBreakable` and attaches/configures `BreakableNavMeshObstacle` when hit by a blast. The focused breakable component immediately disables carving and the movement controller, turns the Rigidbody into a physical body, throws it away, and removes it after 3 seconds. This opens the navigation route without rebaking the whole surface.

`SampleScene` includes one test barrel at `(0, 0, -3.6)`, between the camera and the dynamic wall.

## Editor tools

### Zombie Painter

Open **Tools → Zombie Prototype → Zombie Painter**.

Features:

- Select Normal, Runner, or Tank from a three-column type grid.
- Edit shared zombie balance in a table.
- Edit Tank explosion radius, damage, and force.
- Paint prefab-linked zombies by clicking or dragging in the Scene view.
- Configure brush radius, stroke spacing, density, minimum separation, layer mask, height offset, and yaw.
- Erase with Erase mode or temporarily erase by holding Ctrl/Cmd.
- A whole paint or erase stroke is grouped into one Undo operation.

### Weapon Tuning

Open **Tools → Zombie Prototype → Weapon Tuning**.

The table exposes damage per pellet, fire rate, pellets, spread, knockback, magazine size, reload duration, range, headshot multiplier, recoil, and automatic fire. Values are stored in shared `WeaponProfile` assets.

### Environment Interaction Painter

Open **Tools → Zombie Prototype → Environment Interaction Painter** or **Tools → Environment Interaction → Painter**.

The catalog currently contains an **Explosive Barrel** tile and discovers `EnvironmentInteractionCatalogItem` assets automatically. It supports prefab-linked Scene-view click/drag placement, paint/erase modes, Ctrl/Cmd temporary erase, brush radius, stroke spacing, minimum separation, layer mask, height offset, yaw, automatic parenting under `Painted Environment Interactions`, and grouped Undo. Erase only removes objects carrying `EnvironmentInteractionMarker`.

### Dynamic NavMesh test

`SampleScene` contains a baked `NavMeshSurface` and a blue object named `[B] Dynamic NavMesh Block`.

Test flow:

1. Enter Play Mode manually.
2. Observe zombies route around the centered blue wall.
3. Press `B` to move the wall outside the route.
4. Press `B` again to return it to the center.
5. After the wall stops for 0.2 seconds, stationary carving updates and agents calculate around the new blockage.
6. Alternatively, shoot the red barrel in front of the wall; its blast breaks the wall and immediately removes its carving obstacle.

The wall is excluded from the baked surface and contributes only through `NavMeshObstacle`. It uses local avoidance while moving and carving while stationary. The surface does not need a full runtime rebuild for this test.

## Important assets

| Purpose | Path |
|---|---|
| Main scene | `Assets/Scenes/SampleScene.unity` |
| Base/Normal zombie | `Assets/ZombiePrototype/Prefabs/Zombie.prefab` |
| Runner variant | `Assets/ZombiePrototype/Prefabs/Zombie_Runner.prefab` |
| Tank variant | `Assets/ZombiePrototype/Prefabs/Zombie_Tank.prefab` |
| Zombie profiles | `Assets/ZombiePrototype/Archetypes` |
| Zombie painter | `Assets/ZombiePrototype/Editor/ZombiePlacementWindow.cs` |
| Weapon profiles | `Assets/PlayerPrototype/Weapons` |
| Shotgun viewmodel prefab | `Assets/PlayerPrototype/Prefabs/Shotgun_Viewmodel.prefab` |
| AK-47 viewmodel prefab | `Assets/PlayerPrototype/Prefabs/AK47_Viewmodel.prefab` |
| Weapon tuning window | `Assets/PlayerPrototype/Editor/WeaponTuningWindow.cs` |
| Player scripts | `Assets/PlayerPrototype/Scripts` |
| Floating damage feedback | `Assets/PlayerPrototype/Scripts/FloatingDamageText.cs` |
| Zombie scripts | `Assets/ZombiePrototype/Scripts` |
| Dynamic obstacle test controller | `Assets/ZombiePrototype/Scripts/DynamicNavMeshTestBlock.cs` |
| Explosive barrel prefab | `Assets/EnvironmentInteraction/Prefabs/ExplosiveBarrel.prefab` |
| Environment interaction catalog | `Assets/EnvironmentInteraction/Catalog` |
| Environment interaction runtime scripts | `Assets/EnvironmentInteraction/Scripts` |
| Environment Interaction Painter | `Assets/EnvironmentInteraction/Editor/EnvironmentInteractionPainterWindow.cs` |

## Architectural rules for continuation

- Prefer data changes in `ZombieArchetype` and `WeaponProfile` over duplicated prefab values.
- Derive zombie types from the base zombie prefab so fixes propagate to every variant.
- Keep damage targets behind `IDamageable`.
- Keep hit feedback separate from health and movement logic.
- Maintain prefab linkage when placing or adding content.
- Add focused components instead of a single large zombie manager.
- Keep the prototype easy to reset and inspect.
- Do not add rail movement or free movement until requested.
- Preserve the MOVE/SLOW/BLOCK design hypothesis and add one testable environment interaction at a time.

## Known caveats

- `HitscanPistol` is a legacy class/file name; it now controls the entire weapon loadout. Renaming it requires carefully preserving Unity script GUID references.
- The player currently has no movement and no implemented player-health loop.
- Zombie navigation now uses `NavMeshAgent` with low-quality local avoidance and destinations refreshed every 0.25 seconds. Rigidbody components are kinematic and retained for mass/collider data.
- Knockback is applied through temporary `NavMeshAgent.Move` displacement, so it stays constrained to navigable space rather than behaving as unconstrained Rigidbody physics.
- Death currently deactivates the zombie GameObject; pooling/reset behavior is not yet generalized.
- The Tank radius sphere is a runtime effect, not a persistent scene gizmo.
- The barrel is intentionally simple: its balance lives on the prefab component rather than a separate tuning profile until more interaction types justify shared data tooling.
- The breakable wall response is a prototype whole-object throw/remove effect, not a fragment or destruction system.
- The Unity MCP package reports the project-path space as an error-level warning. This is unrelated to C# compilation.
- No gameplay feel or balance values have been independently validated; all listed numbers are starting points awaiting user feedback.

## Suggested continuation workflow

1. Read this file and `DESIGN_DIRECTION.md`.
2. Inspect the current scene and compiler state before editing.
3. Implement the smallest requested feature using existing profiles and prefabs.
4. Refresh/compile and verify serialized references without entering Play Mode.
5. Let the user playtest and report behavior.
6. Apply their feedback without silently expanding scope.
7. Append the result to `DEVELOPMENT_LOG.md` and update this handoff if architecture, controls, balance, or known caveats changed.
