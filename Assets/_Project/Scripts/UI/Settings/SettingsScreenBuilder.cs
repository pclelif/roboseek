using UnityEngine;
using UnityEngine.UI;
using Robot.Input;
using Robot.Player.CameraControl;
namespace Robot.UI.Production
{
public static class SettingsScreenBuilder
{
    private static Transform Modal(UIRootController root, UIScreen screen, string name, Vector2 size, float dim = .2f, Vector2 position = default)
    {
        var layer = UIView.Screen(name, root.transform, dim); root.state.screens[(int)screen] = layer;
        return UIView.Panel("Panel", layer.transform, size, Robot.UI.UITheme.GlassDark, position).transform;
    }
    public static void Build(UIRootController root, PlayerInputReader input, ThirdPersonCameraController camera)
    {
        var panel = Modal(root, UIScreen.Settings, "SettingsPanel", new Vector2(760, 650));
        UIView.Label("Title", panel, "SETTINGS", 32, new Vector2(650, 60), new Vector2(0, 253));
        root.settings = root.gameObject.AddComponent<SettingsPanelController>(); root.settings.state = root.state; root.settings.cameraController = camera;
        root.graphics = root.gameObject.AddComponent<GraphicsSettingsController>(); root.settings.graphics = root.graphics;
        root.audioSettings = root.gameObject.AddComponent<AudioSettingsController>();
        root.settings.categories = new GameObject[3]; root.categories = new Button[3];
        string[] titles = { "AUDIO", "GRAPHICS", "CONTROLS" };
        for (int i = 0; i < 3; i++)
        {
            root.categories[i] = UIView.Button(titles[i], panel, new Vector2((i - 1) * 220, 170), new Vector2(206, 44));
            root.settings.categories[i] = UIView.Rect(titles[i], panel, new Vector2(650, 330), new Vector2(0, -40)).gameObject;
        }
        var audio = root.settings.categories[0].transform;
        root.settings.master = UIView.Slider("Master Volume", audio, 80, 0, 1); root.settings.music = UIView.Slider("Music Volume", audio, 0, 0, 1); root.settings.sfx = UIView.Slider("SFX Volume", audio, -80, 0, 1);
        var graphics = root.settings.categories[1].transform;
        root.settings.display = UIView.Button("DISPLAY MODE", graphics, new Vector2(0, 103), new Vector2(580, 46));
        root.settings.resolution = UIView.Button("RESOLUTION", graphics, new Vector2(0, 38), new Vector2(580, 46));
        root.settings.quality = UIView.Button("QUALITY", graphics, new Vector2(0, -27), new Vector2(580, 46));
        root.settings.vsync = UIView.Button("VSYNC", graphics, new Vector2(0, -92), new Vector2(580, 46));
        var controlsCategory = root.settings.categories[2].transform;
        root.settings.sensitivity = UIView.Slider("Mouse Sensitivity", controlsCategory, 105, .2f, 3f);
        root.settingsControls = UIView.Button("VIEW CONTROLS", controlsCategory, new Vector2(0, -139), new Vector2(300, 42));
        root.controls = root.gameObject.AddComponent<ControlsPanelController>(); root.controls.input = input; root.controls.settingBindings = new Text[9];
        for (int i = 0; i < 9; i++) root.controls.settingBindings[i] = UIView.Label("Binding" + i, controlsCategory, "", 14, new Vector2(300, 28), new Vector2(i % 2 == 0 ? -160 : 160, 48 - (i / 2) * 36));
        root.settingsBack = UIView.Button("BACK", panel, new Vector2(0, -272), new Vector2(240, 44));
        var controlsPanel = Modal(root, UIScreen.Controls, "ControlsPanel", new Vector2(660, 650));
        UIView.Label("Title", controlsPanel, "CONTROLS", 32, new Vector2(600, 60), new Vector2(0, 255));
        root.controls.keys = new Text[9];
        string[] keys = { "W A S D / ARROWS", "SPACE", "MOUSE", "E", "Q", "H", "R", "T", "ESC" };
        string[] actions = { "MOVE", "JUMP", "CAMERA", "SCAN & RETRIEVE", "ATTACK", "HEAL", "RADAR", "TURBO", "PAUSE" };
        for (int i = 0; i < 9; i++)
        {
            float y = 195 - i * 48;
            root.controls.keys[i] = UIView.Keycap(controlsPanel, keys[i], new Vector2(-135, y), new Vector2(225, 40));
            UIView.Label("Action" + i, controlsPanel, actions[i], 17, new Vector2(260, 40), new Vector2(135, y));
        }
        root.controlsBack = UIView.Button("BACK", controlsPanel, new Vector2(0, -263), new Vector2(240, 44));
        foreach (var screen in new[] { UIScreen.Settings, UIScreen.Controls })
            foreach (var button in root.state.screens[(int)screen].GetComponentsInChildren<Button>(true))
                UIView.StyleButton(button, UIView.AccentYellow, true);
    }
}
}
