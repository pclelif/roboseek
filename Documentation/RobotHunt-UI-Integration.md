# Production UI integration

The production root is serialized into `Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity`, the gameplay scene confirmed by the user. `Assets/_Project/Scenes/UI_System_Demo.unity` is untouched. Existing HUD/showcase/mode-switcher components are retained, disabled in the gameplay scene. The scene opens in Robot Hub.

## New files

New components live under `Assets/Scripts/UI/`:

- Core: UIRootController, UIStateManager, CursorStateController, GameplayInputGate, UIView, TargetVisualLibrary.
- Gameplay: TargetHUD, InteractionPromptUI, InteractionStateAdapter, FeedbackMessageUI, TutorialHintUI.
- Round: RoundIntroController, ResultScreenController.
- Pause: PauseMenuController, ConfirmDialogController.
- Settings: SettingsPanelController, AudioSettingsController, GraphicsSettingsController.
- Controls: ControlsPanelController.
- Menu: MainMenuController, RobotHubController, RobotShowcaseController.
- Editor/Tests: scene inspection, guarded installation/validation, and Play Mode integration checks.

The existing `Assets/_Project/Scripts/UI/Settings/SettingsManager.cs` remains the single settings store; a second SettingsManager was not created. Existing preferences and API callers remain compatible.

## Existing files modified

- `Assets/ThirdParty/SyntyStudios/PolygonCity/Scenes/Demo.unity`: serialized production UI, one EventSystem, legacy UI disabled, manual start and realtime presentation enabled.
- `Assets/_Project/Scripts/ObjectHunt/ObjectHuntRoundManager.cs`: interaction gate, success event, round-end cleanup API. Existing selection/query/spawning rules remain unchanged.
- `Assets/_Project/Scripts/ObjectHunt/RoundGameLoop.cs`: restart/lobby APIs, opt-in realtime presentation delays and failed-preparation guard.
- `Assets/_Project/Scripts/ObjectHunt/CollectibleTarget.cs`: release its movement lock on disable, including round cancellation. Play Mode confirmed that the previous restoration after `SetActive(false)` was unreachable because that stops the coroutine. Retrieval detection, travel/gesture/shrink and notifications are retained.
- `Assets/_Project/Scripts/ObjectHunt/ObjectHuntHUD.cs`: a disabled legacy component no longer constructs its runtime Canvas in Awake.
- `Assets/_Project/Scripts/Input/PlayerInputReader.cs`: expose the existing action asset for control labels and clear buffered input on UI transitions.
- `Assets/_Project/Scripts/Player/Camera/ThirdPersonCameraController.cs`: external UI ownership/input gate, sensitivity multiplier and successful-look observation event.
- `Assets/_Project/Scripts/Combat/CombatAttack.cs`: attack observation event and pending-attack cancellation for round transitions; damage rules unchanged.
- `Assets/_Project/Scripts/NPC/NpcRobotController.cs`: publish existing encounter state transitions for tutorial hints; detection/navigation unchanged.
- `Assets/_Project/Scripts/Score/ScoreManager.cs`: discard unfinished progress on restart without adding it to accumulated score.
- `Assets/_Project/Scripts/UI/Settings/SettingsManager.cs`: preference validation, early initialization and singleton cleanup; existing keys and APIs retained.

Inter Regular/SemiBold font assets and their SIL Open Font License are bundled under `Assets/UI/Resources/RobotHuntUI/` for readable UI typography. Unity metadata accompanies the new scripts/assets.

## Data and lifecycle

`TargetHUD` subscribes to `ObjectHuntRoundManager.RoundStarted` and `TargetCollected`. It initializes from `SelectedTargets`/`CollectedTargets`, matches `objectId`, shows a geometry checkmark and a short pulse, and clears all found states and feedback on round preparation/end. It never selects or spawns gameplay targets. A separate preview library uses the selected definition's actual icon or mesh/material data from its actual prefab, with owned cameras/render textures and explicit cleanup.

`InteractionStateAdapter` adapts the existing `GetNearestInteractable` query in LateUpdate. It does not reproduce range or collision rules. The manager's minimal `InteractionSucceeded` event hides the prompt synchronously when TryCollect succeeds, before the retrieval animation finishes. Round/pause state also gates both the prompt and manager interaction, including the manager's direct E-key fallback.

`RoundIntroController` presents targets supplied during the existing loop's Targets phase. Existing CountdownChanged events supply 3/2/1/GO. The existing loop uses an opt-in realtime presentation clock in this scene so the world can remain frozen during the introduction. Target selection, spawn positions, retrieval animation and result calculation stay in their existing owners.

`UIStateManager` owns navigation and a screen history stack. Settings and Controls are one scene instance each, returning to their caller. Gameplay Escape opens Pause; modal Escape goes back one level. Confirmed restart and next round enter the existing intro/countdown flow. Confirmed quit ends targets and returns to the in-scene Hub without loading the demo scene. Local gameplay time is restored on resume and teardown; a listening network clock is not frozen.

`GameplayInputGate` uses existing component enable state, PlayerInputReader.SetPaused and the target manager's interaction gate. Disabling movement at the component level prevents pickup/combat coroutines from accidentally enabling movement through an input-blocking modal. The camera exposes a small external cursor/input ownership API while retaining its existing Cinemachine pipeline. Buffered input is cleared at state transitions.

Results read `RoundResultData.elapsedTime` and the scene's existing round score. If a ScoreManager is present, the existing ScoreManager round score takes precedence, matching the legacy results' preference. No scoring formula is added. Color changes call the existing RobotColorCustomizer API and retain its preference key.

## Settings and integration limits

Master, music, SFX and mouse sensitivity use the existing PlayerPrefs store. Sensitivity is applied as a multiplier to the existing camera values. Graphics options use Screen.resolutions, supported desktop fullscreen modes, QualitySettings.names and desktop VSync. Unsupported display modes are hidden in editor/mobile/browser contexts. Graphics have separate persistent keys with validation against the current platform.

The current scene contains no music/SFX AudioSources or AudioMixer. Master volume is functional. Music/SFX preferences are persisted and supported through `AudioSettingsController.mixer` (exposed `MusicVolume`/`SFXVolume` dB parameters) or its explicit `musicSources`/`sfxSources` arrays. Actual category playback needs those existing/future audio assets routed there; no music or audio engine was invented.

The pre-existing missing script on `World` and one unused missing TeddyBear prefab catalog entry are documented in the inspection report and not removed. Multiplayer lobby/authority integration is outside this single-player scene installation; network gameplay scripts and multiplayer scenes are untouched.

## Validation

See `RobotHunt-UI-Playmode.txt` for the actual Unity Play Mode checks and `RobotHunt-UI-Analysis.md` for the pre-integration inspection. The installer refuses to overwrite an existing production root and checks the full gameplay scene path before saving. `Robot Hunt/UI/Validate Production UI` checks the installed scene references and single EventSystem.

The automated tests use the real gameplay scene and existing retrieval animation, not fabricated target state. Coverage includes Hub navigation, modal Escape ordering, settings persistence, target reveal/countdown, input blocking, no-target prompt visibility, successful retrieval, movement-lock cleanup, restart during collection, HUD reset, result values, timeout, next round and return to Hub. The test harness restores player settings/tutorial preferences after execution. Cursor ownership is asserted, but OS cursor confinement still needs a focused Game View/player window; batch mode cannot validate that OS behavior.

Final validation: Unity 6000.3.9f1 compiled the changed scripts successfully. The final Play Mode run completed 56 assertions plus its overall pass with process exit code 0, and the complete final log contained no C# compilation errors or runtime exceptions, including shutdown. URP captures are saved in `UI-Previews/`. These captures render the UI through a temporary camera for automation; the production Canvas remains Screen Space Overlay.
