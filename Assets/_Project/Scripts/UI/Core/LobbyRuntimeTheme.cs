using UnityEngine;
using UnityEngine.UI;
using Robot.Robots.Customization;

namespace Robot.UI.Production
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-50)]
    public sealed class LobbyRuntimeTheme : MonoBehaviour
    {
        private Texture2D backdrop;
        private Material backdropMaterial, stageMaterial;

        // The serialized Lobby scene is the approved visual composition.  This
        // component used to rebuild and reposition it at runtime, so Play mode
        // looked different from the scene preview. Runtime updates map colours
        // and default map/robot/music behaviour without rebuilding the layout.
        private void Awake()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall -= DelayApply;
            UnityEditor.EditorApplication.delayCall += DelayApply;
        }

        private void DelayApply()
        {
            if (this != null) Apply();
        }
#endif

        private void OnEnable()
        {
            Robot.Core.MapManager.ResetToDefaultMap();
            Apply();
            if (Application.isPlaying)
            {
                Robot.Audio.AudioManager.Instance?.PlayLobbyMusic();
            }
            Robot.Core.MapManager.MapChanged -= OnMapChanged;
            Robot.Core.MapManager.MapChanged += OnMapChanged;
            UpdateMapTheme(Robot.Core.MapManager.SelectedMap);
        }

        private void OnDisable()
        {
            Robot.Core.MapManager.MapChanged -= OnMapChanged;
        }

        private void OnMapChanged(Robot.Core.MapDefinition map)
        {
            UpdateMapTheme(map);
        }

        private void UpdateMapTheme(Robot.Core.MapDefinition map)
        {
            if (map == null) return;

            Color bgColor = GetMapBackgroundColor(map);

            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = bgColor;
            }

            var wall = GameObject.Find("RearWallCore");
            if (wall != null)
            {
                var ren = wall.GetComponent<Renderer>();
                if (ren != null)
                {
                    Material mat = Application.isPlaying ? ren.material : ren.sharedMaterial;
                    if (mat != null)
                    {
                        mat.SetTexture("_BaseMap", null);
                        mat.SetTexture("_MainTex", null);
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", bgColor);
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", bgColor);
                    }
                }
            }

            var root = GetComponent<UIRootController>();
            if (root != null)
            {
                var title = root.transform.Find("LobbyScreen/BrandHeaderBanner/Title")?.GetComponent<Text>();
                if (title != null) title.color = map.themeColor;
                var deck = root.transform.Find("LobbyScreen/RightControlDeck");
                if (deck != null)
                {
                    var modeHeader = deck.Find("ModeHeader")?.GetComponent<Text>();
                    var colorHeader = deck.Find("ColorHeader")?.GetComponent<Text>();
                    if (modeHeader != null) modeHeader.color = map.themeColor;
                    if (colorHeader != null) colorHeader.color = map.themeColor;
                }
                if (root.hubSettings != null) UIView.StyleButton(root.hubSettings, map.themeColor, true);
                if (root.hubControls != null) UIView.StyleButton(root.hubControls, map.themeColor, true);
                if (root.language != null) UIView.StyleButton(root.language, map.themeColor, true);
            }
        }

        private Color GetMapBackgroundColor(Robot.Core.MapDefinition map)
        {
            if (map == null) return new Color(0.94f, 0.78f, 0.38f);
            switch (map.mapId)
            {
                case Robot.Core.MapType.City:
                    return new Color(0.94f, 0.78f, 0.38f); // Warm Golden Cream Yellow (Default)
                case Robot.Core.MapType.Adventure:
                    return new Color(0.50f, 0.75f, 0.38f); // Warm Natural Leaf Green (Zero mint)
                case Robot.Core.MapType.Polygon:
                    return new Color(0.76f, 0.30f, 0.28f); // Zengin Mat Sıcak Kırmızı Arka Plan (%0 Pembe, sıfır koral pembe!)
                case Robot.Core.MapType.PolygonStarter:
                    return new Color(0.62f, 0.80f, 0.95f); // EFSO MAVİ (Aynen korundu)
                default:
                    return map.themeColor;
            }
        }

        public void Apply()
        {
            var root = GetComponent<UIRootController>();
            if (root == null) return;
            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.fieldOfView = 39f;
                camera.transform.position = new Vector3(0.30f, 1.35f, -3.85f);
                camera.transform.rotation = Quaternion.Euler(8.0f, -14.0f, 0f);
            }
            BuildBackdrop();
            RefineStage();
            SetupFloatingToys();
            UpdateMapTheme(Robot.Core.MapManager.SelectedMap);
            var panel = root.transform.Find("LobbyScreen/RightControlDeck") as RectTransform;
            if (panel == null) return;
            panel.sizeDelta = new Vector2(590, 740);
            panel.anchoredPosition = new Vector2(535, 35);
            var panelImg = panel.GetComponent<Image>();
            if (panelImg != null)
            {
                var panelSprite = Robot.UI.UITheme.GetPanelGlass() ?? Robot.UI.UITheme.GetPanelRectangle();
                if (panelSprite != null)
                {
                    panelImg.sprite = panelSprite;
                    panelImg.type = Image.Type.Sliced;
                }
                panelImg.color = new Color(.07f, .09f, .14f, .94f);
            }

            SetLabel(panel, "DeckLabel", "LOBBY", 56, new Vector2(520, 80), new Vector2(0, 299), Color.white);
            SetLabel(panel, "ModeHeader", "GAME MODE", 18, new Vector2(510, 28), new Vector2(0, 227), new Color(1f, 0.85f, 0.35f, 1f));
            Place(root.hubSolo, -130, 175, 250, 52, Robot.UI.UITheme.AccentYellow, true);
            Place(root.hubMultiplayer, 130, 175, 250, 52, new Color(0.14f, 0.16f, 0.20f, 0.95f), false);

            var desc = panel.Find("ModeDescBox") as RectTransform;
            if (desc != null)
            {
                desc.sizeDelta = new Vector2(510, 44);
                desc.anchoredPosition = new Vector2(0, 113);
                var descImg = desc.GetComponent<Image>();
                if (descImg != null)
                {
                    var descSprite = Robot.UI.UITheme.GetPanelRectangle();
                    if (descSprite != null) { descImg.sprite = descSprite; descImg.type = Image.Type.Sliced; }
                    descImg.color = new Color(.04f, .06f, .09f, .85f);
                }
            }
            if (root.hub != null && root.hub.modeDescription != null)
            {
                root.hub.modeDescription.fontSize = 15;
                root.hub.modeDescription.color = Robot.UI.UITheme.TextMuted;
                root.hub.modeDescription.rectTransform.sizeDelta = new Vector2(490, 42);
            }

            Place(root.startGame, 0, 35, 510, 68, Robot.UI.UITheme.AccentYellow, true);
            SetLabel(panel, "ColorHeader", "ROBOT COLOR", 18, new Vector2(510, 28), new Vector2(0, -37), new Color(1f, 0.85f, 0.35f, 1f));
            Place(root.nextColor, 0, -87, 510, 52, Robot.UI.UITheme.AccentYellow, true);
            Place(root.hubSettings, -130, -158, 250, 52, Robot.UI.UITheme.AccentYellow, true);
            Place(root.hubControls, 130, -158, 250, 52, Robot.UI.UITheme.AccentYellow, true);
            Place(root.language, 0, -229, 510, 52, Robot.Core.MapManager.SelectedMap != null ? Robot.Core.MapManager.SelectedMap.themeColor : Robot.UI.UITheme.AccentYellow, true);
            Place(root.hubBack, 0, -300, 510, 48, new Color(0.14f, 0.16f, 0.20f, 0.95f), false);

            var banner = root.transform.Find("LobbyScreen/BrandHeaderBanner") as RectTransform;
            if (banner != null)
            {
                banner.sizeDelta = new Vector2(680,160);
                banner.anchoredPosition = new Vector2(-335,340);
                banner.GetComponent<Image>().color = Color.clear;
                var activeMap = Robot.Core.MapManager.SelectedMap;
                SetLabel(banner, "Title", "ROBOSEEK", 88, new Vector2(680,100), new Vector2(0,25), activeMap != null ? activeMap.themeColor : UIView.AccentYellow);
                SetLabel(banner, "Subtitle", "WHERE IS MY TOY?", 26, new Vector2(650,44), new Vector2(0,-35), Color.white);
                foreach (var label in banner.GetComponentsInChildren<Text>())
                { var shadow = label.GetComponent<Shadow>() ?? label.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0,0,0,.85f); shadow.effectDistance = new Vector2(2,-2); }
            }
            var lobbyImage = panel.parent.GetComponent<Image>();
            if (lobbyImage != null) lobbyImage.color = Color.clear;
            UpdateMapTheme(Robot.Core.MapManager.SelectedMap);
        }

        private void BuildBackdrop()
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "Layered_Back_Wall" || t.name.StartsWith("FloorPanel_") ||
                    t.name.StartsWith("TechnicalLabel_") || t.name.StartsWith("PanelSeam_") ||
                    t.name.StartsWith("ServiceBay") || t.name.StartsWith("FloorGuide") ||
                    t.name.StartsWith("TurntableLatch_") || t.name.StartsWith("Conduit") || t.name.StartsWith("AmberStatus_"))
                    t.gameObject.SetActive(false);
            }

            var wall = GameObject.Find("RearWallCore");
            if (wall != null)
            {
                var savedMat = Resources.Load<Material>("RobotHuntUI/PolishRearWallCore");
#if UNITY_EDITOR
                if (savedMat == null)
                {
                    savedMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/UI/Resources/RobotHuntUI/PolishRearWallCore.mat");
                }
#endif
                if (savedMat != null)
                {
                    wall.transform.position = new Vector3(-0.90f, 1.85f, 2.7f);
                    wall.transform.localScale = new Vector3(16f, 9f, .2f);
                    var ren = wall.GetComponent<Renderer>();
                    if (ren != null)
                    {
                        if (Application.isPlaying) ren.material = savedMat;
                        else ren.sharedMaterial = savedMat;
                    }
                }
            }
        }

        private void RefineStage()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.55f, .55f, .55f);
            if (stageMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null) stageMaterial = new Material(shader);
            }
            if (stageMaterial != null)
            {
                stageMaterial.SetColor("_BaseColor", new Color(.035f, .035f, .035f));
                stageMaterial.SetFloat("_Metallic", .12f);
                stageMaterial.SetFloat("_Smoothness", .24f);
            }

            var floor = GameObject.Find("IndustrialFloor");
            if (floor != null && stageMaterial != null)
            {
                floor.transform.position = new Vector3(-1.10f, -0.51f, 2.0f);
                floor.transform.localScale = new Vector3(14.0f, 0.18f, 10.0f);
                var ren = floor.GetComponent<Renderer>();
                if (ren != null)
                {
                    if (Application.isPlaying) ren.material = stageMaterial;
                    else ren.sharedMaterial = stageMaterial;
                }
            }
            var platform = GameObject.Find("RobotShowcasePlatform");
            if (platform == null) return;
            // Robot floating lower so it doesn't appear too high on screen
            platform.transform.position = new Vector3(-1.90f, -0.35f, 0f);
            var motion = platform.GetComponent<LobbyAmbientMotion>() ?? platform.AddComponent<LobbyAmbientMotion>();
            motion.rotationSpeed = 0f;
            motion.bobSpeed = 1.4f;
            motion.bobAmount = 0.035f;

            var deck = platform.transform.Find("DeploymentTurntable");
            if (deck != null) deck.gameObject.SetActive(false); // Platform disc removed

            // Live robot — animator stays ENABLED so RobotColorCustomizer works fully.
            // Jump_Air plays on a loop giving a natural hover/flight look.
            var liveRobot = platform.GetComponentInChildren<RobotColorCustomizer>();
            if (liveRobot != null)
            {
                liveRobot.transform.localPosition = Vector3.zero;
                // -20° X tilt counters Jump_Air forward-lean; 200° Y for diagonal hero angle
                liveRobot.transform.localRotation = Quaternion.Euler(-20f, 200f, 0f);
                liveRobot.transform.localScale = Vector3.one * .87f;

                foreach (var cc in liveRobot.GetComponentsInChildren<CharacterController>(true)) cc.enabled = false;
                foreach (var rmc in liveRobot.GetComponentsInChildren<Robot.Player.Movement.RobotMovementController>(true)) rmc.enabled = false;
                foreach (var safety in liveRobot.GetComponentsInChildren<Robot.Player.Movement.WorldSafety>(true)) safety.enabled = false;
                foreach (var body in liveRobot.GetComponentsInChildren<Robot.Player.Movement.SolidRobotBody>(true)) body.enabled = false;
                foreach (var rb in liveRobot.GetComponentsInChildren<Rigidbody>(true)) { rb.isKinematic = true; }

                var animator = liveRobot.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.enabled = true;
                    animator.applyRootMotion = false;
                    animator.Play("Jump_Air", 0, 0.5f); // Loop hover animation — never freeze
                }

                liveRobot.ApplyTheme(RobotColorCustomizer.ColorTheme.Sari);
            }
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            { light.color = Color.white; if (light.name.Contains("Cyan")) light.intensity = .4f; }
        }

        private static void Place(Button button, float x, float y, float w, float h, Color? baseColor = null, bool darkText = false)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);

            if (button.image != null)
            {
                var btnSprite = Robot.UI.UITheme.GetButtonRectangle();
                if (btnSprite != null)
                {
                    button.image.sprite = btnSprite;
                    button.image.type = Image.Type.Sliced;
                }
                button.image.color = baseColor ?? Robot.UI.UITheme.AccentYellow;
            }

            Color surface = baseColor ?? Robot.UI.UITheme.AccentYellow;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 20;
                label.color = darkText ? new Color(.07f, .07f, .07f) : Color.white;
                label.rectTransform.sizeDelta = new Vector2(w - 20, h - 4);
                UIView.ApplyFont(label);
            }
        }

        private static void SetLabel(Transform parent,string name,string value,int size,Vector2 dimensions,Vector2 position,Color color)
        {
            var label=parent.Find(name)?.GetComponent<Text>(); if(label==null)return;
            label.text=value; label.fontSize=size; label.color=color; label.rectTransform.sizeDelta=dimensions; label.rectTransform.anchoredPosition=position;
        }
        private void SetupFloatingToys()
        {
            var collection = GameObject.Find("LobbyToyCollection");
            if (collection == null) return;
            int index = 0;
            foreach (Transform child in collection.transform)
            {
                var toy = child.GetComponent<LobbyFloatingToy>() ?? child.gameObject.AddComponent<LobbyFloatingToy>();
                toy.enabled = true;
                toy.EnsureAnchor();
                toy.bobSpeed = 1.4f;
                toy.bobAmplitude = 0.05f;
                toy.tiltAmplitude = 3.0f;
                toy.tiltSpeed = 1.0f;
                toy.phaseOffset = (index++) * 0.5f;
            }
        }

        private void OnDestroy()
        {
            if (backdrop != null) { if (Application.isPlaying) Destroy(backdrop); else DestroyImmediate(backdrop); }
            if (backdropMaterial != null) { if (Application.isPlaying) Destroy(backdropMaterial); else DestroyImmediate(backdropMaterial); }
            if (stageMaterial != null) { if (Application.isPlaying) Destroy(stageMaterial); else DestroyImmediate(stageMaterial); }
        }
    }
}
