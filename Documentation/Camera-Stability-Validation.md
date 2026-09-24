# Camera stability fix (2026-09-23)

Scope: the shared camera/movement scripts, including City selected from `RoboSeek_Lobby` (`Assets/_Project/Scenes/Maps/RoboSeek_City.unity`).

## Cause and change

The Play Mode investigation found the camera actually following **SolidRobotHull** after round startup. `SolidRobotBody.Awake` copied the root's `Player` tag to this child. Tag-based lookup in round startup could therefore pass the hull to `SnapToRoundStart`. Other robots call `RefreshHull`, which recalculates this child from animated bones; running animation consequently became camera motion. The hull now stays `Untagged`, and all camera target entry points resolve children back to the movement/CharacterController root. Collision handling still uses the existing hull and parent components.

A second issue: the previous rig combined a damped OrbitalFollow position with HardLookAt aiming at the current, undamped player position. Running, stopping, terrain corrections and variable frame times therefore changed the view angle without look input. Movement read that rendered camera orientation on the next frame, feeding camera corrections back into locomotion.

The camera now uses the same input yaw/pitch for its spherical orbit and orientation. HardLookAt stays disabled, and movement uses the controller's yaw basis. Input is sampled before the orbit controller, movement runs in Update, and the Brain follows in LateUpdate. Position damping is zero: Cinemachine applies this damping in camera-offset space, so even Y-only damping would introduce world X/Z lag at nonzero pitch. No interpolated anchor is used. CharacterController's minimum move distance is zero to avoid dropping small displacements, including the map bootstrap assignment that previously overwrote this value.

Existing public APIs and the previous disabled Deoccluder state are retained. City geometry was not changed. This addresses camera-generated motion; genuine movement from collisions still moves the camera. Inspector position decimals necessarily change while running.

## Automated verification

Unity 6000.3.9f1, separate APFS copy of the project, batch mode. No scene was saved by the checks.

`CameraStabilityChecks.RunBatch` in `Assets/_Project/Scripts/Editor/Validation/CameraStabilityChecks.cs`:

- Reproduced the old pipeline with 6 m/s movement, stops/reversals, body rotation and alternating 2 mm height corrections: maximum relative offset error **0.093298 m**, angle error **0.7359 degrees**.
- Passed 36 combinations of 30/60/120 Hz and alternating frame durations, three yaw values and three pitch values. Required relative position error below 0.0001 m and orientation error below 0.08 degrees.
- Passed 120 animated-hull frames: collision proxy stays untagged, and `SetTarget`, `SnapToRoundStart`, and legacy/serialized child targets all resolve to the stationary controller root without moving the camera.
- Loaded the actual City scene and ran 240 CharacterController steps through its colliders: grounded on all 240 steps; maximum relative camera offset error **0.0000002 m**.
- Separate real Play Mode probe: bootstrapped City, started a round and moved through three input directions for **360 frames / 7.929 m**. Follow target was **RobotPlayer**, angle error **0 degrees**, maximum Main Camera relative offset error **0.0000005 m**. This used the scene's actual serialized 5 m distance / 1.2 m target offset.
- Unity compilation succeeded; no C# compilation errors. Existing warnings remain.

Command (use a disposable project copy; the check exits the editor):

```sh
Unity -batchmode -nographics -projectPath /path/to/copy -executeMethod CameraStabilityChecks.RunBatch -logFile /tmp/camera-check.log
```

These are transform/physics checks, not a visual frame-pacing or GPU rendering benchmark.

The Play Mode probe initially exposed `Follow=SolidRobotHull` before the tag/root fix. The final Play Mode run used rendering enabled; a graphics-disabled attempt was unsuitable for URP. An existing Unity Search index exception in the disposable copy did not prevent the final tests. Visual frame pacing was not evaluated.
