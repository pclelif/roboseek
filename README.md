# RoboSeek

[![Unity 6000.3.9f1](https://img.shields.io/badge/Unity-6000.3.9f1-black?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%2017.3-informational)](https://unity.com/srp/universal-render-pipeline)
[![Multiplayer](https://img.shields.io/badge/Netcode-NGO%202.0-blue)](https://docs-multiplayer.unity3d.com/)

RoboSeek is a fast-paced, stylized 3D action-adventure and scavenger hunt game built with Unity 6. Play as an agile robot explorer, track down hidden collectible toys across low-poly worlds, and outmaneuver or fight rogue patrol robots. Each world has its own traversal mechanic, from jetpack flight to double jumping, dashing, and driving.

## Demo

Curious how it plays? [Watch the gameplay video](https://youtu.be/oCGiVz1zFeE) to see the robot explore the maps, fly with the jetpack, and take on the patrol robots.

## How a Round Works

1. **Lobby:** Inspect your robot on a rotating showcase, pick an accent color, then choose a map and a game mode, either Solo or Multiplayer.
2. **Round Reveal:** A short 3D preview shows the targets to find, followed by a 3-2-1-GO countdown.
3. **Scavenger Hunt:** Cross vertical terrain, avoid hazards such as battlement traps and falling chasms, solve small puzzles with portals and pressure plates, and collect 12 hidden toys.
4. **Combat and Survival:** Patrol robots defend their sectors. Strike them in melee or run before your health runs out.
5. **Scoring:** Points depend on retrieval speed, accuracy, and combat performance within the time limit.

## Features

- **Four Worlds:** Hand-crafted maps with NavMesh patrol routes, recovery zones, and their own traversal rules.
- **Traversal:** Jetpack flight and gliding, double jump and sprint, a cooldown-based dash, and vehicle driving with AI traffic.
- **Enemy AI:** Patrol robots with line-of-sight detection, aggression zones, and coordinated attack slots.
- **Character and Camera:** Capsule collision with stepped slope handling and a smooth Cinemachine third-person camera.
- **Interface:** Target HUD with live 3D previews, contextual interaction prompts, and pause and settings panels.
- **Multiplayer:** Synchronized scavenger hunts built on Netcode for GameObjects and Unity Relay.
- **Settings:** Resolution, quality, fullscreen, VSync, separate audio channels, mouse sensitivity, and language selection.

## Maps

| Map | Scene | Theme Color | Signature Mechanic | Highlights |
| --- | --- | --- | --- | --- |
| Polygon | `RoboSeek_PolygonStarter` | Blue `#266BC7` | Jetpack flight | Vertical arena with 8 refueling decks between 4 m and 19 m, 4 ground targets, and automatic recharge on landing |
| Adventure | `RoboSeek_Adventure` | Green `#29A329` | Double jump and sprint | Village and snow clearing linked by portals, bouncy mushroom pads, and falling timber crossings |
| Castle | `RoboSeek_Polygon` | Red `#BC1F1F` | Combat dash | Medieval fortress with a pressure plate that opens the portcullis, battlement stairs, and multi-phase floor traps |
| City | `RoboSeek_City` | Yellow `#F0B81A` | Vehicles | Open city with driveable cars and trucks, traffic, and police NPC chases |

### Safety Rules

- Falling below the safety height, for example under -8 m, or leaving the map boundary teleports the player back to the spawn point.
- Elevated targets need vertical proximity and a clear line of sight, so nothing can be collected through floors or walls.

## Controls

| Action | Keyboard and Mouse | Gamepad and Notes |
| --- | --- | --- |
| Move | `WASD` or arrow keys | Left stick |
| Look | Mouse | Right stick |
| Jump | `Space` | Double tap for a double jump on Adventure |
| Jetpack thrust and glide | Hold `Space`, release to glide | Polygon only |
| Sprint | Left Shift | Adventure only |
| Dash | `F` | Castle only, 1.15 second cooldown |
| Interact and collect | `E` | A prompt appears near targets |
| Melee attack | `Q` | Hits nearby patrol robots |
| Enter or drive a vehicle | `E` or `F` | City only |
| Pause | `Escape` | Also goes back in menus |

## Requirements

- [Unity Hub](https://unity.com/download)
- Unity `6000.3.9f1`
- Universal Render Pipeline 17.3
- Desktop target: Windows, macOS, or Linux

Using the exact editor version is recommended because Unity projects can be changed during automatic upgrades.

## Getting Started

1. Clone the repository:

   ```bash
   git clone https://github.com/pclelif/roboseek.git
   ```

2. Open Unity Hub and select **Add > Add project from disk**.
3. Choose the cloned repository directory.
4. Open the project with Unity `6000.3.9f1` and wait for package import to finish.
5. Open `Assets/_Project/Scenes/RoboSeek_Lobby.unity` and press **Play**.
6. Pick a robot color and a map, then start hunting.

## Project Structure

```text
Assets/_Project/Scripts/
├── AI/             # Pathfinding utilities and behavior trees
├── Audio/          # AudioManager and AudioLibrary
├── Combat/         # Health, damage interfaces, and melee attacks
├── Core/           # MapManager, MapDefinition, GameBootstrapper, SaveSystem
├── Environment/    # Portals, collapsing bridges, launch pads, and traps
├── GameMode/       # Solo and multiplayer routing
├── Gameplay/       # Round loops, scores, and mission criteria
├── Input/          # Input System reader and input gating
├── Multiplayer/    # Netcode sync, Relay, and NetworkRoundManager
├── NPC/            # Robot controller, spawn manager, and attack slots
├── ObjectHunt/     # Collectible targets, round manager, and target HUD
├── Player/         # Movement, jetpack, and camera
├── UI/             # UI state, settings, and HUD
└── Tests/          # Play probes and validation harnesses
```

### Key Systems

- **MapManager:** Central registry for map configurations, spawn points, theme palettes, and toy locations.
- **ObjectHuntRoundManager:** Runs the round states `Idle`, `TargetReveal`, `Countdown`, `Active`, and `RoundOver`, and handles target selection, collection checks, and scoring.
- **NpcRobotController:** NavMesh state machine with `Patrol`, `Investigate`, `Chase`, `Attack`, and `Stunned`. Attack slots are exclusive, so enemies attack one at a time instead of swarming.
- **GameplayInputGate and UIStateManager:** Handle modal focus and cursor locking, and disable character input during transitions.

## Multiplayer

- **Netcode for GameObjects 2.0:** Host and client architecture with synchronized robot movement and animation.
- **Unity Relay:** Session connections through Unity Gaming Services, with no port forwarding needed.
- **Shared Rounds:** Round clocks, collectible targets, and player transforms are synchronized across players.

## Testing

Automated probes can run in batch mode:

```bash
# Rebuild navigation and layout assets for all maps
Unity -batchmode -projectPath . -executeMethod PlayableMapBuilder.BuildAll -logFile /tmp/map-build.log

# Run the full play mode validation
Unity -batchmode -projectPath . -executeMethod PlayableMapValidation.RunBatch -logFile /tmp/map-play.log

# Run the validation for a single map
ROBOSEEK_TEST_MAP=Adventure Unity -batchmode -projectPath . -executeMethod PlayableMapValidation.RunBatch -logFile /tmp/map-adventure.log
```

The probes check:

- UI isolation and theme matching
- Grounded spawns and slope handling
- Camera tracking
- Jetpack fuel, double jump, and dash cooldown
- Boundary enforcement and respawns
- Target collection and round restarts

## Third-Party Assets and License

- 3D environment and character art comes from the Polygon series by Synty Studios, and UI assets come from Kenney.
- The Inter font family is used under the [SIL Open Font License](https://scripts.sil.org/OFL).
- Built with Unity 6, the Universal Render Pipeline, and Cinemachine.

No license is currently granted for the original source code or project-specific artwork in this repository. Before redistributing the project or publishing a build, review the license terms of every third-party asset.
