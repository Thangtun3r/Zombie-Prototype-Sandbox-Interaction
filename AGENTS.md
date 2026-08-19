# Instructions for AI collaborators

Read these files before changing the prototype:

1. `Docs/PROJECT_HANDOFF.md`
2. `Docs/DESIGN_DIRECTION.md`
3. `Docs/DEVELOPMENT_LOG.md`

Project rules:

- Keep systems small, modular, and prefab/profile driven.
- The player is currently a stationary first-person camera. A rail system is intentionally not implemented yet.
- Do not add free player movement.
- Do not introduce additional core environmental verbs without explicit user direction. The current design hypothesis is MOVE, SLOW, and BLOCK.
- The user performs gameplay playtesting and supplies feel/balance feedback. Do not enter Play Mode unless explicitly asked.
- Structural inspection and compiler-error checks are allowed and expected after implementation.
- Preserve prefab links and tune shared values through `ZombieArchetype` and `WeaponProfile` assets.
- Avoid expanding the prototype into a complicated framework before a concrete test requires it.
- After meaningful changes, update both `Docs/PROJECT_HANDOFF.md` and `Docs/DEVELOPMENT_LOG.md`.

Known tooling note: the project path contains a space, so the Unity MCP package logs a path warning even when compilation is clean.
