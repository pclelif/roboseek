# FAZ 8 DEMO-2 Multiplayer Test Guide

## Overview

`DEMO-2.unity` is a duplicated version of the full `DEMO` scene (`Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity`) configured with Phase 8 multiplayer network components.

- **Source Scene**: `Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity` (Untouched)
- **Multiplayer Scene**: `Assets/_Project/Scenes/Multiplayer/DEMO-2.unity`

---

## How to Generate or Refresh DEMO-2

In Unity Editor, run:
**Tools > Robot Hunt > Phase 8 > Build DEMO-2 Multiplayer Scene**

This command automatically:
1. Copies `Demo.unity` to `DEMO-2.unity`.
2. Removes static single-player `RobotPlayer` objects.
3. Instantiates `PlayerServices` (`RobotColorService`).
4. Instantiates `SpawnManager` with 4 spawn points (`SpawnPoint_01` .. `SpawnPoint_04`).
5. Instantiates `NetworkManager` (`NetworkManager`, `UnityTransport`, `NetworkSessionController`, `NetworkDebugLauncher`, `NetworkRobotPlayer` prefab).
6. Registers `DEMO-2.unity` in `EditorBuildSettings`.

---

## Running Multiplayer Tests in DEMO-2

1. Open `Assets/_Project/Scenes/Multiplayer/DEMO-2.unity`.
2. Open **Window > Multiplayer > Multiplayer Play Mode**.
3. Enable the Main Editor and 1 (or more) Virtual Players.
4. Assign `Host` tag to Main Editor and `Client` tag to Virtual Player(s).
5. Press **Play**.
6. Observe:
   - Host and Client spawn dynamically in the city environment at distinct spawn points.
   - Local camera automatically attaches to each player's local instance.
   - Movement (WASD/Shift) and rotation sync across network.
   - Player color customization syncs automatically across host and clients.
