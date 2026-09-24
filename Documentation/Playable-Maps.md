# Playable maps — 2026-09-23

Three existing environment scenes now contain authored gameplay layouts, a robot and stable Cinemachine rig, the full City-style UI/round flow, 12 toy locations, 9 connected NPC spawn positions, baked navigation and map-specific traversal. City retains its vehicle mechanics. Non-City HUDs contain targets, timer, health, pause and the relevant traversal indicator.

| Display name | Existing scene / enum | Theme | Traversal |
|---|---|---|---|
| POLYGON | RoboSeek_PolygonStarter / PolygonStarter | Blue | Hold Space: jetpack; release: glide; land: refill |
| ADVENTURE | RoboSeek_Adventure / Adventure | Green | Shift sprint, Space twice for double jump; mushroom pads |
| CASTLE | RoboSeek_Polygon / Polygon | Red | F dash, 1.15 s cooldown; battlement launch pads |
| CITY | RoboSeek_City / City | Existing City theme | Existing vehicles/police |

The historical Polygon/PolygonStarter identifiers are intentionally retained so serialized scene selection stays compatible. Q remains the existing attack binding; Castle uses F.

## Level layout

- Polygon: 4 ground targets and 8 broad refuelling decks, from 4 m to 19 m high.
- Adventure: 9 village/nature targets, 3 roof lookouts with mushroom routes.
- Castle: 9 connected town-street targets, 3 battlement lookouts with launch pads.
- Grounded road spawns: Polygon (-9, 0, 0), Adventure (8.05, 0, 12.12), Castle (0.60, 0.10, 40.90), plus 0.04 m spawn clearance.
- Falls below -8 m or outside the authored map bounds return the robot to spawn.
- Elevated toys require close vertical proximity and clear line of sight. They cannot be collected through a roof from the street.
- Pads use flat box walking surfaces; squashed sphere/capsule primitive colliders are removed because their physics bounds do not follow the flattened visual shape.

## Authoring and validation

`MapLevelLayout` stores the spawn, toy/NPC positions and safety bounds. Runtime spawning reads this component instead of the old demo coordinates. `PlayableMapBuilder` rebuilds the three maps from their existing environment art; it verifies ground-target NavMesh connectivity and solid surfaces under all 12 targets. Generated materials and NavMesh assets live in `Assets/_Project/Data/PlayableMaps`.

Use a disposable project copy for the batch tools (they save scenes or exit Unity):

```sh
Unity -batchmode -projectPath /path/to/copy -executeMethod PlayableMapBuilder.BuildAll -logFile /tmp/map-build.log
Unity -batchmode -projectPath /path/to/copy -executeMethod PlayableMapValidation.RunBatch -logFile /tmp/map-play.log
```

Rendering must be enabled for Play Mode UI capture. The play probe starts maps through the actual lobby button, checks UI isolation/theme, grounded spawn, follow target, pause/resume, traversal physics, target collection, round restart and return to lobby. NPCs are disabled after their setup for deterministic traversal measurements. `ROBOSEEK_TEST_MAP` optionally selects an enum identifier for a focused run. Full results are written to `/tmp/roboseek-map-validation.txt`.
