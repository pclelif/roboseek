using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Robot.UI.Core;
using Robot.UI.HUD;
using Robot.UI.Demo;

namespace Robot.Editor
{
    /// <summary>
    /// Automated setup script for Faz 15 UI Framework.
    /// Configures Kenney UI sprites, builds reusable UI prefabs, and generates the complete UI_System_Demo scene.
    /// </summary>
    [InitializeOnLoad]
    public static class SetupPhase15UIFramework
    {
        private const string KenneyBasePath = "Assets/ThirdParty/kenney_ui-pack-space-expansion/";
        private const string PrefabSavePath = "Assets/_Project/Prefabs/UI/";
        private const string DemoScenePath = "Assets/_Project/Scenes/UI_System_Demo.unity";
        private const string FontPath = KenneyBasePath + "Font/Kenney Future.ttf";

        static SetupPhase15UIFramework()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(DemoScenePath) || !File.Exists(PrefabSavePath + "RobotHuntHUD.prefab"))
                {
                    RunFullUISetup();
                }
            };
        }

        [MenuItem("Tools/Robot Hunt/Run Full UI Setup (Faz 15)", false, 1)]
        public static void RunFullUISetup()
        {
            Debug.Log("<color=cyan><b>[Faz 15 UI Setup]</b> Starting full UI Framework generation...</color>");
            ConfigureKenneySprites();
            CreateUIPrefabs();
            CreateDemoScene();
            Debug.Log("<color=green><b>[Faz 15 UI Setup]</b> Full UI Framework & UI_System_Demo successfully built!</color>");
        }

        [MenuItem("Tools/Robot Hunt/1. Configure Kenney UI Sprites (9-Slice)", false, 10)]
        public static void ConfigureKenneySprites()
        {
            string pngFolder = KenneyBasePath + "PNG";
            if (!Directory.Exists(pngFolder))
            {
                Debug.LogError($"[UI Setup] PNG folder not found at: {pngFolder}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { pngFolder });
            int modifiedCount = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    string fileName = Path.GetFileNameWithoutExtension(path).ToLower();
                    bool isDouble = path.Contains("/Double/");
                    int borderBase = isDouble ? 24 : 12;

                    Vector4 border = Vector4.zero;

                    if (fileName.StartsWith("panel_"))
                    {
                        border = new Vector4(borderBase, borderBase, borderBase, borderBase);
                    }
                    else if (fileName.StartsWith("button_rectangle") || fileName.StartsWith("button_square"))
                    {
                        border = new Vector4(borderBase, borderBase, borderBase, borderBase);
                    }
                    else if (fileName.StartsWith("button_square_header_"))
                    {
                        border = new Vector4(borderBase * 4 / 3, borderBase, borderBase * 4 / 3, borderBase);
                    }
                    else if (fileName.StartsWith("bar_"))
                    {
                        border = new Vector4(borderBase, borderBase / 2, borderBase, borderBase / 2);
                    }
                    else
                    {
                        border = Vector4.zero;
                    }

                    bool needsUpdate = importer.textureType != TextureImporterType.Sprite ||
                                      importer.spriteBorder != border ||
                                      !importer.alphaIsTransparency;

                    if (needsUpdate)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false;
                        importer.filterMode = FilterMode.Bilinear;
                        importer.spriteBorder = border;
                        importer.SaveAndReimport();
                        modifiedCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[UI Setup] Configured {modifiedCount} Kenney textures as 9-sliced UI Sprites.");
        }

        [MenuItem("Tools/Robot Hunt/2. Generate UI Prefabs", false, 11)]
        public static void CreateUIPrefabs()
        {
            if (!Directory.Exists(PrefabSavePath))
            {
                Directory.CreateDirectory(PrefabSavePath);
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateButtonPrefab(font);
            CreateHeaderPrefab(font);
            CreateStatusBarPrefab(font);
            CreateIndicatorPrefab(font);
            CreateReticlePrefab();
            CreateNotificationItemPrefab(font);
            CreateWindowPrefabs(font);
            CreateHUDPrefab(font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UI Setup] All UI Prefabs successfully generated in " + PrefabSavePath);
        }

        [MenuItem("Tools/Robot Hunt/3. Create UI_System_Demo Scene", false, 12)]
        public static void CreateDemoScene()
        {
            string scenesDir = "Assets/_Project/Scenes";
            if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera & Sci-Fi Environment Setup
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.07f, 0.12f, 1f);
            cam.transform.position = new Vector3(0f, 1.4f, -3.8f);
            cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            camObj.tag = "MainCamera";

            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.90f, 0.95f, 1.0f);
            light.intensity = 1.1f;
            lightObj.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            GameObject rimLightObj = new GameObject("Rim Light (Cyan)");
            Light rimLight = rimLightObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(0.1f, 0.7f, 1.0f);
            rimLight.intensity = 0.5f;
            rimLightObj.transform.rotation = Quaternion.Euler(-20f, 140f, 0f);

            // 2. 3D Showcase Turntable (Clean Robot Only)
            GameObject turntableObj = new GameObject("ShowcaseTurntable");
            turntableObj.transform.position = Vector3.zero;

            // Pedestal Platform
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(turntableObj.transform, false);
            pedestal.transform.position = new Vector3(0f, -0.05f, 0f);
            pedestal.transform.localScale = new Vector3(2.2f, 0.08f, 2.2f);
            var pedRend = pedestal.GetComponent<Renderer>();
            if (pedRend != null) pedRend.sharedMaterial.color = new Color(0.10f, 0.14f, 0.20f);

            // Robot model on turntable
            GameObject robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Robot/FreeLowPolyRobot/Meshes_and_Animations/RandomModularRobots_Prefab.prefab")
                                 ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Robot/FreeLowPolyRobot/Meshes_and_Animations/Modular_Parts.prefab");
            if (robotPrefab != null)
            {
                GameObject robotInst = (GameObject)PrefabUtility.InstantiatePrefab(robotPrefab, turntableObj.transform);
                robotInst.transform.localPosition = Vector3.zero;
                robotInst.transform.localRotation = Quaternion.Euler(0f, 165f, 0f);
            }

            // 3. EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();

            // 4. Canvas & UIRoot
            GameObject canvasObj = new GameObject("Canvas_UI_Root");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 5. UIRoot Layers
            GameObject uiRootObj = new GameObject("UIRoot", typeof(RectTransform));
            uiRootObj.transform.SetParent(canvasObj.transform, false);
            StretchFull(uiRootObj.GetComponent<RectTransform>());

            GameObject hudLayer = CreateLayer(uiRootObj.transform, "HUDLayer");
            GameObject windowsLayer = CreateLayer(uiRootObj.transform, "WindowsLayer");
            GameObject modalBackdrop = CreateModalBackdrop(uiRootObj.transform);
            GameObject modalLayer = CreateLayer(uiRootObj.transform, "ModalLayer");
            GameObject notificationsLayer = CreateLayer(uiRootObj.transform, "NotificationsLayer");
            GameObject debugLayer = CreateLayer(uiRootObj.transform, "DebugLayer");

            // 6. Notification System on NotificationsLayer
            GameObject notifObj = new GameObject("NotificationSystem", typeof(RectTransform), typeof(UINotification));
            notifObj.transform.SetParent(notificationsLayer.transform, false);
            RectTransform notifRect = notifObj.GetComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(1f, 1f);
            notifRect.anchorMax = new Vector2(1f, 1f);
            notifRect.pivot = new Vector2(1f, 1f);
            notifRect.anchoredPosition = new Vector2(-28f, -270f); // Placed just below the Target Tracker
            notifRect.sizeDelta = new Vector2(360f, 500f);

            VerticalLayoutGroup notifLayout = notifObj.AddComponent<VerticalLayoutGroup>();
            notifLayout.childAlignment = TextAnchor.UpperRight;
            notifLayout.spacing = 8f;
            notifLayout.childControlHeight = false;
            notifLayout.childControlWidth = false;
            notifLayout.childForceExpandHeight = false;
            notifLayout.childForceExpandWidth = false;

            UINotification notifComp = notifObj.GetComponent<UINotification>();
            GameObject notifItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSavePath + "UINotificationItem.prefab");
            SetSerializedProperty(notifComp, "notificationPrefab", notifItemPrefab);

            // 7. Instantiate RobotHuntHUD on HUDLayer
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSavePath + "RobotHuntHUD.prefab");
            GameObject hudInstance = null;
            if (hudPrefab != null)
            {
                hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, hudLayer.transform);
                if (hudInstance != null)
                {
                    hudInstance.name = "RobotHuntHUD";
                    StretchFull(hudInstance.GetComponent<RectTransform>());
                }
            }
            RobotHuntHUD hudComp = hudInstance != null ? hudInstance.GetComponent<RobotHuntHUD>() : null;

            // 8. Instantiate Windows on WindowsLayer
            GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSavePath + "SettingsWindow.prefab");
            GameObject settingsInstance = settingsPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, windowsLayer.transform) : null;
            UIWindow settingsWindow = settingsInstance != null ? settingsInstance.GetComponent<UIWindow>() : null;
            if (settingsWindow != null) settingsWindow.Close(true);

            GameObject pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSavePath + "PauseWindow.prefab");
            GameObject pauseInstance = pausePrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(pausePrefab, windowsLayer.transform) : null;
            UIWindow pauseWindow = pauseInstance != null ? pauseInstance.GetComponent<UIWindow>() : null;
            if (pauseWindow != null) pauseWindow.Close(true);

            // 9. UIManager Setup
            GameObject managerObj = new GameObject("UIManager", typeof(UIManager));
            managerObj.transform.SetParent(canvasObj.transform, false);
            UIManager uiManager = managerObj.GetComponent<UIManager>();

            SetSerializedProperty(uiManager, "hudLayer", hudLayer.GetComponent<RectTransform>());
            SetSerializedProperty(uiManager, "windowsLayer", windowsLayer.GetComponent<RectTransform>());
            SetSerializedProperty(uiManager, "modalBackdrop", modalBackdrop.GetComponent<CanvasGroup>());
            SetSerializedProperty(uiManager, "modalLayer", modalLayer.GetComponent<RectTransform>());
            SetSerializedProperty(uiManager, "notificationSystem", notifComp);
            SetSerializedProperty(uiManager, "debugLayer", debugLayer.GetComponent<RectTransform>());
            SetSerializedProperty(uiManager, "settingsWindow", settingsWindow);
            SetSerializedProperty(uiManager, "pauseWindow", pauseWindow);

            // 10. Sleek Bottom Showcase & Debug Dock on DebugLayer
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildSleekBottomDock(debugLayer.transform, font, uiManager, hudComp, settingsWindow, pauseWindow, turntableObj.transform);

            // Save Scene
            EditorSceneManager.SaveScene(scene, DemoScenePath);
            Debug.Log("[UI Setup] UI_System_Demo scene successfully saved at " + DemoScenePath);
        }

        private static void LoadAndPlaceProp(string prefabPath, Transform parent, Vector3 localPos)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.transform.localPosition = localPos;
                instance.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            }
        }

        // =========================================================================
        // COMPREHENSIVE HUD PREFAB BUILDER
        // =========================================================================

        private static void CreateHUDPrefab(Font font)
        {
            GameObject root = new GameObject("RobotHuntHUD", typeof(RectTransform), typeof(RobotHuntHUD));
            StretchFull(root.GetComponent<RectTransform>());

            Sprite glassSpr = LoadKenneySprite("Extra/Default/panel_glass.png");
            Sprite glassNotchesSpr = LoadKenneySprite("Extra/Default/panel_glass_notches.png") ?? glassSpr;
            Sprite glassScrewsSpr = LoadKenneySprite("Extra/Default/panel_glass_screws.png") ?? glassSpr;
            Sprite buttonBgSpr = LoadKenneySprite("Extra/Default/button_rectangle.png");

            // -------------------------------------------------------------
            // 1. TOP-LEFT: Unit & Sector Telemetry Badge
            // -------------------------------------------------------------
            GameObject topLeftBadge = new GameObject("UnitSectorBadge", typeof(RectTransform), typeof(Image));
            topLeftBadge.transform.SetParent(root.transform, false);
            RectTransform tlRect = topLeftBadge.GetComponent<RectTransform>();
            tlRect.anchorMin = new Vector2(0f, 1f);
            tlRect.anchorMax = new Vector2(0f, 1f);
            tlRect.pivot = new Vector2(0f, 1f);
            tlRect.anchoredPosition = new Vector2(28f, -24f);
            tlRect.sizeDelta = new Vector2(280f, 72f);

            Image tlBg = topLeftBadge.GetComponent<Image>();
            tlBg.sprite = glassSpr;
            tlBg.type = Image.Type.Sliced;
            tlBg.color = UIStateColor.GlassDark;

            // Unit Label
            GameObject unitObj = new GameObject("UnitLabel", typeof(RectTransform), typeof(Text));
            unitObj.transform.SetParent(topLeftBadge.transform, false);
            RectTransform uRect = unitObj.GetComponent<RectTransform>();
            uRect.anchorMin = new Vector2(0f, 0.5f);
            uRect.anchorMax = new Vector2(1f, 1f);
            uRect.offsetMin = new Vector2(14f, 0f);
            uRect.offsetMax = new Vector2(-14f, -6f);

            Text uText = unitObj.GetComponent<Text>();
            uText.text = "RC-HUNTER // UNIT-01";
            uText.font = font;
            uText.fontSize = 13;
            uText.fontStyle = FontStyle.Bold;
            uText.alignment = TextAnchor.MiddleLeft;
            uText.color = UIStateColor.TextPrimary;

            // Sector Label
            GameObject secObj = new GameObject("SectorLabel", typeof(RectTransform), typeof(Text));
            secObj.transform.SetParent(topLeftBadge.transform, false);
            RectTransform sRect = secObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0f);
            sRect.anchorMax = new Vector2(0.6f, 0.5f);
            sRect.offsetMin = new Vector2(14f, 6f);
            sRect.offsetMax = Vector2.zero;

            Text sText = secObj.GetComponent<Text>();
            sText.text = "SECTOR: 04 - CITY PLAZA";
            sText.font = font;
            sText.fontSize = 9;
            sText.alignment = TextAnchor.MiddleLeft;
            sText.color = UIStateColor.TextSecondary;

            // Mini Status Indicator
            GameObject indPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSavePath + "UIIndicator.prefab");
            GameObject statusInd = indPrefab != null ? UnityEngine.Object.Instantiate(indPrefab, topLeftBadge.transform) : null;
            UIIndicator unitIndComp = null;
            if (statusInd != null)
            {
                statusInd.name = "UnitStatusIndicator";
                RectTransform siRect = statusInd.GetComponent<RectTransform>();
                siRect.anchorMin = new Vector2(0.55f, 0.5f);
                siRect.anchorMax = new Vector2(0.55f, 0.5f);
                siRect.pivot = new Vector2(0f, 0.5f);
                siRect.anchoredPosition = new Vector2(0f, -14f);
                siRect.sizeDelta = new Vector2(110f, 22f);
                unitIndComp = statusInd.GetComponent<UIIndicator>();
                if (unitIndComp != null) unitIndComp.SetIndicator("NOMINAL", UIStateType.Green, true);
            }

            // -------------------------------------------------------------
            // 2. TOP-CENTER: Navigation Compass & Round Timer Bar
            // -------------------------------------------------------------
            GameObject topCenterBar = new GameObject("TopNavigationCompass", typeof(RectTransform), typeof(Image));
            topCenterBar.transform.SetParent(root.transform, false);
            RectTransform tcRect = topCenterBar.GetComponent<RectTransform>();
            tcRect.anchorMin = new Vector2(0.5f, 1f);
            tcRect.anchorMax = new Vector2(0.5f, 1f);
            tcRect.pivot = new Vector2(0.5f, 1f);
            tcRect.anchoredPosition = new Vector2(0f, -16f);
            tcRect.sizeDelta = new Vector2(440f, 42f);

            Image tcBg = topCenterBar.GetComponent<Image>();
            tcBg.sprite = LoadKenneySprite("Blue/Default/button_square_header_notch_rectangle.png") ?? glassSpr;
            tcBg.type = Image.Type.Sliced;
            tcBg.color = new Color(0.06f, 0.12f, 0.20f, 0.94f);

            // Compass Readings
            GameObject compassObj = new GameObject("CompassText", typeof(RectTransform), typeof(Text));
            compassObj.transform.SetParent(topCenterBar.transform, false);
            RectTransform compRect = compassObj.GetComponent<RectTransform>();
            compRect.anchorMin = new Vector2(0f, 0f);
            compRect.anchorMax = new Vector2(0.72f, 1f);
            compRect.offsetMin = new Vector2(16f, 0f);
            compRect.offsetMax = Vector2.zero;

            Text compText = compassObj.GetComponent<Text>();
            compText.text = "NW • • 315° • • N • • 000° • • NE • • 045°";
            compText.font = font;
            compText.fontSize = 10;
            compText.fontStyle = FontStyle.Bold;
            compText.alignment = TextAnchor.MiddleLeft;
            compText.color = UIStateColor.BlueGlow;

            // Timer Badge
            GameObject timerObj = new GameObject("RoundTimerText", typeof(RectTransform), typeof(Text));
            timerObj.transform.SetParent(topCenterBar.transform, false);
            RectTransform tmRect = timerObj.GetComponent<RectTransform>();
            tmRect.anchorMin = new Vector2(0.72f, 0f);
            tmRect.anchorMax = new Vector2(1f, 1f);
            tmRect.offsetMin = Vector2.zero;
            tmRect.offsetMax = new Vector2(-16f, 0f);

            Text timerText = timerObj.GetComponent<Text>();
            timerText.text = "⏱ 08:45";
            timerText.font = font;
            timerText.fontSize = 13;
            timerText.fontStyle = FontStyle.Bold;
            timerText.alignment = TextAnchor.MiddleRight;
            timerText.color = UIStateColor.Yellow;

            // -------------------------------------------------------------
            // 3. TOP-RIGHT: Object Hunt Protocol Target Tracker
            // -------------------------------------------------------------
            GameObject trackerPanel = new GameObject("ObjectHuntTrackerPanel", typeof(RectTransform), typeof(Image));
            trackerPanel.transform.SetParent(root.transform, false);
            RectTransform trRect = trackerPanel.GetComponent<RectTransform>();
            trRect.anchorMin = new Vector2(1f, 1f);
            trRect.anchorMax = new Vector2(1f, 1f);
            trRect.pivot = new Vector2(1f, 1f);
            trRect.anchoredPosition = new Vector2(-28f, -24f);
            trRect.sizeDelta = new Vector2(340f, 225f);

            Image trBg = trackerPanel.GetComponent<Image>();
            trBg.sprite = glassNotchesSpr;
            trBg.type = Image.Type.Sliced;
            trBg.color = UIStateColor.GlassDark;

            // Header Banner
            GameObject trHeader = new GameObject("HeaderBanner", typeof(RectTransform), typeof(Image));
            trHeader.transform.SetParent(trackerPanel.transform, false);
            RectTransform trhRect = trHeader.GetComponent<RectTransform>();
            trhRect.anchorMin = new Vector2(0f, 1f);
            trhRect.anchorMax = new Vector2(1f, 1f);
            trhRect.pivot = new Vector2(0.5f, 1f);
            trhRect.anchoredPosition = Vector2.zero;
            trhRect.sizeDelta = new Vector2(0f, 38f);

            Image trhImg = trHeader.GetComponent<Image>();
            trhImg.sprite = LoadKenneySprite("Blue/Default/button_square_header_notch_rectangle.png") ?? buttonBgSpr;
            trhImg.type = Image.Type.Sliced;
            trhImg.color = UIStateColor.Blue;

            GameObject trTitle = new GameObject("Title", typeof(RectTransform), typeof(Text));
            trTitle.transform.SetParent(trHeader.transform, false);
            RectTransform trtRect = trTitle.GetComponent<RectTransform>();
            trtRect.anchorMin = new Vector2(0f, 0f);
            trtRect.anchorMax = new Vector2(0.6f, 1f);
            trtRect.offsetMin = new Vector2(12f, 0f);
            trtRect.offsetMax = Vector2.zero;

            Text trtText = trTitle.GetComponent<Text>();
            trtText.text = "OBJECT HUNT";
            trtText.font = font;
            trtText.fontSize = 12;
            trtText.fontStyle = FontStyle.Bold;
            trtText.alignment = TextAnchor.MiddleLeft;
            trtText.color = UIStateColor.TextPrimary;

            GameObject trCounter = new GameObject("CounterText", typeof(RectTransform), typeof(Text));
            trCounter.transform.SetParent(trHeader.transform, false);
            RectTransform trcRect = trCounter.GetComponent<RectTransform>();
            trcRect.anchorMin = new Vector2(0.6f, 0f);
            trcRect.anchorMax = new Vector2(1f, 1f);
            trcRect.offsetMin = Vector2.zero;
            trcRect.offsetMax = new Vector2(-12f, 0f);

            Text counterText = trCounter.GetComponent<Text>();
            counterText.text = "TARGETS  [ 1 / 3 ]";
            counterText.font = font;
            counterText.fontSize = 11;
            counterText.fontStyle = FontStyle.Bold;
            counterText.alignment = TextAnchor.MiddleRight;
            counterText.color = UIStateColor.YellowGlow;

            // Target Cards Container
            GameObject cardsContainer = new GameObject("TargetCards", typeof(RectTransform));
            cardsContainer.transform.SetParent(trackerPanel.transform, false);
            RectTransform ccRect = cardsContainer.GetComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0f, 0f);
            ccRect.anchorMax = new Vector2(1f, 1f);
            ccRect.offsetMin = new Vector2(10f, 26f);
            ccRect.offsetMax = new Vector2(-10f, -44f);

            VerticalLayoutGroup cardLayout = cardsContainer.AddComponent<VerticalLayoutGroup>();
            cardLayout.spacing = 6f;
            cardLayout.childControlHeight = false;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childForceExpandWidth = true;

            List<RobotHuntHUD.TargetCardUI> targetCards = new List<RobotHuntHUD.TargetCardUI>();

            // Card 1 (Found)
            targetCards.Add(BuildRichTargetCard(cardsContainer.transform, "TEDDY BEAR", "✓ RECOVERED", UIStateType.Green, font));
            // Card 2 (In Progress)
            targetCards.Add(BuildRichTargetCard(cardsContainer.transform, "SPORTS CAR", "SCANNING AREA...", UIStateType.Blue, font));
            // Card 3 (Distance detected)
            targetCards.Add(BuildRichTargetCard(cardsContainer.transform, "CERAMIC VASE", "DISTANCE: 14.2M", UIStateType.Yellow, font));

            // Bottom Progress Bar
            GameObject statusBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSavePath + "UIStatusBar.prefab");
            GameObject huntBarObj = statusBarPrefab != null ? UnityEngine.Object.Instantiate(statusBarPrefab, trackerPanel.transform) : null;
            UIStatusBar huntBarComp = null;
            if (huntBarObj != null)
            {
                huntBarObj.name = "HuntProgressBar";
                RectTransform hbRect = huntBarObj.GetComponent<RectTransform>();
                hbRect.anchorMin = new Vector2(0f, 0f);
                hbRect.anchorMax = new Vector2(1f, 0f);
                hbRect.pivot = new Vector2(0.5f, 0f);
                hbRect.anchoredPosition = new Vector2(0f, 6f);
                hbRect.sizeDelta = new Vector2(-20f, 16f);

                huntBarComp = huntBarObj.GetComponent<UIStatusBar>();
                if (huntBarComp != null)
                {
                    huntBarComp.SetLabel("HUNT PROGRESS");
                    huntBarComp.SetProgress(0.33f, false);
                    huntBarComp.SetState(UIStateType.Blue);
                }
            }

            // -------------------------------------------------------------
            // 4. CENTER: Reticle, Distance Meter & Interaction Prompt
            // -------------------------------------------------------------
            GameObject reticlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSavePath + "UIReticle.prefab");
            GameObject reticleInstance = reticlePrefab != null ? UnityEngine.Object.Instantiate(reticlePrefab, root.transform) : null;
            UIReticle reticleComp = null;
            if (reticleInstance != null)
            {
                reticleInstance.name = "UIReticle";
                RectTransform rRect = reticleInstance.GetComponent<RectTransform>();
                rRect.anchoredPosition = Vector2.zero;
                reticleComp = reticleInstance.GetComponent<UIReticle>();
            }

            // Distance & Target Lock Readout
            GameObject distObj = new GameObject("TargetDistanceReadout", typeof(RectTransform), typeof(Text));
            distObj.transform.SetParent(root.transform, false);
            RectTransform distRect = distObj.GetComponent<RectTransform>();
            distRect.anchorMin = new Vector2(0.5f, 0.5f);
            distRect.anchorMax = new Vector2(0.5f, 0.5f);
            distRect.pivot = new Vector2(0.5f, 0.5f);
            distRect.anchoredPosition = new Vector2(0f, -42f);
            distRect.sizeDelta = new Vector2(300f, 24f);

            Text distText = distObj.GetComponent<Text>();
            distText.text = "TARGET: TEDDY BEAR [LOCKED] • DIST: 2.4M";
            distText.font = font;
            distText.fontSize = 11;
            distText.fontStyle = FontStyle.Bold;
            distText.alignment = TextAnchor.MiddleCenter;
            distText.color = UIStateColor.YellowGlow;

            // Floating [E] Prompt Badge
            GameObject promptRoot = new GameObject("InteractionPrompt", typeof(RectTransform));
            promptRoot.transform.SetParent(root.transform, false);
            RectTransform pRect = promptRoot.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.anchoredPosition = new Vector2(0f, -90f);
            pRect.sizeDelta = new Vector2(380f, 44f);

            GameObject pBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            pBg.transform.SetParent(promptRoot.transform, false);
            StretchFull(pBg.GetComponent<RectTransform>());
            Image pBgImg = pBg.GetComponent<Image>();
            pBgImg.sprite = buttonBgSpr;
            pBgImg.type = Image.Type.Sliced;
            pBgImg.color = new Color(0.06f, 0.10f, 0.16f, 0.94f);

            GameObject keyBadge = new GameObject("KeyBadge", typeof(RectTransform), typeof(Image));
            keyBadge.transform.SetParent(promptRoot.transform, false);
            RectTransform kbRect = keyBadge.GetComponent<RectTransform>();
            kbRect.anchorMin = new Vector2(0f, 0.5f);
            kbRect.anchorMax = new Vector2(0f, 0.5f);
            kbRect.pivot = new Vector2(0f, 0.5f);
            kbRect.anchoredPosition = new Vector2(8f, 0f);
            kbRect.sizeDelta = new Vector2(32f, 32f);

            Image kbImg = keyBadge.GetComponent<Image>();
            kbImg.sprite = LoadKenneySprite("Yellow/Default/button_square_header_small_square.png") ?? LoadKenneySprite("Extra/Default/button_square.png");
            kbImg.type = Image.Type.Sliced;
            kbImg.color = UIStateColor.Yellow;

            GameObject keyTextObj = new GameObject("KeyText", typeof(RectTransform), typeof(Text));
            keyTextObj.transform.SetParent(keyBadge.transform, false);
            StretchFull(keyTextObj.GetComponent<RectTransform>());
            Text keyText = keyTextObj.GetComponent<Text>();
            keyText.text = "E";
            keyText.font = font;
            keyText.fontSize = 16;
            keyText.fontStyle = FontStyle.Bold;
            keyText.alignment = TextAnchor.MiddleCenter;
            keyText.color = new Color(0.08f, 0.08f, 0.08f, 1f);

            GameObject actionTextObj = new GameObject("ActionText", typeof(RectTransform), typeof(Text));
            actionTextObj.transform.SetParent(promptRoot.transform, false);
            RectTransform actRect = actionTextObj.GetComponent<RectTransform>();
            actRect.anchorMin = new Vector2(0f, 0f);
            actRect.anchorMax = new Vector2(1f, 1f);
            actRect.offsetMin = new Vector2(48f, 0f);
            actRect.offsetMax = new Vector2(-12f, 0f);

            Text actionText = actionTextObj.GetComponent<Text>();
            actionText.text = "PRESS E TO SCAN & RETRIEVE (TEDDY BEAR)";
            actionText.font = font;
            actionText.fontSize = 12;
            actionText.fontStyle = FontStyle.Bold;
            actionText.alignment = TextAnchor.MiddleLeft;
            actionText.color = UIStateColor.YellowGlow;

            // -------------------------------------------------------------
            // 5. BOTTOM-LEFT: Robot Vital Telemetry
            // -------------------------------------------------------------
            GameObject statusArea = new GameObject("RobotTelemetryPanel", typeof(RectTransform), typeof(Image));
            statusArea.transform.SetParent(root.transform, false);
            RectTransform stRect = statusArea.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 0f);
            stRect.anchorMax = new Vector2(0f, 0f);
            stRect.pivot = new Vector2(0f, 0f);
            stRect.anchoredPosition = new Vector2(28f, 28f);
            stRect.sizeDelta = new Vector2(340f, 185f);

            Image stBg = statusArea.GetComponent<Image>();
            stBg.sprite = glassScrewsSpr;
            stBg.type = Image.Type.Sliced;
            stBg.color = UIStateColor.GlassDark;

            // Panel Title
            GameObject telTitle = new GameObject("TelemetryTitle", typeof(RectTransform), typeof(Text));
            telTitle.transform.SetParent(statusArea.transform, false);
            RectTransform ttRect = telTitle.GetComponent<RectTransform>();
            ttRect.anchorMin = new Vector2(0f, 1f);
            ttRect.anchorMax = new Vector2(1f, 1f);
            ttRect.pivot = new Vector2(0f, 1f);
            ttRect.anchoredPosition = new Vector2(16f, -12f);
            ttRect.sizeDelta = new Vector2(0f, 22f);

            Text ttText = telTitle.GetComponent<Text>();
            ttText.text = "ROBOT TELEMETRY // MODEL 2.0";
            ttText.font = font;
            ttText.fontSize = 11;
            ttText.fontStyle = FontStyle.Bold;
            ttText.color = UIStateColor.BlueGlow;

            // Health Bar
            GameObject healthInstance = statusBarPrefab != null ? UnityEngine.Object.Instantiate(statusBarPrefab, statusArea.transform) : null;
            UIStatusBar healthComp = null;
            if (healthInstance != null)
            {
                healthInstance.name = "HealthBar";
                RectTransform hRect = healthInstance.GetComponent<RectTransform>();
                hRect.anchoredPosition = new Vector2(0f, 115f);
                healthComp = healthInstance.GetComponent<UIStatusBar>();
                if (healthComp != null)
                {
                    healthComp.SetLabel("HULL INTEGRITY");
                    healthComp.SetState(UIStateType.Green);
                    healthComp.SetValue(100f, 100f, false);
                }
            }

            // Energy Bar
            GameObject energyInstance = statusBarPrefab != null ? UnityEngine.Object.Instantiate(statusBarPrefab, statusArea.transform) : null;
            UIStatusBar energyComp = null;
            if (energyInstance != null)
            {
                energyInstance.name = "EnergyBar";
                RectTransform eRect = energyInstance.GetComponent<RectTransform>();
                eRect.anchoredPosition = new Vector2(0f, 78f);
                energyComp = energyInstance.GetComponent<UIStatusBar>();
                if (energyComp != null)
                {
                    energyComp.SetLabel("POWER CORE");
                    energyComp.SetState(UIStateType.Blue);
                    energyComp.SetValue(85f, 100f, false);
                }
            }

            // Shield Bar
            GameObject shieldInstance = statusBarPrefab != null ? UnityEngine.Object.Instantiate(statusBarPrefab, statusArea.transform) : null;
            UIStatusBar shieldComp = null;
            if (shieldInstance != null)
            {
                shieldInstance.name = "ShieldBar";
                RectTransform shRect = shieldInstance.GetComponent<RectTransform>();
                shRect.anchoredPosition = new Vector2(0f, 42f);
                shieldComp = shieldInstance.GetComponent<UIStatusBar>();
                if (shieldComp != null)
                {
                    shieldComp.SetLabel("SHIELD CAP");
                    shieldComp.SetState(UIStateType.Blue);
                    shieldComp.SetValue(100f, 100f, false);
                }
            }

            // LED Chips Row (Bottom)
            GameObject ledRow = new GameObject("LEDRow", typeof(RectTransform));
            ledRow.transform.SetParent(statusArea.transform, false);
            RectTransform lrRect = ledRow.GetComponent<RectTransform>();
            lrRect.anchorMin = new Vector2(0f, 0f);
            lrRect.anchorMax = new Vector2(1f, 0f);
            lrRect.pivot = new Vector2(0.5f, 0f);
            lrRect.anchoredPosition = new Vector2(16f, 8f);
            lrRect.sizeDelta = new Vector2(-32f, 24f);

            HorizontalLayoutGroup ledLayout = ledRow.AddComponent<HorizontalLayoutGroup>();
            ledLayout.spacing = 8f;
            ledLayout.childControlWidth = false;
            ledLayout.childControlHeight = false;

            GameObject scInd = indPrefab != null ? UnityEngine.Object.Instantiate(indPrefab, ledRow.transform) : null;
            UIIndicator scIndComp = scInd != null ? scInd.GetComponent<UIIndicator>() : null;
            if (scIndComp != null)
            {
                scInd.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 22f);
                scIndComp.SetIndicator("SCANNER: ACTIVE", UIStateType.Blue, false);
            }

            GameObject radInd = indPrefab != null ? UnityEngine.Object.Instantiate(indPrefab, ledRow.transform) : null;
            UIIndicator radIndComp = radInd != null ? radInd.GetComponent<UIIndicator>() : null;
            if (radIndComp != null)
            {
                radInd.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 22f);
                radIndComp.SetIndicator("RADAR: 360°", UIStateType.Green, true);
            }

            // -------------------------------------------------------------
            // Assign Component References to RobotHuntHUD
            // -------------------------------------------------------------
            RobotHuntHUD hudComp = root.GetComponent<RobotHuntHUD>();
            SetSerializedProperty(hudComp, "reticle", reticleComp);
            SetSerializedProperty(hudComp, "targetDistanceText", distText);
            SetSerializedProperty(hudComp, "interactionPromptRoot", promptRoot);
            SetSerializedProperty(hudComp, "promptKeyText", keyText);
            SetSerializedProperty(hudComp, "promptActionText", actionText);
            SetSerializedProperty(hudComp, "promptTargetText", null);
            SetSerializedProperty(hudComp, "promptKeyBadge", kbImg);
            SetSerializedProperty(hudComp, "sectorText", sText);
            SetSerializedProperty(hudComp, "compassText", compText);
            SetSerializedProperty(hudComp, "roundTimerText", timerText);
            SetSerializedProperty(hudComp, "unitStatusIndicator", unitIndComp);
            SetSerializedProperty(hudComp, "healthBar", healthComp);
            SetSerializedProperty(hudComp, "energyBar", energyComp);
            SetSerializedProperty(hudComp, "shieldBar", shieldComp);
            SetSerializedProperty(hudComp, "scannerIndicator", scIndComp);
            SetSerializedProperty(hudComp, "radarIndicator", radIndComp);
            SetSerializedProperty(hudComp, "targetTrackerRoot", trackerPanel);
            SetSerializedProperty(hudComp, "targetCounterText", counterText);
            SetSerializedProperty(hudComp, "huntProgressBar", huntBarComp);
            SetSerializedProperty(hudComp, "targetCardsContainer", cardsContainer.transform);

            var field = typeof(RobotHuntHUD).GetField("targetCards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(hudComp, targetCards);

            SavePrefab(root, "RobotHuntHUD.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static RobotHuntHUD.TargetCardUI BuildRichTargetCard(Transform parent, string name, string status, UIStateType state, Font font)
        {
            GameObject cardObj = new GameObject($"Card_{name}", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(parent, false);
            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 36f);

            Image cardBg = cardObj.GetComponent<Image>();
            cardBg.sprite = LoadKenneySprite("Extra/Default/button_rectangle.png");
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.08f, 0.14f, 0.22f, 0.90f);

            // Left State Accent
            GameObject accentObj = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentObj.transform.SetParent(cardObj.transform, false);
            RectTransform aRect = accentObj.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0f, 0f);
            aRect.anchorMax = new Vector2(0f, 1f);
            aRect.pivot = new Vector2(0f, 0.5f);
            aRect.anchoredPosition = new Vector2(2f, 0f);
            aRect.sizeDelta = new Vector2(4f, -4f);

            Image accImg = accentObj.GetComponent<Image>();
            accImg.color = UIStateColor.GetColor(state);

            // Target Name Text (Top Left)
            GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(Text));
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nRect = nameObj.GetComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0f, 0.45f);
            nRect.anchorMax = new Vector2(0.6f, 1f);
            nRect.offsetMin = new Vector2(14f, 0f);
            nRect.offsetMax = Vector2.zero;

            Text nameText = nameObj.GetComponent<Text>();
            nameText.text = name.ToUpper();
            nameText.font = font;
            nameText.fontSize = 11;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = state == UIStateType.Green ? UIStateColor.Green : UIStateColor.TextPrimary;

            // Status Subtitle (Bottom Left)
            GameObject statusObj = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
            statusObj.transform.SetParent(cardObj.transform, false);
            RectTransform stRect = statusObj.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 0f);
            stRect.anchorMax = new Vector2(0.6f, 0.45f);
            stRect.offsetMin = new Vector2(14f, 2f);
            stRect.offsetMax = Vector2.zero;

            Text statusText = statusObj.GetComponent<Text>();
            statusText.text = status;
            statusText.font = font;
            statusText.fontSize = 8;
            statusText.alignment = TextAnchor.MiddleLeft;
            statusText.color = UIStateColor.GetGlowColor(state);

            // Checkmark / Pill Badge (Right)
            GameObject chkObj = new GameObject("CheckmarkText", typeof(RectTransform), typeof(Text));
            chkObj.transform.SetParent(cardObj.transform, false);
            RectTransform chkRect = chkObj.GetComponent<RectTransform>();
            chkRect.anchorMin = new Vector2(0.6f, 0f);
            chkRect.anchorMax = new Vector2(1f, 1f);
            chkRect.offsetMin = Vector2.zero;
            chkRect.offsetMax = new Vector2(-12f, 0f);

            Text checkText = chkObj.GetComponent<Text>();
            checkText.text = state == UIStateType.Green ? "✓" : "•";
            checkText.font = font;
            checkText.fontSize = 14;
            checkText.fontStyle = FontStyle.Bold;
            checkText.alignment = TextAnchor.MiddleRight;
            checkText.color = UIStateColor.GetColor(state);

            return new RobotHuntHUD.TargetCardUI
            {
                root = cardObj,
                nameText = nameText,
                statusText = statusText,
                statusBadge = accImg,
                checkmarkText = checkText
            };
        }

        // =========================================================================
        // SLEEK BOTTOM TEST DOCK BUILDER
        // =========================================================================

        private static void BuildSleekBottomDock(Transform parent, Font font, UIManager uiManager, RobotHuntHUD hud, UIWindow settingsWin, UIWindow pauseWin, Transform turntable)
        {
            GameObject dockRoot = new GameObject("Showcase_Control_Dock", typeof(RectTransform), typeof(Image), typeof(UIDemoController));
            dockRoot.transform.SetParent(parent, false);
            RectTransform dockRect = dockRoot.GetComponent<RectTransform>();
            dockRect.anchorMin = new Vector2(0.5f, 0f);
            dockRect.anchorMax = new Vector2(0.5f, 0f);
            dockRect.pivot = new Vector2(0.5f, 0f);
            dockRect.anchoredPosition = new Vector2(0f, 12f);
            dockRect.sizeDelta = new Vector2(980f, 74f);

            Image dockBg = dockRoot.GetComponent<Image>();
            dockBg.sprite = LoadKenneySprite("Extra/Default/panel_glass.png");
            dockBg.type = Image.Type.Sliced;
            dockBg.color = new Color(0.04f, 0.08f, 0.14f, 0.96f);

            UIDemoController demoCtrl = dockRoot.GetComponent<UIDemoController>();
            SetSerializedProperty(demoCtrl, "uiManager", uiManager);
            SetSerializedProperty(demoCtrl, "hud", hud);
            SetSerializedProperty(demoCtrl, "settingsWindow", settingsWin);
            SetSerializedProperty(demoCtrl, "pauseWindow", pauseWin);
            SetSerializedProperty(demoCtrl, "showcaseTurntable", turntable);

            // Toggle Expand Tab (Top Center)
            GameObject tabBtn = new GameObject("ToggleTab", typeof(RectTransform), typeof(Image), typeof(Button));
            tabBtn.transform.SetParent(dockRoot.transform, false);
            RectTransform tRect = tabBtn.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 1f);
            tRect.anchorMax = new Vector2(0.5f, 1f);
            tRect.pivot = new Vector2(0.5f, 0f);
            tRect.anchoredPosition = new Vector2(0f, -2f);
            tRect.sizeDelta = new Vector2(160f, 22f);

            Image tabImg = tabBtn.GetComponent<Image>();
            tabImg.sprite = LoadKenneySprite("Blue/Default/button_square_header_notch_rectangle.png") ?? LoadKenneySprite("Extra/Default/button_rectangle.png");
            tabImg.type = Image.Type.Sliced;
            tabImg.color = UIStateColor.Blue;

            GameObject tabTxt = new GameObject("Text", typeof(RectTransform), typeof(Text));
            tabTxt.transform.SetParent(tabBtn.transform, false);
            StretchFull(tabTxt.GetComponent<RectTransform>());
            Text tText = tabTxt.GetComponent<Text>();
            tText.text = "▼ HIDE CONTROLS";
            tText.font = font;
            tText.fontSize = 9;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = Color.white;

            Button tBtn = tabBtn.GetComponent<Button>();
            tBtn.onClick.AddListener(() => demoCtrl.ToggleDock());

            SetSerializedProperty(demoCtrl, "dockToggleText", tText);

            // Content Container (Horizontal Rows)
            GameObject contentObj = new GameObject("DockContent", typeof(RectTransform));
            contentObj.transform.SetParent(dockRoot.transform, false);
            StretchFull(contentObj.GetComponent<RectTransform>());
            contentObj.GetComponent<RectTransform>().offsetMin = new Vector2(12f, 8f);
            contentObj.GetComponent<RectTransform>().offsetMax = new Vector2(-12f, -8f);

            SetSerializedProperty(demoCtrl, "dockContentRoot", contentObj);

            HorizontalLayoutGroup hLayout = contentObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 6f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            // Sleek Horizontal Buttons
            CreateDockButton(contentObj.transform, "TOGGLE HUD", UIStateType.Blue, font, () => demoCtrl.ToggleHUD());
            CreateDockButton(contentObj.transform, "SETTINGS", UIStateType.Blue, font, () => demoCtrl.OpenSettingsWindow());
            CreateDockButton(contentObj.transform, "PAUSE", UIStateType.Yellow, font, () => demoCtrl.OpenPauseWindow());
            CreateDockButton(contentObj.transform, "TOAST: INFO", UIStateType.Blue, font, () => demoCtrl.SpawnInfoToast());
            CreateDockButton(contentObj.transform, "TOAST: SUCCESS", UIStateType.Green, font, () => demoCtrl.SpawnSuccessToast());
            CreateDockButton(contentObj.transform, "TOAST: WARN", UIStateType.Yellow, font, () => demoCtrl.SpawnWarningToast());
            CreateDockButton(contentObj.transform, "TOAST: ERROR", UIStateType.Red, font, () => demoCtrl.SpawnErrorToast());
            CreateDockButton(contentObj.transform, "HEALTH -/+", UIStateType.Green, font, () => demoCtrl.ModifyHealth(-20f));
            CreateDockButton(contentObj.transform, "TARGET +", UIStateType.Green, font, () => demoCtrl.SimulateCollectNextTarget());
            CreateDockButton(contentObj.transform, "RESET TARGETS", UIStateType.Grey, font, () => demoCtrl.ResetTargetsSimulation());
            CreateDockButton(contentObj.transform, "RETICLE", UIStateType.Grey, font, () => demoCtrl.CycleReticleState());
            CreateDockButton(contentObj.transform, "PROMPT [E]", UIStateType.Yellow, font, () => demoCtrl.ToggleInteractionPrompt());
        }

        private static void CreateDockButton(Transform parent, string label, UIStateType state, Font font, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(UIButton));
            btnObj.transform.SetParent(parent, false);

            Sprite normalSpr = LoadKenneySprite("Extra/Default/button_rectangle.png");
            Sprite pressedSpr = LoadKenneySprite("Extra/Default/button_rectangle_depth.png");

            Image img = btnObj.GetComponent<Image>();
            img.sprite = normalSpr;
            img.type = Image.Type.Sliced;
            img.color = UIStateColor.GetColor(state);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            StretchFull(txtObj.GetComponent<RectTransform>());

            Text txt = txtObj.GetComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = 8;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = UIStateColor.TextPrimary;

            UIButton btnComp = btnObj.GetComponent<UIButton>();
            SetSerializedProperty(btnComp, "labelText", txt);
            SetSerializedProperty(btnComp, "buttonImage", img);
            SetSerializedProperty(btnComp, "buttonState", state);
            SetSerializedProperty(btnComp, "normalSprite", normalSpr);
            SetSerializedProperty(btnComp, "pressedSprite", pressedSpr);

            if (onClick != null) btnComp.AddListener(onClick);
        }

        // =========================================================================
        // CORE COMPONENT PREFAB BUILDERS
        // =========================================================================

        private static void CreateButtonPrefab(Font font)
        {
            GameObject root = new GameObject("UIButton", typeof(RectTransform), typeof(Image), typeof(UIButton));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 48f);

            Sprite normalSpr = LoadKenneySprite("Extra/Default/button_rectangle.png");
            Sprite depthSpr = LoadKenneySprite("Extra/Default/button_rectangle_depth.png");

            Image img = root.GetComponent<Image>();
            img.sprite = normalSpr;
            img.type = Image.Type.Sliced;
            img.color = UIStateColor.Blue;

            GameObject barObj = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(root.transform, false);
            RectTransform barRect = barObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 0.5f);
            barRect.anchoredPosition = new Vector2(4f, 0f);
            barRect.sizeDelta = new Vector2(4f, -8f);

            Image barImg = barObj.GetComponent<Image>();
            barImg.color = UIStateColor.BlueGlow;

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(root.transform, false);
            StretchFull(labelObj.GetComponent<RectTransform>());

            Text labelText = labelObj.GetComponent<Text>();
            labelText.text = "BUTTON ACTION";
            labelText.font = font;
            labelText.fontSize = 15;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = UIStateColor.TextPrimary;

            UIButton btnComp = root.GetComponent<UIButton>();
            SetSerializedProperty(btnComp, "labelText", labelText);
            SetSerializedProperty(btnComp, "buttonImage", img);
            SetSerializedProperty(btnComp, "accentBar", barImg);
            SetSerializedProperty(btnComp, "normalSprite", normalSpr);
            SetSerializedProperty(btnComp, "pressedSprite", depthSpr);
            SetSerializedProperty(btnComp, "hoverSprite", normalSpr);

            SavePrefab(root, "UIButton.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateHeaderPrefab(Font font)
        {
            GameObject root = new GameObject("UIHeader", typeof(RectTransform), typeof(UIHeader));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 56f);

            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(root.transform, false);
            StretchFull(bgObj.GetComponent<RectTransform>());

            Sprite headerSpr = LoadKenneySprite("Blue/Default/button_square_header_notch_rectangle.png") ?? LoadKenneySprite("Extra/Default/panel_rectangle.png");
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = headerSpr;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = UIStateColor.Blue;

            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(root.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.35f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(16f, 0f);
            titleRect.offsetMax = new Vector2(-16f, -4f);

            Text titleText = titleObj.GetComponent<Text>();
            titleText.text = "HEADER TITLE";
            titleText.font = font;
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = UIStateColor.TextPrimary;

            GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
            subObj.transform.SetParent(root.transform, false);
            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(1f, 0.45f);
            subRect.offsetMin = new Vector2(16f, 4f);
            subRect.offsetMax = new Vector2(-16f, 0f);

            Text subText = subObj.GetComponent<Text>();
            subText.text = "Subsystem diagnostics & configuration";
            subText.font = font;
            subText.fontSize = 10;
            subText.alignment = TextAnchor.MiddleLeft;
            subText.color = UIStateColor.TextSecondary;

            UIHeader headerComp = root.GetComponent<UIHeader>();
            SetSerializedProperty(headerComp, "titleText", titleText);
            SetSerializedProperty(headerComp, "subtitleText", subText);
            SetSerializedProperty(headerComp, "headerBackground", bgImg);

            SavePrefab(root, "UIHeader.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateStatusBarPrefab(Font font)
        {
            GameObject root = new GameObject("UIStatusBar", typeof(RectTransform), typeof(UIStatusBar));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 32f);

            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(root.transform, false);
            StretchFull(bgObj.GetComponent<RectTransform>());

            Sprite shadowSpr = LoadKenneySprite("Extra/Default/bar_shadow_round_large.png");
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = shadowSpr;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(bgObj.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);

            Sprite barSpr = LoadKenneySprite("Blue/Default/bar_round_large.png") ?? LoadKenneySprite("Extra/Default/bar_shadow_round_large.png");
            Image fillImg = fillObj.GetComponent<Image>();
            fillImg.sprite = barSpr;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.75f;
            fillImg.color = UIStateColor.Blue;

            GameObject labelObj = new GameObject("LabelText", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.6f, 1f);
            labelRect.offsetMin = new Vector2(12f, 0f);
            labelRect.offsetMax = Vector2.zero;

            Text labelText = labelObj.GetComponent<Text>();
            labelText.text = "ENERGY";
            labelText.font = font;
            labelText.fontSize = 11;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = UIStateColor.TextPrimary;

            GameObject valObj = new GameObject("ValueText", typeof(RectTransform), typeof(Text));
            valObj.transform.SetParent(root.transform, false);
            RectTransform valRect = valObj.GetComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.6f, 0f);
            valRect.anchorMax = new Vector2(1f, 1f);
            valRect.offsetMin = Vector2.zero;
            valRect.offsetMax = new Vector2(-12f, 0f);

            Text valText = valObj.GetComponent<Text>();
            valText.text = "75%";
            valText.font = font;
            valText.fontSize = 11;
            valText.fontStyle = FontStyle.Bold;
            valText.alignment = TextAnchor.MiddleRight;
            valText.color = UIStateColor.TextPrimary;

            UIStatusBar statusComp = root.GetComponent<UIStatusBar>();
            SetSerializedProperty(statusComp, "backgroundImage", bgImg);
            SetSerializedProperty(statusComp, "fillImage", fillImg);
            SetSerializedProperty(statusComp, "labelText", labelText);
            SetSerializedProperty(statusComp, "valueText", valText);
            SetSerializedProperty(statusComp, "barLabel", "ENERGY");

            SavePrefab(root, "UIStatusBar.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateIndicatorPrefab(Font font)
        {
            GameObject root = new GameObject("UIIndicator", typeof(RectTransform), typeof(UIIndicator));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 26f);

            GameObject bgObj = new GameObject("PillBackground", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(root.transform, false);
            StretchFull(bgObj.GetComponent<RectTransform>());

            Sprite pillSpr = LoadKenneySprite("Extra/Default/button_rectangle.png");
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = pillSpr;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.2f, 0.8f, 0.4f, 0.15f);

            GameObject ledObj = new GameObject("LED_Dot", typeof(RectTransform), typeof(Image));
            ledObj.transform.SetParent(root.transform, false);
            RectTransform ledRect = ledObj.GetComponent<RectTransform>();
            ledRect.anchorMin = new Vector2(0f, 0.5f);
            ledRect.anchorMax = new Vector2(0f, 0.5f);
            ledRect.pivot = new Vector2(0f, 0.5f);
            ledRect.anchoredPosition = new Vector2(8f, 0f);
            ledRect.sizeDelta = new Vector2(8f, 8f);

            Sprite dotSpr = LoadKenneySprite("Extra/Default/button_square.png");
            Image ledImg = ledObj.GetComponent<Image>();
            ledImg.sprite = dotSpr;
            ledImg.color = UIStateColor.Green;

            GameObject labelObj = new GameObject("StatusLabel", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(22f, 0f);
            labelRect.offsetMax = new Vector2(-6f, 0f);

            Text labelText = labelObj.GetComponent<Text>();
            labelText.text = "ONLINE";
            labelText.font = font;
            labelText.fontSize = 10;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = UIStateColor.Green;

            UIIndicator indComp = root.GetComponent<UIIndicator>();
            SetSerializedProperty(indComp, "ledDot", ledImg);
            SetSerializedProperty(indComp, "backgroundPill", bgImg);
            SetSerializedProperty(indComp, "statusLabel", labelText);

            SavePrefab(root, "UIIndicator.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateReticlePrefab()
        {
            GameObject root = new GameObject("UIReticle", typeof(RectTransform), typeof(Image), typeof(UIReticle));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40f, 40f);

            Sprite reticleSpr = LoadKenneySprite("Blue/Default/crosshair_color_a.png") ?? LoadKenneySprite("Extra/Default/crosshair_a.png");
            Sprite interactSpr = LoadKenneySprite("Yellow/Default/crosshair_color_a.png") ?? LoadKenneySprite("Extra/Default/crosshair_b.png");
            Sprite lockSpr = LoadKenneySprite("Red/Default/crosshair_color_a.png") ?? LoadKenneySprite("Extra/Default/crosshair_c.png");

            Image img = root.GetComponent<Image>();
            img.sprite = reticleSpr;
            img.color = UIStateColor.Blue;

            GameObject dotObj = new GameObject("CenterDot", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(root.transform, false);
            RectTransform dotRect = dotObj.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(4f, 4f);
            dotRect.anchoredPosition = Vector2.zero;

            Image dotImg = dotObj.GetComponent<Image>();
            dotImg.color = UIStateColor.BlueGlow;

            UIReticle retComp = root.GetComponent<UIReticle>();
            SetSerializedProperty(retComp, "centerDot", dotImg);
            SetSerializedProperty(retComp, "defaultCrosshair", reticleSpr);
            SetSerializedProperty(retComp, "interactCrosshair", interactSpr);
            SetSerializedProperty(retComp, "lockCrosshair", lockSpr);

            SavePrefab(root, "UIReticle.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateNotificationItemPrefab(Font font)
        {
            GameObject root = new GameObject("UINotificationItem", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(UINotificationItem));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(340f, 76f);

            Sprite glassSpr = LoadKenneySprite("Extra/Default/panel_glass.png");
            Image bgImg = root.GetComponent<Image>();
            bgImg.sprite = glassSpr;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = UIStateColor.GlassDark;

            GameObject barObj = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(root.transform, false);
            RectTransform barRect = barObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 0.5f);
            barRect.anchoredPosition = new Vector2(2f, 0f);
            barRect.sizeDelta = new Vector2(4f, -4f);

            Image barImg = barObj.GetComponent<Image>();
            barImg.color = UIStateColor.Blue;

            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(root.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.55f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(16f, 0f);
            titleRect.offsetMax = new Vector2(-12f, -6f);

            Text titleText = titleObj.GetComponent<Text>();
            titleText.text = "NOTIFICATION TITLE";
            titleText.font = font;
            titleText.fontSize = 13;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = UIStateColor.TextPrimary;

            GameObject msgObj = new GameObject("MessageText", typeof(RectTransform), typeof(Text));
            msgObj.transform.SetParent(root.transform, false);
            RectTransform msgRect = msgObj.GetComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0f, 0f);
            msgRect.anchorMax = new Vector2(1f, 0.55f);
            msgRect.offsetMin = new Vector2(16f, 8f);
            msgRect.offsetMax = new Vector2(-12f, 0f);

            Text msgText = msgObj.GetComponent<Text>();
            msgText.text = "Detailed diagnostic or objective status description message.";
            msgText.font = font;
            msgText.fontSize = 10;
            msgText.alignment = TextAnchor.UpperLeft;
            msgText.color = UIStateColor.TextSecondary;

            GameObject timerObj = new GameObject("TimerBar", typeof(RectTransform), typeof(Image));
            timerObj.transform.SetParent(root.transform, false);
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0f, 0f);
            timerRect.anchorMax = new Vector2(1f, 0f);
            timerRect.pivot = new Vector2(0f, 0f);
            timerRect.anchoredPosition = new Vector2(4f, 2f);
            timerRect.sizeDelta = new Vector2(-8f, 2f);

            Image timerImg = timerObj.GetComponent<Image>();
            timerImg.type = Image.Type.Filled;
            timerImg.fillMethod = Image.FillMethod.Horizontal;
            timerImg.fillAmount = 1f;
            timerImg.color = UIStateColor.Blue;

            UINotificationItem itemComp = root.GetComponent<UINotificationItem>();
            SetSerializedProperty(itemComp, "titleText", titleText);
            SetSerializedProperty(itemComp, "messageText", msgText);
            SetSerializedProperty(itemComp, "accentBar", barImg);
            SetSerializedProperty(itemComp, "timerBar", timerImg);
            SetSerializedProperty(itemComp, "backgroundImage", bgImg);

            SavePrefab(root, "UINotificationItem.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateWindowPrefabs(Font font)
        {
            Sprite glassSpr = LoadKenneySprite("Extra/Default/panel_glass.png");
            Sprite headerSpr = LoadKenneySprite("Blue/Default/button_square_header_notch_rectangle.png") ?? LoadKenneySprite("Extra/Default/panel_rectangle.png");
            Sprite closeBtnSpr = LoadKenneySprite("Red/Default/button_square_header_small_square.png") ?? LoadKenneySprite("Extra/Default/button_square.png");

            GameObject baseWindow = BuildWindowGameObject("UIWindow", new Vector2(500f, 400f), "WINDOW TITLE", "System window panel", glassSpr, headerSpr, closeBtnSpr, font, UIStateType.Blue);
            SavePrefab(baseWindow, "UIWindow.prefab");
            UnityEngine.Object.DestroyImmediate(baseWindow);

            GameObject settingsWindow = BuildSettingsWindowGameObject("SettingsWindow", new Vector2(560f, 480f), glassSpr, headerSpr, closeBtnSpr, font);
            SavePrefab(settingsWindow, "SettingsWindow.prefab");
            UnityEngine.Object.DestroyImmediate(settingsWindow);

            GameObject pauseWindow = BuildPauseWindowGameObject("PauseWindow", new Vector2(440f, 420f), glassSpr, headerSpr, closeBtnSpr, font);
            SavePrefab(pauseWindow, "PauseWindow.prefab");
            UnityEngine.Object.DestroyImmediate(pauseWindow);
        }

        private static GameObject BuildWindowGameObject(string name, Vector2 size, string title, string subtitle, Sprite glassSpr, Sprite headerSpr, Sprite closeBtnSpr, Font font, UIStateType state)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(UIWindow));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = size;

            Image bgImg = root.GetComponent<Image>();
            bgImg.sprite = glassSpr;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = UIStateColor.GlassDark;

            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(UIHeader));
            headerObj.transform.SetParent(root.transform, false);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 54f);

            GameObject headerBg = new GameObject("HeaderBackground", typeof(RectTransform), typeof(Image));
            headerBg.transform.SetParent(headerObj.transform, false);
            StretchFull(headerBg.GetComponent<RectTransform>());

            Image headerBgImg = headerBg.GetComponent<Image>();
            headerBgImg.sprite = headerSpr;
            headerBgImg.type = Image.Type.Sliced;
            headerBgImg.color = UIStateColor.GetColor(state);

            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.35f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(16f, 0f);
            titleRect.offsetMax = new Vector2(-54f, -4f);

            Text titleText = titleObj.GetComponent<Text>();
            titleText.text = title.ToUpper();
            titleText.font = font;
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = UIStateColor.TextPrimary;

            GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
            subObj.transform.SetParent(headerObj.transform, false);
            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(1f, 0.45f);
            subRect.offsetMin = new Vector2(16f, 4f);
            subRect.offsetMax = new Vector2(-54f, 0f);

            Text subText = subObj.GetComponent<Text>();
            subText.text = subtitle;
            subText.font = font;
            subText.fontSize = 10;
            subText.color = UIStateColor.TextSecondary;

            UIHeader headerComp = headerObj.GetComponent<UIHeader>();
            SetSerializedProperty(headerComp, "titleText", titleText);
            SetSerializedProperty(headerComp, "subtitleText", subText);
            SetSerializedProperty(headerComp, "headerBackground", headerBgImg);

            GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(UIButton));
            closeObj.transform.SetParent(root.transform, false);
            RectTransform closeRect = closeObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-8f, -8f);
            closeRect.sizeDelta = new Vector2(36f, 36f);

            Image closeImg = closeObj.GetComponent<Image>();
            closeImg.sprite = closeBtnSpr;
            closeImg.type = Image.Type.Sliced;
            closeImg.color = UIStateColor.Red;

            GameObject closeTxt = new GameObject("Text", typeof(RectTransform), typeof(Text));
            closeTxt.transform.SetParent(closeObj.transform, false);
            StretchFull(closeTxt.GetComponent<RectTransform>());
            Text cText = closeTxt.GetComponent<Text>();
            cText.text = "X";
            cText.font = font;
            cText.fontSize = 16;
            cText.fontStyle = FontStyle.Bold;
            cText.alignment = TextAnchor.MiddleCenter;
            cText.color = Color.white;

            UIButton closeBtnComp = closeObj.GetComponent<UIButton>();
            SetSerializedProperty(closeBtnComp, "labelText", cText);
            SetSerializedProperty(closeBtnComp, "buttonImage", closeImg);
            SetSerializedProperty(closeBtnComp, "buttonState", UIStateType.Red);

            GameObject contentObj = new GameObject("ContentRoot", typeof(RectTransform));
            contentObj.transform.SetParent(root.transform, false);
            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(20f, 20f);
            contentRect.offsetMax = new Vector2(-20f, -65f);

            UIWindow windowComp = root.GetComponent<UIWindow>();
            SetSerializedProperty(windowComp, "header", headerComp);
            SetSerializedProperty(windowComp, "contentRoot", contentRect);
            SetSerializedProperty(windowComp, "closeButton", closeBtnComp);
            SetSerializedProperty(windowComp, "backgroundImage", bgImg);
            SetSerializedProperty(windowComp, "isModal", true);

            return root;
        }

        private static GameObject BuildSettingsWindowGameObject(string name, Vector2 size, Sprite glassSpr, Sprite headerSpr, Sprite closeBtnSpr, Font font)
        {
            GameObject window = BuildWindowGameObject(name, size, "SYSTEM SETTINGS", "Audio, graphics & controls configuration", glassSpr, headerSpr, closeBtnSpr, font, UIStateType.Blue);
            UIWindow winComp = window.GetComponent<UIWindow>();
            RectTransform content = winComp.ContentRoot;

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            CreateInteractiveSlider(content, "MASTER VOLUME", 0.85f, font);
            CreateInteractiveSlider(content, "SFX VOLUME", 0.90f, font);
            CreateInteractiveSlider(content, "MUSIC VOLUME", 0.70f, font);
            CreateInteractiveSlider(content, "UI BRIGHTNESS", 1.00f, font);

            // Action Buttons Row (Apply & Reset)
            GameObject btnRow = new GameObject("ActionButtonsRow", typeof(RectTransform));
            btnRow.transform.SetParent(content, false);
            RectTransform brRect = btnRow.GetComponent<RectTransform>();
            brRect.sizeDelta = new Vector2(0f, 44f);

            HorizontalLayoutGroup hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateSimpleButton(btnRow.transform, "APPLY SETTINGS", UIStateType.Green, font, () =>
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowNotification("SETTINGS SAVED", "Audio and visual preferences applied.", NotificationType.Success, 2.5f);
                }
                winComp.Close();
            });

            CreateSimpleButton(btnRow.transform, "CLOSE", UIStateType.Grey, font, () => winComp.Close());

            return window;
        }

        private static void CreateInteractiveSlider(Transform parent, string label, float defaultValue, Font font)
        {
            GameObject row = new GameObject($"Setting_{label}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 38f);

            // Label (Left)
            GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            lblObj.transform.SetParent(row.transform, false);
            RectTransform lRect = lblObj.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0f, 0f);
            lRect.anchorMax = new Vector2(0.40f, 1f);
            lRect.offsetMin = new Vector2(6f, 0f);
            lRect.offsetMax = Vector2.zero;

            Text lText = lblObj.GetComponent<Text>();
            lText.text = label;
            lText.font = font;
            lText.fontSize = 11;
            lText.fontStyle = FontStyle.Bold;
            lText.alignment = TextAnchor.MiddleLeft;
            lText.color = UIStateColor.TextPrimary;

            // Value Text (Right)
            GameObject valObj = new GameObject("ValueText", typeof(RectTransform), typeof(Text));
            valObj.transform.SetParent(row.transform, false);
            RectTransform vRect = valObj.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0.88f, 0f);
            vRect.anchorMax = new Vector2(1f, 1f);
            vRect.offsetMin = Vector2.zero;
            vRect.offsetMax = new Vector2(-6f, 0f);

            Text vText = valObj.GetComponent<Text>();
            vText.text = $"{Mathf.RoundToInt(defaultValue * 100f)}%";
            vText.font = font;
            vText.fontSize = 11;
            vText.fontStyle = FontStyle.Bold;
            vText.alignment = TextAnchor.MiddleRight;
            vText.color = UIStateColor.YellowGlow;

            // Slider Component (Middle)
            GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(row.transform, false);
            RectTransform sRect = sliderObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.42f, 0.5f);
            sRect.anchorMax = new Vector2(0.86f, 0.5f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = Vector2.zero;
            sRect.sizeDelta = new Vector2(0f, 18f);

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultValue;

            // Slider Background
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(sliderObj.transform, false);
            StretchFull(bgObj.GetComponent<RectTransform>());

            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = LoadKenneySprite("Extra/Default/bar_shadow_round_large.png");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0f, 0f);
            faRect.anchorMax = new Vector2(1f, 1f);
            faRect.offsetMin = new Vector2(3f, 3f);
            faRect.offsetMax = new Vector2(-3f, -3f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRect = fill.GetComponent<RectTransform>();
            fRect.sizeDelta = Vector2.zero;

            Image fImg = fill.GetComponent<Image>();
            fImg.sprite = LoadKenneySprite("Blue/Default/bar_round_large.png");
            fImg.type = Image.Type.Sliced;
            fImg.color = UIStateColor.Blue;

            // Handle Area
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            StretchFull(handleArea.GetComponent<RectTransform>());

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform hRect = handle.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(18f, 18f);

            Image hImg = handle.GetComponent<Image>();
            hImg.sprite = LoadKenneySprite("Yellow/Default/button_square_header_small_square.png") ?? LoadKenneySprite("Extra/Default/button_square.png");
            hImg.type = Image.Type.Sliced;
            hImg.color = UIStateColor.Yellow;

            slider.targetGraphic = hImg;
            slider.fillRect = fRect;
            slider.handleRect = hRect;
            slider.direction = Slider.Direction.LeftToRight;

            slider.onValueChanged.AddListener((val) =>
            {
                if (vText != null) vText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            });
        }

        private static GameObject BuildPauseWindowGameObject(string name, Vector2 size, Sprite glassSpr, Sprite headerSpr, Sprite closeBtnSpr, Font font)
        {
            GameObject window = BuildWindowGameObject(name, size, "GAME PAUSED", "Tactical operations suspended", glassSpr, headerSpr, closeBtnSpr, font, UIStateType.Yellow);
            UIWindow winComp = window.GetComponent<UIWindow>();
            RectTransform content = winComp.ContentRoot;

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            CreateSimpleButton(content, "RESUME OPERATION", UIStateType.Green, font, () => winComp.Close());
            CreateSimpleButton(content, "MISSION OBJECTIVES", UIStateType.Blue, font, () =>
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowNotification("OBJECTIVES", "Locate 3 lost toys across the sector.", NotificationType.Info, 3f);
                }
            });
            CreateSimpleButton(content, "SYSTEM SETTINGS", UIStateType.Blue, font, () =>
            {
                if (UIManager.Instance != null)
                {
                    winComp.Close();
                    // UIManager will open settings window if triggered
                }
            });
            CreateSimpleButton(content, "QUIT TO MAIN MENU", UIStateType.Red, font, () =>
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowNotification("MISSION ABORTED", "Returning to command hub...", NotificationType.Warning, 3f);
                }
                winComp.Close();
            });

            return window;
        }

        private static void CreateSimpleButton(Transform parent, string text, UIStateType state, Font font, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Btn_{text}", typeof(RectTransform), typeof(Image), typeof(UIButton));
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(290f, 44f);

            Sprite normalSpr = LoadKenneySprite("Extra/Default/button_rectangle.png");
            Sprite pressedSpr = LoadKenneySprite("Extra/Default/button_rectangle_depth.png");

            Image img = btnObj.GetComponent<Image>();
            img.sprite = normalSpr;
            img.type = Image.Type.Sliced;
            img.color = UIStateColor.GetColor(state);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            StretchFull(txtObj.GetComponent<RectTransform>());

            Text txt = txtObj.GetComponent<Text>();
            txt.text = text;
            txt.font = font;
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = UIStateColor.TextPrimary;

            UIButton btnComp = btnObj.GetComponent<UIButton>();
            SetSerializedProperty(btnComp, "labelText", txt);
            SetSerializedProperty(btnComp, "buttonImage", img);
            SetSerializedProperty(btnComp, "buttonState", state);
            SetSerializedProperty(btnComp, "normalSprite", normalSpr);
            SetSerializedProperty(btnComp, "pressedSprite", pressedSpr);

            if (onClick != null) btnComp.AddListener(onClick);
        }



        // =========================================================================
        // UTILITIES
        // =========================================================================

        private static GameObject CreateLayer(Transform parent, string layerName)
        {
            GameObject go = new GameObject(layerName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            StretchFull(go.GetComponent<RectTransform>());
            return go;
        }

        private static GameObject CreateModalBackdrop(Transform parent)
        {
            GameObject go = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            StretchFull(go.GetComponent<RectTransform>());

            Image img = go.GetComponent<Image>();
            img.color = UIStateColor.GlassModalOverlay;

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            go.SetActive(false);
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite LoadKenneySprite(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(KenneyBasePath + "PNG/" + relativePath);
        }

        private static void SavePrefab(GameObject root, string fileName)
        {
            string fullPath = PrefabSavePath + fileName;
            PrefabUtility.SaveAsPrefabAsset(root, fullPath);
        }

        private static void SetSerializedProperty(UnityEngine.Object target, string propName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty sp = so.FindProperty(propName);
            if (sp != null)
            {
                if (value is UnityEngine.Object uo) sp.objectReferenceValue = uo;
                else if (value is string s) sp.stringValue = s;
                else if (value is bool b) sp.boolValue = b;
                else if (value is int i) sp.intValue = i;
                else if (value is float f) sp.floatValue = f;
                else if (value is Enum e) sp.enumValueIndex = Convert.ToInt32(e);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
