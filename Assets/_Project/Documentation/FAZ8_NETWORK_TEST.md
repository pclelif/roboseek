# FAZ 8 Network Foundation Test

Open `Assets/_Project/Scenes/Multiplayer/NetworkFoundationTest.unity`.

## Multiplayer Play Mode

1. Open **Window > Multiplayer > Multiplayer Play Mode**.
2. Enable the Main Editor and one Virtual Player.
3. Assign the `Host` tag to the Main Editor and the `Client` tag to the Virtual Player.
4. Enter Play Mode. Tagged instances start automatically. Without tags, use the on-screen **Start Host** and **Start Client** buttons.

## Acceptance checks

- Both instances connect and spawn at different `SpawnPoint_01..04` transforms.
- WASD/Shift movement affects only the locally owned robot and is visible in the other instance.
- Position and rotation synchronize through `OwnerNetworkTransform`.
- Color is assigned by the server and synchronizes through a server-write `NetworkVariable<int>`.
- Requesting an occupied color returns `<Color> is already taken` and assigns the next free color.
- Stopping one player removes its `NetworkObject` and releases its spawn/color reservations.

The test scene intentionally contains no Object Hunt round or multiplayer combat. FAZ 7 remains isolated until those systems are network-enabled in later phases.
