# Critical gameplay fixes

- NPC NavMesh agents now plan movement while CharacterControllers execute collision-aware movement. Player/NPC controller step offsets are 0.05 m, with overlap recovery enabled and 0.015 m skin widths.
- Toys retain enabled, solid colliders and remain stationary until collected. Successful pickup disables collisions during the retrieval animation. Incorrect targets shake a temporary visual copy for 0.4 seconds while their physical collider stays stationary; progress does not change.
- Missing environmental mesh colliders were saved into the gameplay scene. Generated objects use the same collider repair helper. `CollisionAudit.md` lists the 6,526 inspected scene meshes. Water, sky, effects and UI decorations are excluded; runtime robot and toy colliders are checked in Play Mode.
- Lobby control deck moved up 40 canvas units. Multiplayer selection greys and disables Start Game; direct calls to the lobby StartGame handler also require Solo.
- Gameplay HUD includes a Kenney yellow health bar with numerical health, updated for damage, knockout and healing.
- Within 3 m of an ambulance (`Ambo` in the Polygon City assets), the HUD displays an H healing prompt. Press H to restore full health. Healing requires active gameplay and an operational robot; full health gets a separate message.
- Restart Round returns the player to the position and rotation captured at initialization and clears movement velocity.
- Dedicated yellow preview materials are saved on lobby and gameplay player renderers, so yellow appears before Play. Runtime color selection remains available.

## Reproduce

Unity menu: **Tools > RoboSeek > Apply Critical Physics Fixes** refreshes scene colliders and preview materials and writes the audit. Save current scene work before using this editor maintenance command: it opens and saves the gameplay and lobby scenes.

Batch Play Mode checks: execute `RoboSeekCriticalFixes.Test` (without `-quit`; the probe exits Unity with the test result). Checks cover the multiplayer guard, lobby placement, health display, solid toys, rejection cleanup, ambulance H input, large movement sweeps against thin obstacles/robots/toys and restart spawn restoration.

Validation result: all 30 checks passed in Unity 6000.3.9f1. See `CriticalFixesValidation.txt`. H keyboard state was injected for the unfocused batch editor; the normal health input handler was exercised.
