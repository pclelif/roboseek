using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Robot.UI.HUD;

namespace Robot.ObjectHunt
{
    [RequireComponent(typeof(ObjectHuntRoundManager), typeof(RoundGameLoop))]
    public sealed class ObjectHuntHUD : MonoBehaviour
    {
        private ObjectHuntRoundManager manager;
        private RoundGameLoop gameLoop;
        private Canvas canvas;
        private GameObject introPanel;
        private GameObject trackerPanel;
        private GameObject resultPanel;
        private Text introTitle;
        private Text countdown;
        private Text timer;
        private Text prompt;
        private GameObject promptPanel;
        private Text result;
        private Button nextRoundButton;
        private readonly List<Text> cardLabels = new List<Text>();
        private readonly List<Text> cardChecks = new List<Text>();
        private readonly List<RawImage> previews = new List<RawImage>();
        private readonly List<GameObject> cards = new List<GameObject>();
        private readonly List<GameObject> previewObjects = new List<GameObject>();

        private void Awake()
        {
            manager = GetComponent<ObjectHuntRoundManager>();
            gameLoop = GetComponent<RoundGameLoop>();
            BuildUI();
        }

        private void OnEnable()
        {
            manager.RoundStarted += HandleRoundStarted;
            manager.TargetCollected += HandleCollected;
            manager.TimerChanged += HandleTimer;
            if (gameLoop != null)
            {
                gameLoop.PhaseChanged += HandlePhaseChanged;
                gameLoop.CountdownChanged += HandleCountdown;
                gameLoop.ResultReady += HandleResult;
            }
        }

        private void OnDisable()
        {
            manager.RoundStarted -= HandleRoundStarted;
            manager.TargetCollected -= HandleCollected;
            manager.TimerChanged -= HandleTimer;
            if (gameLoop != null)
            {
                gameLoop.PhaseChanged -= HandlePhaseChanged;
                gameLoop.CountdownChanged -= HandleCountdown;
                gameLoop.ResultReady -= HandleResult;
            }
        }

        private void Update()
        {
            CollectibleTarget nearest = manager != null ? manager.GetNearestInteractable() : null;
            bool show = nearest != null && !nearest.IsCollecting;

            if (promptPanel != null) promptPanel.SetActive(show);
            else if (prompt != null) prompt.gameObject.SetActive(show);

            if (prompt != null && show)
            {
                string targetName = nearest.Definition != null ? nearest.Definition.displayName.ToUpper() : "TARGET";
                prompt.text = $"<b>[E]</b>  PRESS E TO PICK UP ({targetName})";
            }

            if (gameLoop != null && gameLoop.CurrentPhase == RoundPhase.Result && (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space)))
                gameLoop.StartNextRound();
        }

        private void HandleRoundStarted(IReadOnlyList<TargetDefinition> targets)
        {
            for (int i = 0; i < cardLabels.Count; i++)
            {
                bool active = i < targets.Count;
                cardLabels[i].transform.parent.gameObject.SetActive(active);
                if (!active) continue;
                cardLabels[i].text = targets[i].displayName;
                cardLabels[i].gameObject.SetActive(false);
                cardChecks[i].text = string.Empty;
                cardLabels[i].color = Color.white;
                BuildPreview(previews[i], targets[i], i);
            }
            ArrangeIntroCards();
        }

        private void ArrangeIntroCards()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].transform.SetParent(introPanel.transform, false);
                RectTransform cardRect = cards[i].GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(190f, 150f);
                cardRect.anchoredPosition = new Vector2((i - 1) * 210f, 10f);
                previews[i].rectTransform.sizeDelta = new Vector2(105f, 90f);
                previews[i].rectTransform.anchoredPosition = new Vector2(0f, 15f);
                cardChecks[i].rectTransform.anchoredPosition = new Vector2(65f, 40f);
                cardChecks[i].fontSize = 34;
            }
            introPanel.SetActive(true);
            trackerPanel.SetActive(false);
            introTitle.text = "FIND THESE";
            countdown.text = string.Empty;
        }

        private void HandlePhaseChanged(RoundPhase phase)
        {
            if (phase == RoundPhase.Lobby)
            {
                introPanel.SetActive(true); trackerPanel.SetActive(false); resultPanel.SetActive(false);
                introTitle.text = "ROBOT HUNT"; countdown.text = "START";
            }
            else if (phase == RoundPhase.Intro)
            {
                resultPanel.SetActive(false); trackerPanel.SetActive(false); introPanel.SetActive(true);
                introTitle.text = $"ROUND {gameLoop.RoundNumber}"; countdown.text = string.Empty;
            }
            else if (phase == RoundPhase.Targets) ArrangeIntroCards();
            else if (phase == RoundPhase.Countdown) countdown.text = string.Empty;
            else if (phase == RoundPhase.Search) StartCoroutine(CollapseToTrackerRoutine());
            else if (phase == RoundPhase.RoundComplete)
            {
                trackerPanel.SetActive(false); resultPanel.SetActive(true);
                result.text = "ALL OBJECTS FOUND!\n3 / 3";
                nextRoundButton.gameObject.SetActive(false);
            }
        }

        private void HandleCountdown(int value) => countdown.text = value > 0 ? value.ToString() : "GO!";

        private void HandleResult(RoundResultData data)
        {
            introPanel.SetActive(false); trackerPanel.SetActive(false); resultPanel.SetActive(true);
            string status = data.completed ? "ROUND COMPLETE" : "ROUND FAILED";
            string foundObjects = manager.CollectedTargets.Count > 0
                ? string.Join("  •  ", manager.CollectedTargets.Select(item => item.displayName))
                : "NONE";

            if (Robot.Multiplayer.NetworkRoundManager.Instance != null && Robot.Multiplayer.NetworkRoundManager.Instance.IsSpawned)
            {
                var leaderboard = Robot.Multiplayer.NetworkRoundManager.Instance.GetLeaderboard();
                string leaderboardLines = "\n\n<b>LEADERBOARD:</b>\n";
                for (int i = 0; i < leaderboard.Count; i++)
                {
                    var p = leaderboard[i];
                    string rank = i == 0 ? "🥇 1st" : (i == 1 ? "🥈 2nd" : (i == 2 ? "🥉 3rd" : $"{i + 1}th"));
                    leaderboardLines += $"{rank}  {p.playerName}:  {p.objectsFound} Found  •  Round: +{p.RoundScore}  •  Total: {p.TotalScore} Pts\n";
                }
                result.text = $"<b>{status}</b>\nFOUND: {data.foundCount} / 3  ({foundObjects})\nTIME: {FormatTime(data.elapsedTime)}{leaderboardLines}";
            }
            else if (Robot.Score.ScoreManager.Instance != null)
            {
                string breakdown = Robot.Score.ScoreManager.Instance.GetBreakdownString();
                result.text = $"<b>{status}</b>\nFOUND: {data.foundCount} / 3  ({foundObjects})\nTIME: {FormatTime(data.elapsedTime)}\n\n<b>SCORE BREAKDOWN:</b>\n{breakdown}";
            }
            else
            {
                result.text = $"{status}\nFOUND  {data.foundCount} / 3   {foundObjects}\nTIME  {FormatTime(data.elapsedTime)}\nSCORE  +{data.roundScore}\nTOTAL  {data.totalScore}";
            }

            nextRoundButton.gameObject.SetActive(true);
        }

        private IEnumerator CollapseToTrackerRoutine()
        {
            RectTransform introRect = introPanel.GetComponent<RectTransform>();
            float transition = 0f;
            while (transition < 0.3f)
            {
                transition += Time.deltaTime;
                introRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.35f, transition / 0.3f);
                yield return null;
            }
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].transform.SetParent(trackerPanel.transform, false);
                RectTransform cardRect = cards[i].GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(116f, 88f);
                cardRect.anchoredPosition = new Vector2((i - 1) * 125f, -6f);
                previews[i].rectTransform.sizeDelta = new Vector2(60f, 52f);
                previews[i].rectTransform.anchoredPosition = new Vector2(0f, -2f);
                cardChecks[i].rectTransform.anchoredPosition = new Vector2(31f, 20f);
                cardChecks[i].fontSize = 22;
            }
            introPanel.SetActive(false); introRect.localScale = Vector3.one; trackerPanel.SetActive(true);
        }

        private static string FormatTime(float seconds)
        {
            int value = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{value / 60:00}:{value % 60:00}";
        }

        private IEnumerator IntroRoutine()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].transform.SetParent(introPanel.transform, false);
                RectTransform cardRect = cards[i].GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(190f, 150f);
                cardRect.anchoredPosition = new Vector2((i - 1) * 210f, 10f);
                previews[i].rectTransform.sizeDelta = new Vector2(105f, 90f);
                previews[i].rectTransform.anchoredPosition = new Vector2(0f, 15f);
                cardLabels[i].rectTransform.anchoredPosition = new Vector2(0f, -52f);
                cardLabels[i].rectTransform.sizeDelta = new Vector2(180f, 30f);
                cardLabels[i].fontSize = 15;
                cardChecks[i].rectTransform.anchoredPosition = new Vector2(65f, 40f);
                cardChecks[i].fontSize = 34;
            }
            introPanel.SetActive(true);
            trackerPanel.SetActive(false);
            introTitle.text = "FIND THESE";
            string[] steps = { "3", "2", "1", "GO!" };
            foreach (string step in steps)
            {
                countdown.text = step;
                yield return new WaitForSeconds(step == "GO!" ? 0.45f : 0.65f);
            }
            RectTransform introRect = introPanel.GetComponent<RectTransform>();
            Vector2 startAnchor = introRect.anchorMin;
            float transition = 0f;
            while (transition < 0.35f)
            {
                transition += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, transition / 0.35f);
                Vector2 anchor = Vector2.Lerp(startAnchor, new Vector2(0.98f, 0.97f), t);
                introRect.anchorMin = introRect.anchorMax = anchor;
                introRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.35f, t);
                yield return null;
            }
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].transform.SetParent(trackerPanel.transform, false);
                RectTransform cardRect = cards[i].GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(116f, 88f);
                cardRect.anchoredPosition = new Vector2((i - 1) * 125f, -6f);
                previews[i].rectTransform.sizeDelta = new Vector2(60f, 52f);
                previews[i].rectTransform.anchoredPosition = new Vector2(0f, -2f);
                cardLabels[i].rectTransform.anchoredPosition = new Vector2(0f, -23f);
                cardLabels[i].rectTransform.sizeDelta = new Vector2(90f, 20f);
                cardLabels[i].fontSize = 12;
                cardChecks[i].rectTransform.anchoredPosition = new Vector2(31f, 20f);
                cardChecks[i].fontSize = 22;
            }
            introPanel.SetActive(false);
            introRect.anchorMin = introRect.anchorMax = new Vector2(0.5f, 0.5f);
            introRect.localScale = Vector3.one;
            trackerPanel.SetActive(true);
        }

        private void HandleCollected(TargetDefinition target, int count)
        {
            int index = -1;
            for (int i = 0; i < manager.SelectedTargets.Count; i++) if (manager.SelectedTargets[i].objectId == target.objectId) index = i;
            if (index < 0) return;
            cardChecks[index].text = "✓";
            cardLabels[index].color = new Color(1f, 1f, 1f, 0.45f);
        }

        private void HandleTimer(float seconds)
        {
            int value = Mathf.CeilToInt(seconds);
            timer.text = $"{value / 60:00}:{value % 60:00}";
        }

        private void HandleCompleted() => StartCoroutine(ResultRoutine("ALL OBJECTS FOUND!\n3 / 3", "ROUND COMPLETE"));
        private void HandleFailed() => StartCoroutine(ResultRoutine("TIME'S UP\n" + manager.CollectedCount + " / 3", "ROUND FAILED"));

        private IEnumerator ResultRoutine(string first, string second)
        {
            prompt.gameObject.SetActive(false);
            resultPanel.SetActive(true);
            result.text = first;
            yield return new WaitForSeconds(1.5f);
            result.text = second;
        }

        private void BuildUI()
        {
            GameObject canvasObject = new GameObject("Object Hunt HUD");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();

            introPanel = Panel("Round Intro", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(760f, 360f));
            introTitle = Label("FIND THESE", introPanel.transform, 34, new Vector2(0f, 125f), new Vector2(700f, 55f));
            countdown = Label("3", introPanel.transform, 42, new Vector2(0f, -125f), new Vector2(300f, 65f));

            trackerPanel = Panel("Target Tracker", canvas.transform, Vector2.one, new Vector2(400f, 145f));
            RectTransform trackerRect = trackerPanel.GetComponent<RectTransform>();
            trackerRect.pivot = Vector2.one;
            trackerRect.anchoredPosition = new Vector2(-20f, -20f);
            Label("FIND", trackerPanel.transform, 16, new Vector2(0f, 57f), new Vector2(350f, 26f));
            timer = Label("10:00", trackerPanel.transform, 15, new Vector2(0f, -58f), new Vector2(150f, 24f));

            for (int i = 0; i < 3; i++)
            {
                GameObject card = Panel("Target Card " + (i + 1), introPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(190f, 150f));
                card.GetComponent<RectTransform>().anchoredPosition = new Vector2((i - 1) * 210f, 10f);
                RawImage preview = Child<RawImage>("Preview", card.transform, new Vector2(0f, 15f), new Vector2(105f, 90f));
                Text label = Label("Target", card.transform, 15, new Vector2(0f, -52f), new Vector2(180f, 30f));
                label.gameObject.SetActive(false);
                Text check = Label(string.Empty, card.transform, 34, new Vector2(65f, 40f), new Vector2(45f, 45f));
                previews.Add(preview); cardLabels.Add(label); cardChecks.Add(check); cards.Add(card);

                // The same cards move under the persistent tracker after the intro.
                card.transform.SetParent(trackerPanel.transform, false);
                card.GetComponent<RectTransform>().sizeDelta = new Vector2(116f, 88f);
                card.GetComponent<RectTransform>().anchoredPosition = new Vector2((i - 1) * 125f, -6f);
                preview.rectTransform.sizeDelta = new Vector2(60f, 52f);
                preview.rectTransform.anchoredPosition = new Vector2(0f, -2f);
                label.rectTransform.anchoredPosition = new Vector2(0f, -23f);
                label.rectTransform.sizeDelta = new Vector2(90f, 20f);
                label.fontSize = 12;
                check.rectTransform.anchoredPosition = new Vector2(31f, 20f);
                check.fontSize = 22;
            }

            // Intro uses the same three visual cards while tracker is hidden; the transition stays minimal.
            resultPanel = Panel("Round Result", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(650f, 260f));
            result = Label(string.Empty, resultPanel.transform, 24, new Vector2(0f, 25f), new Vector2(610f, 180f));
            nextRoundButton = CreateButton("NEXT ROUND", resultPanel.transform, new Vector2(0f, -100f), new Vector2(220f, 42f));
            nextRoundButton.onClick.AddListener(() => gameLoop?.StartNextRound());
            nextRoundButton.gameObject.SetActive(false);
            resultPanel.SetActive(false);
            promptPanel = Panel("Prompt Background", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(360f, 54f));
            promptPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -140f);
            promptPanel.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.94f);
            prompt = Label("<b>[E]</b>  TOPLA", promptPanel.transform, 22, Vector2.zero, new Vector2(360f, 54f));
            prompt.supportRichText = true;
            prompt.color = new Color(1f, 0.88f, 0.22f, 1f);
            promptPanel.SetActive(false);
            trackerPanel.SetActive(false);
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("Object Hunt EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildPreview(RawImage image, TargetDefinition definition, int index)
        {
            if (definition.icon != null) { image.texture = definition.icon.texture; return; }
            RenderTexture texture = new RenderTexture(128, 128, 16, RenderTextureFormat.ARGB32) { name = definition.objectId + " Preview" };
            Vector3 center = new Vector3(10000f + index * 20f, 10000f, 10000f);
            // These toy prefabs face away from a -Z preview camera at their authored rotation.
            // Rotate the model around so TeddyBear, cars and asymmetric balls show their front/readable side.
            GameObject model = Instantiate(definition.prefab, center, Quaternion.Euler(10f, 210f, 0f));
            model.name = definition.objectId + " HUD Preview";
            SetLayer(model, 31);
            foreach (Collider collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
            Bounds bounds = CalculateBounds(model);
            GameObject cameraObject = new GameObject(definition.objectId + " Preview Camera");
            Camera previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
            previewCamera.cullingMask = 1 << 31;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.35f;
            previewCamera.transform.position = bounds.center + new Vector3(0f, bounds.extents.y * 0.15f, -5f);
            previewCamera.transform.LookAt(bounds.center);
            previewCamera.targetTexture = texture;
            image.texture = texture;
            previewObjects.Add(model); previewObjects.Add(cameraObject);
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Bounds result = renderers.Length > 0 ? renderers[0].bounds : new Bounds(root.transform.position, Vector3.one);
            foreach (Renderer renderer in renderers) result.Encapsulate(renderer.bounds);
            return result;
        }

        private static void SetLayer(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform) SetLayer(child.gameObject, layer);
        }

        private static GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            Image image = Child<Image>(name, parent, Vector2.zero, size);
            image.color = name.StartsWith("Target Card") ? RobotHudTheme.ControlColor : RobotHudTheme.PanelColor;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = anchor;
            return image.gameObject;
        }

        private static Text Label(string value, Transform parent, int size, Vector2 position, Vector2 dimensions)
        {
            Text text = Child<Text>(value, parent, position, dimensions);
            text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        private static T Child<T>(string name, Transform parent, Vector2 position, Vector2 size) where T : Graphic
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            RectTransform rect = child.AddComponent<RectTransform>();
            rect.sizeDelta = size; rect.anchoredPosition = position;
            return child.AddComponent<T>();
        }

        private static Button CreateButton(string textValue, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject root = new GameObject(textValue);
            root.transform.SetParent(parent, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = root.AddComponent<Image>(); image.color = RobotHudTheme.ControlColor;
            Button button = root.AddComponent<Button>();
            Text label = Label(textValue, root.transform, 15, Vector2.zero, size);
            label.raycastTarget = false;
            return button;
        }
    }
}
