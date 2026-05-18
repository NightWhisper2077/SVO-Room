using FinalProject;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FinalProject.Editor
{
    public static class SmartRoomFinalProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Start Scene.unity";
        private const string ModelPath = "Assets/FinalProject/Models/SafetyGuideDrone.fbx";

        [MenuItem("Tools/Smart Room/Setup Final Project Scene")]
        public static void SetupScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureEventSystem();

            var root = GetOrCreate("Final Project Systems");
            var waypointMaterial = GetOrCreateMaterial("Assets/FinalProject/Materials/AStarWaypoint.mat", new Color(0.1f, 0.75f, 1f), false);
            var routeMaterial = GetOrCreateMaterial("Assets/FinalProject/Materials/AStarRoute.mat", new Color(0.1f, 0.95f, 1f), true);
            var markerMaterial = GetOrCreateMaterial("Assets/FinalProject/Materials/AStarRouteMarker.mat", new Color(1f, 0.55f, 0.08f), false);

            var pathfinder = CreatePathfinder(root.transform);
            var waypoints = CreateWaypoints(root.transform, waypointMaterial);
            var guide = CreateGuide(root.transform, pathfinder, waypoints, routeMaterial, markerMaterial);
            CreateSettingsMenu(root.transform, guide);

            PlayerSettings.productName = "VR Smart Room Safety Training";
            PlayerSettings.bundleVersion = "1.0";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Smart Room final project setup completed.");
        }

        public static void SetupSceneFromCommandLine()
        {
            SetupScene();
        }

        private static AStarPathfinder CreatePathfinder(Transform parent)
        {
            var go = GetOrCreate("A* Pathfinder", parent);
            go.transform.position = Vector3.zero;

            var pathfinder = GetOrAdd<AStarPathfinder>(go);
            pathfinder.Configure(Vector2.zero, new Vector2(7f, 7f), 0.35f, 0.9f, LayerMask.GetMask("Default"));
            SetBool(pathfinder, "allowDiagonals", true);
            SetFloat(pathfinder, "obstacleSampleHeight", 0.7f);
            SetFloat(pathfinder, "obstacleSampleHalfHeight", 0.3f);
            return pathfinder;
        }

        private static Transform[] CreateWaypoints(Transform parent, Material material)
        {
            var waypointRoot = GetOrCreate("A* Route Points", parent);
            ClearChildren(waypointRoot.transform);

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
                    Object.DestroyImmediate(collider);

                var renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = material;

                waypoints[i] = marker.transform;
            }

            return waypoints;
        }

        private static SafetyGuideAgent CreateGuide(Transform parent, AStarPathfinder pathfinder, Transform[] waypoints, Material routeMaterial, Material markerMaterial)
        {
            var go = GetOrCreate("A* Safety Guide Drone", parent);
            go.transform.position = new Vector3(-2.7f, 0.9f, -2.6f);
            go.transform.rotation = Quaternion.identity;

            ClearChildren(go.transform);

            var modelRoot = new GameObject("SafetyGuideDroneModel").transform;
            modelRoot.SetParent(go.transform, false);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelAsset != null)
            {
                var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                modelInstance.name = "Blender SafetyGuideDrone";
                modelInstance.transform.SetParent(modelRoot, false);
                modelInstance.transform.localScale = Vector3.one * 0.55f;
            }
            else
            {
                CreateFallbackGuideModel(modelRoot);
            }

            var light = GetOrAdd<Light>(go);
            light.type = LightType.Point;
            light.color = new Color(0.25f, 0.8f, 1f);
            light.range = 2.2f;
            light.intensity = 1.3f;

            var line = GetOrAdd<LineRenderer>(go);
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.startWidth = 0.035f;
            line.endWidth = 0.035f;
            line.numCapVertices = 4;
            line.sharedMaterial = routeMaterial;

            var markerPoolGo = GetOrCreate("Route Marker Pool", go.transform);
            var markerPool = GetOrAdd<RouteMarkerPool>(markerPoolGo);
            SetObject(markerPool, "markerMaterial", markerMaterial);
            SetFloat(markerPool, "markerScale", 0.07f);
            SetInt(markerPool, "stride", 2);

            var guide = GetOrAdd<SafetyGuideAgent>(go);
            SetObject(guide, "pathfinder", pathfinder);
            SetObject(guide, "routeLine", line);
            SetObject(guide, "markerPool", markerPool);
            SetObject(guide, "modelRoot", modelRoot);
            SetTransformArray(guide, "routePoints", waypoints);
            SetFloat(guide, "moveSpeed", 1.25f);
            SetBool(guide, "showRoute", true);
            SetBool(guide, "runOnStart", true);
            return guide;
        }

        private static void CreateFallbackGuideModel(Transform parent)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Fallback Drone Body";
            body.transform.SetParent(parent, false);
            body.transform.localScale = new Vector3(0.35f, 0.18f, 0.28f);
            var collider = body.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
        }

        private static void CreateSettingsMenu(Transform parent, SafetyGuideAgent guide)
        {
            var menuGo = GetOrCreate("Smart Room Settings Menu", parent);
            menuGo.transform.position = new Vector3(0f, 1.55f, 2.55f);
            menuGo.transform.rotation = Quaternion.identity;
            menuGo.transform.localScale = Vector3.one * 0.00155f;
            ClearChildren(menuGo.transform);

            var canvas = GetOrAdd<Canvas>(menuGo);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var canvasRect = menuGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(720f, 520f);

            GetOrAdd<CanvasScaler>(menuGo).dynamicPixelsPerUnit = 12f;
            GetOrAdd<GraphicRaycaster>(menuGo);
            GetOrAdd<TrackedDeviceGraphicRaycaster>(menuGo);
            var canvasGroup = GetOrAdd<CanvasGroup>(menuGo);
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

            var manager = GetOrAdd<SmartRoomSettingsMenu>(menuGo);
            var music = FindComponentOnNamedObject<AudioSource>("Music") ?? Object.FindFirstObjectByType<AudioSource>();
            var video = Object.FindFirstObjectByType<VideoPlayer>();

            SetObject(manager, "canvasGroup", canvasGroup);
            SetObject(manager, "musicSource", music);
            SetObject(manager, "trainingVideo", video);
            SetObject(manager, "guideAgent", guide);
            SetObject(manager, "musicVolumeSlider", volumeSlider);
            SetObject(manager, "guideSpeedSlider", speedSlider);
            SetObject(manager, "routeToggle", routeToggle);
            SetObject(manager, "guideToggle", guideToggle);
            SetObject(manager, "statusText", statusText);

            UnityEventTools.AddPersistentListener(volumeSlider.onValueChanged, manager.SetMusicVolume);
            UnityEventTools.AddPersistentListener(speedSlider.onValueChanged, manager.SetGuideSpeed);
            UnityEventTools.AddPersistentListener(routeToggle.onValueChanged, manager.SetRouteVisible);
            UnityEventTools.AddPersistentListener(guideToggle.onValueChanged, manager.SetGuideEnabled);
            UnityEventTools.AddPersistentListener(playVideoButton.onClick, manager.PlayTrainingVideo);
            UnityEventTools.AddPersistentListener(pauseVideoButton.onClick, manager.PauseTrainingVideo);
            UnityEventTools.AddPersistentListener(restartButton.onClick, manager.RestartGuideRoute);

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
                Object.DestroyImmediate(standalone);

            GetOrAdd<XRUIInputModule>(eventSystem.gameObject);
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
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            return button;
        }

        private static Material GetOrCreateMaterial(string path, Color color, bool transparent)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find(transparent ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            return material;
        }

        private static T FindComponentOnNamedObject<T>(string name) where T : Component
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<T>() : null;
        }

        private static GameObject GetOrCreate(string name, Transform parent = null)
        {
            Transform existing = null;
            if (parent != null)
                existing = parent.Find(name);
            else
            {
                var go = GameObject.Find(name);
                if (go != null)
                    existing = go.transform;
            }

            if (existing != null)
                return existing.gameObject;

            var created = new GameObject(name);
            if (parent != null)
                created.transform.SetParent(parent, false);

            return created;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();

            return component;
        }

        private static void ClearChildren(Transform transform)
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
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

        private static void SetObject(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetTransformArray(Object target, string propertyName, Transform[] values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;

            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
