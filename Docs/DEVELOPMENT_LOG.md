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

## Four-type environmental opportunity authoring tool

- Added a designer-facing window at **Tools → Level Design → Environmental Interaction Tool** with exactly DROP, PUSH, SHOCK, and EXPLODE creation choices.
- Added independent serialized runtime authoring data with a shared base, physical trigger metadata, and focused type-specific components.
- Kept trigger objects separate from effect objects/origins and affected areas so the Scene View clearly answers what is shot and what is affected.
- Added readable generated `ENV_<Type>_###` hierarchies at the Scene View pivot with blockout geometry, colliders, local references, one-use metadata, and designer notes.
- Added a reusable transparent-overlay `EnvironmentalInteractableHighlight` with tunable opacity, pulse, emission, material, and editor/game visibility.
- Added grouped custom Inspectors and Scene visualization for trigger links, DROP trajectories/impact areas, PUSH direction/volume, SHOCK source/conductive areas, and EXPLODE inner/outer radii.
- Added Scene handles for the primary spatial parameters and lightweight edit-mode previews; no Play Mode or gameplay activation pipeline is required.
- Added support for converting an existing scene object into a collider-backed highlighted trigger.
- Kept all `UnityEditor` dependencies under the authoring `Editor` folder and left the runtime data independent from player, weapon, zombie, damage, rail, ragdoll, and encounter systems.
- Refreshed and compiled the project without C# errors or exceptions.
- Created all four types through the actual authoring backend in a temporary additive scene and verified trigger/highlight/effect references plus duplicated-hierarchy reference remapping. The temporary scene was closed without saving or changing `SampleScene`.

Entries below occurred on 2026-08-20.

## Weapon-triggered environmental opportunities

- Connected `EnvironmentalTrigger` to the existing `IDamageable` contract, allowing the pistol, shotgun, and AK-47 to activate authored opportunities without adding a weapon-specific branch.
- Added owner binding, a shared `TryActivate` lifecycle, one-use consumption, runtime reset support, and `On Activated` / `On Effect Completed` events.
- One-use activation now disables the shootable collider and hides the interactable treatment so later shots pass through instead of repeatedly confirming the same opportunity.
- Added a deterministic DROP coroutine with delay, authored fall duration, impact damage, radial force, and sphere/box overlap support.
- Added PUSH box-volume targeting with directional impulses for `ZombieMovement` and non-kinematic rigidbodies.
- Added SHOCK pulse damage plus a focused temporary slow component that restores each zombie's original movement speed on completion or disable.
- Added EXPLODE inner/outer damage falloff, zombie and Rigidbody force, a radius pulse, and optional explosive-object hiding.
- Added configurable affected-layer masks and runtime tuning fields to each focused Inspector.
- Replaced the per-frame C# highlight pulse with the lightweight transparent `Environmental Interactable Pulse` URP shader. Generated and converted weak points use a slightly enlarged overlay mesh, and the effect turns off when consumed.
- Ensured existing scene objects converted into weapon targets use non-trigger colliders because the weapon raycast intentionally ignores trigger colliders.
- Hardened `ZombieDamageFeedback` initialization after runtime effect damage exposed a null `MaterialPropertyBlock` path during script/domain reload conditions.
- Structurally verified the exact `IDamageable.TakeDamage → TryActivate → consume` call chain in an isolated edit-mode component and confirmed all four generated types bind their owner, effect tuning, collider, overlay, and supported pulse shader.
- Refreshed Unity after the final changes with no compiler errors, exceptions, or warnings. Play Mode gameplay feel remains for user testing.
- Updated the interactable shader to pulse the exact shootable overlay between designer-tunable cool and warm colors, making the weak point clearer without tinting the effect object or affected zone.
- Corrected the generated DROP silhouette: enlarged the hanging container, moved it up so its authored fall ends on the ground plane, and placed the thin flashing string trigger directly above and touching the container.
- Replaced DROP's authored direction/distance endpoint with unlimited downward floor detection. The container now computes its landing center from its collider height, ignores zombies and interaction colliders while finding the floor, and defaults to a container-sized box smash with 500 damage so every current zombie archetype underneath is crushed.
- Added optional DROP impact particles: a footprint-scaled, self-cleaning burst combines dusty puffs with faster debris chips exactly when smash damage is applied.

## Environmental visual language and tuning

- Added the supported transparent `Zombie Prototype/Environmental Particle Unlit` URP shader and assigned runtime environment particles an explicit self-cleaning material, removing the pink fallback renderer.
- Added a shared Appearance section for every DROP/PUSH/SHOCK/EXPLODE root with tunable colored renderers, object color, and shootable-part color.
- Added tunable DROP dust/debris color, particle amount, and pulse color; PUSH water color and particle amount; SHOCK water, wire, and effect colors; and EXPLODE effect color.
- Rebuilt the generated PUSH blockout as a seven-part red fire hydrant with a separate flashing front valve/nozzle trigger and a blue duration-matched water jet emitted in the authored push direction.
- Changed generated SHOCK so the power box is the physical weapon trigger; the separate wire is red visual guidance and the conductive water surface is blue.
- Color-coded the generated DROP container safety yellow/orange with a red string trigger and kept EXPLODE red/orange.
- Migrated the saved generated `ENV_Shock_001` in `SampleScene` to the new power-box trigger, red wire, blue water, and tunable appearance references.
- Created all four types through the authoring backend in a temporary additive scene and verified trigger mappings, visual references, colors, hydrant hierarchy, particle defaults, and shader support; the temporary scene was closed without saving.

## Scene-wide environmental tuning

- Added **Tools → Level Design → Environmental Interaction Tuning** and a shortcut button in the creation tool.
- The first version discovered every DROP/PUSH/SHOCK/EXPLODE in open scenes and exposed per-object cards.
- Added clear controls for SHOCK duration/pulse timing/slow strength, hydrant water duration/reach/pushback power, DROP timing/smash values, and EXPLODE blast/full-damage radii/damage/knockback.
- Renamed the matching custom-Inspector labels so the selected-object and scene-wide workflows use the same gameplay language.

## Global environmental tuning and stronger hydrant push

- Replaced the per-object tuning-window workflow with the shared `EnvironmentalInteractionGlobalTuning` project asset. Editing one DROP, PUSH, SHOCK, or EXPLODE group now immediately applies that group to all matching objects in every open scene.
- Added explicit per-type and all-types apply buttons, while keeping selected-object Inspectors available for intentional local overrides.
- Made every newly generated interaction inherit the global profile automatically.
- Applied the initial global profile to the four current SampleScene opportunities (`ENV_Drop_001`, `ENV_Drop_002`, `ENV_Push_001`, and `ENV_Shock_001`) and saved the scene.
- Changed the hydrant from one short impulse to a sustained directional push for the full water duration. Raised its global starting values to 18 power, 1.25 seconds, 8 m reach, 4 m width, and 2.5 m height; zombie Rigidbody mass still provides archetype resistance.

## Zombie-only SHOCK targeting and taze feedback

- Replaced SHOCK's generic `IDamageable` collection with explicit `ZombieHealth` and `ZombieMovement` targeting. Electrical pulses can no longer damage or detonate `ExplosiveBarrel` objects.
- Added `EnvironmentalTazeFeedback`, which emits supported-URP blue electrical particles for the active taze duration and jitters collider-free zombie body visuals without moving the NavMesh root or headshot collider.
- Added global and per-object controls for enabling taze particles, particle color/amount, enemy shake strength, and enemy shake speed.
- Applied the updated global SHOCK profile to the current `ENV_Shock_001` and saved `SampleScene`.
- Unity compilation and serialized-field checks passed. A later isolated Edit Mode overlap test confirmed SHOCK reports zero barrel targets and leaves barrel health unchanged at 20.

## Active SHOCK-zone electrical field

- Added a self-cleaning `SHOCK Active Electrical Field` particle system that begins when the activation delay ends and remains over the complete authored box/sphere for the full SHOCK duration, including when no zombie is present.
- Added global and per-object controls for enabling the field, its electric color, and particle amount. Applied the updated global profile to `ENV_Shock_001` and saved `SampleScene`.
- The field uses the supported `Zombie Prototype/Environmental Particle Unlit` shader, a noisy stretched-spark treatment, and `ParticleSystemStopAction.Destroy` cleanup.
- Verified in an isolated temporary scene: 2-second duration, 5 × 0.35 × 4 box coverage, active playback, supported shader, and automatic destruction configuration.

## Full-stop taze and environmental ragdoll deaths

- Removed SHOCK's previous 5% minimum speed floor. The global multiplier is now `0`; it also clears current NavMesh velocity and pending knockback, producing a true immediate stop for the remaining active SHOCK duration before restoring the original archetype speed.
- Added `ZombieHealth.Kill(bool deactivateGameObject)` so environmental physics deaths can invoke normal damage/death events without immediately hiding the zombie.
- Added `EnvironmentalZombieRagdoll`, a whole-body physics response suited to the current primitive zombie rig. It disables NavMesh, movement, attacks, taze/slow feedback, enables gravity and free Rigidbody rotation, applies directional/upward impulse plus tumble torque, and destroys the visible corpse after a tunable delay.
- Enabled the response globally for PUSH (`4` upward force, `8` torque, `3s` corpse delay) and EXPLODE (`6` upward force, `12` torque, `3s` corpse delay). Added matching global and per-object controls.
- Applied the profile to current `ENV_Push_001`, `ENV_Shock_001`, and `ENV_Explode_001` and saved `SampleScene`.
- Isolated integration verification confirmed exact speed `0`; PUSH and EXPLODE each marked their zombie dead while visible, disabled movement/attack/NavMesh, switched the root Rigidbody from kinematic to gravity physics with unconstrained rotation, and attached the active ragdoll component.

## Affected-area visual synchronization

- Added collider-free translucent box/sphere meshes for DROP, PUSH, SHOCK, and EXPLODE affected areas using a dedicated URP preview shader and shared material.
- Bound preview shape and world dimensions to the same gameplay values used by overlap checks. Synchronization now runs after global tuning changes, selected-object Inspector validation, and direct Scene-handle edits.
- Kept the meshes visible only for edit-mode authoring so they do not block weapon traces, enter runtime physics, or clutter the played game.
- Made SHOCK's blue conductive surface resize with the authored box footprint or sphere diameter in addition to the separate full-volume preview.
- Migrated and saved all five current SampleScene opportunities. Verified saved dimensions of `4 × 2 × 2.5` for both DROP boxes, `4 × 2.5 × 8` for PUSH, `5 × 0.5 × 4` for SHOCK, and a `10`-meter-diameter EXPLODE sphere.
- In an unsaved additive test scene, verified arbitrary resize propagation, SHOCK box-to-sphere switching, conductive-surface footprint changes, zero preview colliders, and the supported `Zombie Prototype/Environmental Area Preview` shader. Play Mode was not entered for the test.

## SHOCK particle velocity-mode correction

- Corrected the active electrical field's Velocity over Lifetime setup: X, Y, and Z now all use Constant curves, satisfying Unity's requirement that every axis share one curve mode.
- Retained the field's irregular electrical movement through its existing Noise module while removing the `Particle Velocity curves must all be in the same mode` error.

## Recoverable hydrant knockdown

- Changed PUSH ragdoll handling from lethal corpse cleanup to a nonlethal temporary knockdown. Hydrant-hit zombies preserve their current health, receive the existing launch/tumble impulse, and cannot navigate or attack while down.
- Repurposed PUSH's existing 3-second corpse-delay tuning as a global **Get-Up Delay**. After it expires, the zombie is made upright, placed on nearby NavMesh when possible, returned to its original kinematic movement setup, and resumes navigation and attacks.
- Kept EXPLODE lethal and disappearing. An explosion can also upgrade a currently knocked-down PUSH zombie into the lethal ragdoll path instead of allowing it to recover.

## DROP container and smash-volume matching

- Bound generated box-shaped DROP containers to their authored smash-area size. Global tuning, selected-object Inspector edits, and Scene handles now resize both the translucent affected-area preview and the visible hanging block to identical world dimensions.
- Recalculate floor landing height from the resized container and automatically keep the generated shootable string centered directly above its top surface.

## Landed DROP navigation obstacle

- Added a disabled box `NavMeshObstacle` to generated DROP containers. It does not affect navigation while the container is hanging or falling.
- At impact, the obstacle is configured from the resized container collider and enables stationary carving with a short settling threshold, making the landed block dynamically redirect zombies on the existing baked NavMesh.
- Added a global/per-object **Blocks Navigation After Landing** toggle and migrated the current SampleScene DROP container to the new obstacle setup.
- Runtime verification also exposed and corrected recovery ordering: a knocked-down zombie now samples and moves to nearby NavMesh before its disabled agent is re-enabled, preventing off-mesh agent warnings.
- Stopped newly created blood and Tank-death particle systems before configuring their duration, removing Unity's `Setting the duration while system is still playing is not supported` assertion during weapon hits and DROP crush chain effects.

## Deterministic hydrant flow knockdown

- Removed PUSH's upward launch from both the global tuning profile and the current SampleScene hydrant. EXPLODE retains its independent upward-force and random lethal-ragdoll behavior.
- Replaced the nonlethal PUSH tumble with one deterministic forward-topple axis derived from the authored hydrant direction. While down, upward and sideways velocity are rejected so the zombie follows the water flow instead of taking an unpredictable physics path.
- Stopped the zombie's rotation once it reaches a horizontal pose, leaving it lying down until the global Get-Up Delay completes. Renamed the remaining PUSH rotation control to **Forward Topple Force** and kept it globally applied.

## Hydrant returned to normal push

- Removed the PUSH ragdoll/knockdown path after playtest feedback. Hydrant water now uses the existing `ZombieMovement.ApplyKnockback` behavior exclusively, keeping zombies upright, alive, and NavMesh-controlled.
- Removed PUSH knockdown, upward-force, topple-force, and get-up-delay fields from the runtime component, shared global profile, selected-object Inspector, and global tuning window. EXPLODE keeps its independent lethal ragdoll controls and behavior.
