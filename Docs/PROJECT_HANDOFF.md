# Project Handoff

Last updated: 2026-08-20

## Current prototype state

The project is a Unity `6000.0.32f1` URP prototype using the Input System. The working scene is `Assets/Scenes/SampleScene.unity`.

Current deliberate scope:

- Stationary first-person camera with mouse look.
- No player translation or free movement.
- No rail system yet.
- Zombies use baked NavMesh paths toward the main camera and can reroute around carved dynamic obstacles.
- Simple hitscan combat, feedback, modular zombie prefabs, and editor painting tools.
- A first modular environment interaction is implemented: shootable explosive barrels that affect zombies and break marked dynamic obstacles.
- A designer-facing environmental opportunity tool supports exactly DROP, PUSH, SHOCK, and EXPLODE; its physical triggers now connect to the existing weapon damage contract and run small one-use prototype effects.
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

The Tank's death event damages and knocks back nearby `IDamageable` targets except `ExplosiveBarrel`. It spawns particles, a point-light flash, and a translucent sphere that expands to the exact damage radius before fading. Damage can synchronously trigger another Tank death, so Tank-to-Tank chain explosions remain possible, but Tank deaths never damage or detonate barrels.

## Environment interactions

The first reusable interaction is `ExplosiveBarrel`:

- Implements the existing `IDamageable` contract, so pistol, shotgun, and AK-47 hits work without weapon-specific barrel logic.
- Starts at 20 health; current starting balance is a 5 m radius, 125 damage, and 8 knockback force.
- Deduplicates overlapping colliders so each target is damaged and pushed once per blast.
- Damages and knocks back zombies, chain-reacts with other barrels, and displays particles, a light flash, and an expanding sphere matched to the damage radius.
- Barrel chain reactions can still start from weapons, other barrels, and environmental explosion damage; Tank death explosions explicitly ignore barrels.
- Calls `IExplosionBreakable` on marked objects in range.
- Blood particles are now limited to zombie hits; environment hits still use normal hitmarker and floating-damage feedback.

The blue wall's saved `DynamicNavMeshTestBlock` implements `IExplosionBreakable` and attaches/configures `BreakableNavMeshObstacle` when hit by a blast. The focused breakable component immediately disables carving and the movement controller, turns the Rigidbody into a physical body, throws it away, and removes it after 3 seconds. This opens the navigation route without rebaking the whole surface.

`SampleScene` includes one test barrel at `(0, 0, -3.6)`, between the camera and the dynamic wall.

## Environmental opportunity authoring

Open **Tools → Level Design → Environmental Interaction Tool**.

Open **Tools → Level Design → Environmental Interaction Tuning** (or click **Open Scene-Wide Tuning** in the creation tool) to edit the shared project-wide profile. Every change is immediately copied to all matching opportunities in every open scene, and newly created opportunities inherit the same values. The button at the top can explicitly reapply the entire profile to all current objects; selected-object Inspectors remain available for intentional local overrides.

The tuning window exposes:

- **DROP:** trigger delay, forward lead distance, fall time, smash area, damage, knockback, and impact-particle feedback.
- **PUSH:** sustained water/push duration, reach, affected width/height, pushback power, and water feedback.
- **SHOCK:** activation delay, total duration, pulse interval, affected area, damage per pulse, zombie speed multiplier down to a complete `0%` stop, active-zone electricity, enemy taze particles, and shake strength/speed.
- **EXPLODE:** delay, blast radius, full-damage radius, damage, knockback power, enemy ragdoll launch/tumble, and corpse visibility time.

This independent authoring workflow exposes exactly four placement choices: **DROP**, **PUSH**, **SHOCK**, and **EXPLODE**. Each generated root is placed at the current Scene View pivot, receives a unique `ENV_<Type>_###` name, and keeps the physical shootable trigger separate from the effect object/origin and affected area.

Generated interactions include:

- Shared serialized ID, display name, enabled state, one-use flag, trigger reference, visualization preferences, and designer notes.
- Focused type components (`DropInteraction`, `PushInteraction`, `ShockInteraction`, and `ExplodeInteraction`) rather than one component containing every type's fields.
- A physical `EnvironmentalTrigger` with collider and visual references. It implements `IDamageable`, so any positive pistol, shotgun, or AK-47 hit asks its owning interaction to activate.
- A reusable `EnvironmentalInteractableHighlight` driving a subtle transparent overlay with the lightweight `Zombie Prototype/Environmental Interactable Pulse` URP shader. Only the assigned shootable overlay pulses from a cool cyan base toward warm amber; both colors, opacity, flash amount/speed, emission, material, and editor/game visibility remain designer-tunable.
- A shared **Appearance — Color Coding** Inspector section exposes the colored renderers, object color, and shootable-part color for every opportunity. Type-specific effect colors and particle amounts remain alongside their gameplay tuning.
- Clear selected-object Scene visualization and labels for trigger/effect relationships, trajectories, directions, volumes, and radii.
- Direct Scene handles for DROP smash-area size, PUSH direction/range/width, SHOCK area size, and EXPLODE inner/outer radii.
- Collider-free translucent edit-mode geometry previews for all four interaction types. Their box/sphere shape and world dimensions synchronize with the gameplay area after global-profile changes, selected-object Inspector edits, and direct Scene-handle edits; they are hidden during Play Mode and remain separate from runtime activation.
- SHOCK's visible blue conductive-surface footprint also follows its authored box size or sphere diameter, while the translucent volume shows the full vertical extent used by overlap checks.
- Placeholder blockout geometry that can be replaced or reassigned to existing scene objects. The custom Inspector can convert any assigned scene object into a collider-backed highlighted trigger.
- The generated DROP blockout uses a large hanging container with its thin shootable string directly above the container. For box-shaped DROP opportunities, the global/Inspector/Scene-handle smash size also resizes the generated container to the exact same world dimensions and repositions the string above it. DROP leads toward its authored forward direction, then searches downward without a distance limit for the first valid solid floor and offsets the resized container so its bottom lands on that surface.

Generated trigger and color language:

- **DROP:** shoot the flashing red string; the large container is safety yellow/orange.
- **PUSH:** shoot any flashing part of the red fire-hydrant blockout. One compound collider encloses all eight visible pieces, the entire assembly pulses as interactable, and its push direction still starts at the front water nozzle with blue water particles.
- **SHOCK:** shoot the safety-yellow power box. The separate red wire is visual guidance, while the conductive water surface is blue.
- **EXPLODE:** shoot the red/orange explosive body.

Runtime activation is deliberately small and deterministic:

- Shared activation is one-use by default. On activation the trigger stops flashing, its shootable collider is disabled, `On Activated` is invoked, and the focused effect runs. `On Effect Completed` is available for later sequencing without coupling the tool to an encounter manager.
- **DROP** waits for its delay, leads 1.5 m along its authored forward direction, resolves the floor there with an unlimited downward query, and moves the object diagonally until its bottom reaches that floor before applying smash damage and force. This small lead helps compensate for horde movement between the player's shot and impact. The hanging container has a disabled box `NavMeshObstacle`; after landing it enables stationary carving at the container's exact collider size, so zombies dynamically route around the new blockage without a NavMesh rebake. Generated DROP interactions use a container-sized box area and 500 starting damage, enough to crush every current zombie archetype caught underneath. Their string trigger collider is three times the visible string's original collider size for easier shooting, while the visible string remains thin. An optional self-cleaning impact effect emits broad dust puffs and faster debris chips around the authored smash footprint.
- **PUSH** repeatedly applies ordinary directional knockback for the full sustained duration. Zombies remain upright, alive, and under `ZombieMovement`/NavMesh control while sliding in the authored hydrant direction; they never enter the environmental ragdoll component. Non-kinematic props still receive physical force, and the blue water jet uses the same active duration. Global starting balance is 1.25 seconds, 8 m reach, a 4 × 2.5 m cross-section, and 18 push power.
- **SHOCK** targets `ZombieHealth`/`ZombieMovement` only, so its electrical zone cannot damage or chain-trigger explosive barrels or other generic `IDamageable` props. As soon as the activation delay ends, blue electrical particles cover the authored sphere/box for the entire active duration even when no enemy is inside. Zombies inside receive repeated damage, a globally defaulted `0` movement multiplier that immediately clears navigation velocity and pending knockback for a complete stop, attached taze particles, and a small visual-body shake. The shake deliberately leaves the NavMesh root and headshot collider untouched.
- **EXPLODE** still applies inner-to-outer damage falloff to non-zombie damageables and blows physical props outward. Zombies use the enabled-by-default environmental ragdoll response and are killed visibly, launched radially with upward force and tumble, then removed after the corpse delay. Global ragdoll starting balance is 6 upward force, 12 tumble torque, and 3 seconds visible.

These are prototype gameplay effects, not final skeletal ragdolls, electricity propagation, VFX, sound, encounter choreography, or puzzle logic. The earlier `ExplosiveBarrel` remains a separate live prefab interaction.

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
| Four-type opportunity authoring runtime | `Assets/EnvironmentInteraction/Authoring/Runtime` |
| Runtime enemy taze feedback | `Assets/EnvironmentInteraction/Authoring/Runtime/EnvironmentalTazeFeedback.cs` |
| Runtime environmental zombie ragdoll | `Assets/EnvironmentInteraction/Authoring/Runtime/EnvironmentalZombieRagdoll.cs` |
| Environmental Interaction Tool and handles | `Assets/EnvironmentInteraction/Authoring/Editor` |
| Scene-wide environment tuning window | `Assets/EnvironmentInteraction/Authoring/Editor/EnvironmentalInteractionTuningWindow.cs` |
| Global environment tuning profile | `Assets/EnvironmentInteraction/Authoring/Settings/EnvironmentalInteractionGlobalTuning.asset` |
| Shared interactable highlight material | `Assets/EnvironmentInteraction/Authoring/Materials/EnvironmentalInteractableHighlight.mat` |
| Interactable flashing shader | `Assets/EnvironmentInteraction/Authoring/Shaders/EnvironmentalInteractablePulse.shader` |
| URP environment particle shader | `Assets/EnvironmentInteraction/Authoring/Shaders/EnvironmentalParticleUnlit.shader` |

## Architectural rules for continuation

- Prefer data changes in `ZombieArchetype` and `WeaponProfile` over duplicated prefab values.
- Derive zombie types from the base zombie prefab so fixes propagate to every variant.
- Keep damage targets behind `IDamageable`.
- Keep hit feedback separate from health and movement logic.
- Maintain prefab linkage when placing or adding content.
- Add focused components instead of a single large zombie manager.
- Keep the prototype easy to reset and inspect.
- Do not add rail movement or free movement until requested.
- Keep the opportunity authoring tool limited to DROP/PUSH/SHOCK/EXPLODE unless the user explicitly changes that set.
- Keep the focused prototype effects small and profile-like; do not infer encounter scripting or puzzle solutions from authored opportunities. The earlier MOVE/SLOW/BLOCK model remains a higher-level design lens documented in `DESIGN_DIRECTION.md`.

## Known caveats

- `HitscanPistol` is a legacy class/file name; it now controls the entire weapon loadout. Renaming it requires carefully preserving Unity script GUID references.
- The player currently has no movement and no implemented player-health loop.
- Zombie navigation now uses `NavMeshAgent` with low-quality local avoidance and destinations refreshed every 0.25 seconds. Rigidbody components are kinematic and retained for mass/collider data.
- Knockback is applied through temporary `NavMeshAgent.Move` displacement, so it stays constrained to navigable space rather than behaving as unconstrained Rigidbody physics.
- Standard health death still deactivates the zombie GameObject. Environmental EXPLODE death deliberately keeps it active as a physics corpse, then destroys it after the global corpse delay. PUSH is nonlethal normal NavMesh knockback and never creates an environmental ragdoll.
- The Tank radius sphere is a runtime effect, not a persistent scene gizmo.
- Tank death explosions skip `ExplosiveBarrel` before applying either damage or Rigidbody force, preventing Tank-to-barrel chain reactions without changing normal barrel chaining.
- The barrel is intentionally simple: its balance lives on the prefab component rather than a separate tuning profile until more interaction types justify shared data tooling.
- The breakable wall response is a prototype whole-object throw/remove effect, not a fragment or destruction system.
- DROP/PUSH/SHOCK/EXPLODE now perform simple prototype runtime effects, but their values and feel have not been user-validated and are not final gameplay implementations.
- PUSH duration now controls both the blue water effect and sustained repeated pushing. Pushback power is still divided by zombie Rigidbody mass, so Tanks deliberately move less than Normal and Runner zombies.
- SHOCK damage is intentionally zombie-only; explosive barrels still chain-react to weapons and explosion damage, but never to the electrical zone.
- Generated hydrant, power-box, wire, water, container, and explosive visuals are readable color-coded primitive blockouts rather than final environment art.
- Generated PUSH hydrants use one non-trigger compound `BoxCollider` on the `FireHydrant` assembly and eight synchronized highlight overlays. The individual body, rings, dome, caps, and valve have no separate colliders, so every visible part resolves to one activation and the complete collider disables after one-use consumption.
- Runtime DROP, PUSH-water, and explosive-barrel particle renderers explicitly use the supported `Zombie Prototype/Environmental Particle Unlit` URP shader instead of Unity's pink incompatible fallback material.
- Newly created weapon-blood and Tank-death particle systems are stopped before their duration modules are configured, avoiding Unity's runtime duration assertion during hits and environmental kills.
- The active SHOCK field keeps all Velocity over Lifetime axes in Constant curve mode; its Noise module provides movement variation without Unity's mixed-curve-mode particle error.
- The flashing weak-point treatment uses a second, slightly enlarged mesh renderer. Existing objects without a `MeshFilter` or `SkinnedMeshRenderer` need a manually assigned overlay renderer.
- Duplicating a generated hierarchy correctly remaps its local trigger/effect references, but the duplicated root retains the copied interaction ID until the designer uses **Regenerate** in its Inspector.
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
