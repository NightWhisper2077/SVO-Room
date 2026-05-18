using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FinalProject
{
    public static class SmartRoomRuntimeBootstrap
    {
        private const string RootName = "Final Project Systems";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "Start Scene")
                return;

            if (GameObject.Find(RootName) != null)
                return;

            EnsureEventSystem();

            var root = new GameObject(RootName);
            var routeMaterial = CreateMaterial("Runtime A* Route", new Color(0.1f, 0.9f, 1f, 1f), true);
            var markerMaterial = CreateMaterial("Runtime A* Marker", new Color(1f, 0.55f, 0.08f, 1f), false);
            var waypointMaterial = CreateMaterial("Runtime A* Waypoint", new Color(0.1f, 0.75f, 1f, 1f), false);

            var pathfinder = CreatePathfinder(root.transform);
            var waypoints = CreateWaypoints(root.transform, waypointMaterial);
            var guide = CreateGuide(root.transform, pathfinder, waypoints, routeMaterial, markerMaterial);
            CreateMenu(root.transform, guide);
        }

        private static AStarPathfinder CreatePathfinder(Transform parent)
        {
            var go = new GameObject("A* Pathfinder");
            go.transform.SetParent(parent, false);

            var pathfinder = go.AddComponent<AStarPathfinder>();
            pathfinder.Configure(Vector2.zero, new Vector2(7f, 7f), 0.35f, 0.9f, LayerMask.GetMask("Default"));
            return pathfinder;
        }

        private static Transform[] CreateWaypoints(Transform parent, Material material)
        {
            var waypointRoot = new GameObject("A* Route Points");
            waypointRoot.transform.SetParent(parent, false);

            var positions = new[]
            {
                new Vector3(-2.7f, 0.9f, -2.6f),
                new Vector3(2.6f, 0.9f, -2.35f),
                new Vector3(2.55f, 0.9f, 2.35f),
                new Vector3(-2.45f, 0.9f, 2.45f),
                new Vector3(0f, 0.9f, 0.2f),
            };

            var waypoints = new Transform[positions.Length];
            for (var i = 0; i < positions.Length; i++)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"A* Waypoint {i + 1}";
                marker.transform.SetParent(waypointRoot.transform, false);
                marker.transform.position = positions[i];
                marker.transform.localScale = Vector3.one * 0.12f;

                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                    Object.Destroy(collider);

                var renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = material;

                waypoints[i] = marker.transform;
            }

            return waypoints;
        }

        private static SafetyGuideAgent CreateGuide(Transform parent, AStarPathfinder pathfinder, Transform[] waypoints, Material routeMaterial, Material markerMaterial)
        {
            var go = new GameObject("A* Safety Guide Drone");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-2.7f, 0.9f, -2.6f);

            var modelRoot = new GameObject("SafetyGuideDroneModel").transform;
            modelRoot.SetParent(go.transform, false);

            var model = Resources.Load<GameObject>("FinalProject/SafetyGuideDrone");
            if (model != null)
            {
                var instance = Object.Instantiate(model, modelRoot);
                instance.name = "Blender SafetyGuideDrone";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one * 0.55f;
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.name = "Fallback Drone Body";
                body.transform.SetParent(modelRoot, false);
                body.transform.localScale = new Vector3(0.35f, 0.18f, 0.28f);
                Object.Destroy(body.GetComponent<Collider>());
            }

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.25f, 0.8f, 1f);
            light.range = 2.2f;
            light.intensity = 1.3f;

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.startWidth = 0.035f;
            line.endWidth = 0.035f;
            line.numCapVertices = 4;
            line.sharedMaterial = routeMaterial;

            var markerPoolGo = new GameObject("Route Marker Pool");
            markerPoolGo.transform.SetParent(go.transform, false);
            var markerPool = markerPoolGo.AddComponent<RouteMarkerPool>();
            markerPool.Configure(markerMaterial, 0.07f, 2);

            var guide = go.AddComponent<SafetyGuideAgent>();
            guide.Configure(pathfinder, waypoints, line, markerPool, modelRoot, 1.25f, true);
            return guide;
        }

        private static void CreateMenu(Transform parent, SafetyGuideAgent guide)
        {
            var menuGo = new GameObject("Smart Room Settings Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster), typeof(CanvasGroup));
            menuGo.transform.SetParent(parent, false);
            menuGo.transform.position = new Vector3(0f, 1.55f, 2.55f);
            menuGo.transform.rotation = Quaternion.identity;
            menuGo.transform.localScale = Vector3.one * 0.00155f;

            var rect = menuGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(720f, 520f);

            var canvas = menuGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            menuGo.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 12f;
            var canvasGroup = menuGo.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var panel = CreatePanel(menuGo.transform, "Panel", new Color(0.035f, 0.045f, 0.05f, 0.92f));
            Stretch(panel.rectTransform);

            CreateText(panel.transform, "Title", "VR Smart Room Safety Training", 34f, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(660f, 56f));
            CreateText(panel.transform, "Subtitle", "Настройки проекта и демонстрация A*", 18f, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -83f), new Vector2(660f, 36f));

            CreateText(panel.transform, "Music Label", "Музыка", 20f, FontStyles.Bold, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(55f, -145f), new Vector2(190f, 34f));
            var volumeSlider = CreateSlider(panel.transform, "Music Volume Slider", new Vector2(395f, -145f), 0f, 1f, 0.65f);

            CreateText(panel.transform, "Speed Label", "Скорость дрона", 20f, FontStyles.Bold, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(55f, -205f), new Vector2(220f, 34f));
            var speedSlider = CreateSlider(panel.transform, "Guide Speed Slider", new Vector2(395f, -205f), 0.4f, 3f, 1.25f);

            var routeToggle = CreateToggle(panel.transform, "Route Toggle", "Показывать маршрут A*", new Vector2(55f, -270f), true);
            var guideToggle = CreateToggle(panel.transform, "Guide Toggle", "Дрон-помощник", new Vector2(365f, -270f), true);
            var playVideoButton = CreateButton(panel.transform, "Play Video Button", "Включить видео", new Vector2(185f, -348f), new Vector2(240f, 54f));
            var pauseVideoButton = CreateButton(panel.transform, "Pause Video Button", "Пауза видео", new Vector2(455f, -348f), new Vector2(210f, 54f));
            var restartButton = CreateButton(panel.transform, "Restart Route Button", "Перестроить A*", new Vector2(602f, -270f), new Vector2(190f, 48f));
            var statusText = CreateText(panel.transform, "Status Text", "Комната готова.", 17f, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(640f, 54f));

            var manager = menuGo.AddComponent<SmartRoomSettingsMenu>();
            var music = FindComponentOnNamedObject<AudioSource>("Music") ?? Object.FindFirstObjectByType<AudioSource>();
            var video = Object.FindFirstObjectByType<VideoPlayer>();
            manager.Configure(canvasGroup, music, video, guide, volumeSlider, speedSlider, routeToggle, guideToggle, statusText);

            volumeSlider.onValueChanged.AddListener(manager.SetMusicVolume);
            speedSlider.onValueChanged.AddListener(manager.SetGuideSpeed);
            routeToggle.onValueChanged.AddListener(manager.SetRouteVisible);
            guideToggle.onValueChanged.AddListener(manager.SetGuideEnabled);
            playVideoButton.onClick.AddListener(manager.PlayTrainingVideo);
            pauseVideoButton.onClick.AddListener(manager.PauseTrainingVideo);
            restartButton.onClick.AddListener(manager.RestartGuideRoute);

            if (music != null)
                volumeSlider.SetValueWithoutNotify(music.volume);
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = go.GetComponent<EventSystem>();
            }

            var standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
                Object.Destroy(standalone);

            if (eventSystem.GetComponent<XRUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }

        private static Material CreateMaterial(string name, Color color, bool unlit)
        {
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader)
            {
                name = name,
                color = color,
            };
            return material;
        }

        private static T FindComponentOnNamedObject<T>(string name) where T : Component
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<T>() : null;
        }

        private static Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, FontStyles style, TextAlignmentOptions alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, anchoredPosition, sizeDelta);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = new Color(0.92f, 0.96f, 0.94f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPosition, float min, float max, float value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, new Vector2(270f, 34f));

            var background = CreatePanel(go.transform, "Background", new Color(0.11f, 0.14f, 0.14f, 1f));
            Stretch(background.rectTransform);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);

            var fill = CreatePanel(fillArea.transform, "Fill", new Color(0.1f, 0.75f, 1f, 1f));
            Stretch(fill.rectTransform);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);

            var handle = CreatePanel(handleArea.transform, "Handle", new Color(1f, 0.58f, 0.1f, 1f));
            SetRect(handle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(28f, 42f));

            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        private static Toggle CreateToggle(Transform parent, string name, string text, Vector2 anchoredPosition, bool value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, new Vector2(270f, 42f));

            var background = CreatePanel(go.transform, "Box", new Color(0.12f, 0.15f, 0.15f, 1f));
            SetRect(background.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(32f, 32f));

            var checkmark = CreatePanel(background.transform, "Checkmark", new Color(0.1f, 0.75f, 1f, 1f));
            Stretch(checkmark.rectTransform, 7f, 7f, 7f, 7f);

            CreateText(go.transform, "Label", text, 18f, FontStyles.Normal, TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(160f, 0f), new Vector2(220f, 34f));

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            toggle.isOn = value;
            return toggle;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.5f, 0.72f, 1f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            CreateText(go.transform, "Label", text, 18f, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f, 0f, 0f, 0f);
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

    }
}
