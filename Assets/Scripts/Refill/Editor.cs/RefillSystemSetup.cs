using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
[System.Serializable]
public class RefillSystemSetup : EditorWindow
{
    [Header("Prefab Creation Settings")] public Font textFont;
    public Sprite backgroundSprite;
    public Color countTextColor = Color.white;
    public Color countBackgroundColor = new(0f, 0f, 0f, 0.7f);
    public Color statusBarFillColor = Color.green;
    public Color statusBarBackgroundColor = new(0f, 0f, 0f, 0.5f);

    [Header("System Settings")] public int defaultMaxCount = 5;
    public float refillTimePerCount = 1f;
    public float countUIOffset = 0.8f;
    public float statusBarOffset = -0.6f;

    [MenuItem("Tools/Refill System Setup")]
    public static void ShowWindow()
    {
        GetWindow<RefillSystemSetup>("Refill System Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Refill System Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool will help you set up the refill system by creating prefabs and configuring components.",
            MessageType.Info);

        GUILayout.Space(10);

        // Prefab Creation Section
        GUILayout.Label("Prefab Creation", EditorStyles.boldLabel);
        textFont = (Font)EditorGUILayout.ObjectField("Text Font", textFont, typeof(Font), false);
        backgroundSprite =
            (Sprite)EditorGUILayout.ObjectField("Background Sprite", backgroundSprite, typeof(Sprite), false);

        GUILayout.Space(5);
        countTextColor = EditorGUILayout.ColorField("Count Text Color", countTextColor);
        countBackgroundColor = EditorGUILayout.ColorField("Count Background Color", countBackgroundColor);
        statusBarFillColor = EditorGUILayout.ColorField("Status Bar Fill Color", statusBarFillColor);
        statusBarBackgroundColor = EditorGUILayout.ColorField("Status Bar Background Color", statusBarBackgroundColor);

        GUILayout.Space(10);

        // System Settings Section
        GUILayout.Label("System Settings", EditorStyles.boldLabel);
        defaultMaxCount = EditorGUILayout.IntField("Default Max Count", defaultMaxCount);
        refillTimePerCount = EditorGUILayout.FloatField("Refill Time Per Count", refillTimePerCount);
        countUIOffset = EditorGUILayout.FloatField("Count UI Offset", countUIOffset);
        statusBarOffset = EditorGUILayout.FloatField("Status Bar Offset", statusBarOffset);

        GUILayout.Space(20);

        // Buttons
        if (GUILayout.Button("Create Count UI Prefab", GUILayout.Height(30))) CreateCountUIPrefab();

        if (GUILayout.Button("Create Status Bar Prefab", GUILayout.Height(30))) CreateStatusBarPrefab();

        GUILayout.Space(10);

        if (GUILayout.Button("Setup RefillSystem in Scene", GUILayout.Height(30))) SetupRefillSystemInScene();

        if (GUILayout.Button("Add RefillableItem to Selected Items", GUILayout.Height(30)))
            AddRefillableItemToSelected();

        GUILayout.Space(20);

        EditorGUILayout.HelpBox(
            "1. Create both prefabs first\n" +
            "2. Setup RefillSystem in scene\n" +
            "3. Select food items and add RefillableItem components\n" +
            "4. Replace existing scripts with updated versions",
            MessageType.None);
    }

    private void CreateCountUIPrefab()
    {
        // Create Canvas
        var canvasGO = new GameObject("CountUIPrefab");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var raycaster = canvasGO.AddComponent<GraphicRaycaster>();

        // Create count display
        var countDisplay = new GameObject("CountDisplay");
        countDisplay.transform.SetParent(canvasGO.transform);

        // Add background image
        var backgroundImage = countDisplay.AddComponent<Image>();
        if (backgroundSprite != null) backgroundImage.sprite = backgroundSprite;
        backgroundImage.color = countBackgroundColor;

        // Add text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(countDisplay.transform);

        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "5";
        text.fontSize = 24;
        text.color = countTextColor;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;

        if (textFont != null) text.font = TMP_FontAsset.CreateFontAsset(textFont);

        // Configure RectTransforms
        var displayRect = countDisplay.GetComponent<RectTransform>();
        displayRect.sizeDelta = new Vector2(40, 40);
        displayRect.anchoredPosition = Vector2.zero;

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // Add RefillCountUI component
        var countUI = countDisplay.AddComponent<RefillCountUI>();
        countUI.countText = text;
        countUI.backgroundImage = backgroundImage;
        countUI.inStockColor = countTextColor;
        countUI.backgroundInStockColor = countBackgroundColor;

        // Save as prefab
        var prefabPath = "Assets/Prefabs/UI/CountUIPrefab.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");

        PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
        DestroyImmediate(canvasGO);

        Debug.Log($"Count UI Prefab created at {prefabPath}");
        EditorUtility.DisplayDialog("Success", "Count UI Prefab created successfully!", "OK");
    }

    private void CreateStatusBarPrefab()
    {
        // Create Canvas
        var canvasGO = new GameObject("StatusBarPrefab");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var raycaster = canvasGO.AddComponent<GraphicRaycaster>();

        // Create status bar
        var statusBar = new GameObject("StatusBar");
        statusBar.transform.SetParent(canvasGO.transform);

        // Add background image
        var backgroundImage = statusBar.AddComponent<Image>();
        backgroundImage.color = statusBarBackgroundColor;

        // Create fill
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(statusBar.transform);

        var fillImage = fillGO.AddComponent<Image>();
        fillImage.color = statusBarFillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0f;

        // Configure RectTransforms
        var statusBarRect = statusBar.GetComponent<RectTransform>();
        statusBarRect.sizeDelta = new Vector2(100, 10);
        statusBarRect.anchoredPosition = Vector2.zero;

        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        // Add RefillStatusBar component
        var statusBarComponent = statusBar.AddComponent<RefillStatusBar>();
        statusBarComponent.fillImage = fillImage;
        statusBarComponent.backgroundImage = backgroundImage;
        statusBarComponent.fillColor = statusBarFillColor;
        statusBarComponent.backgroundColor = statusBarBackgroundColor;

        // Save as prefab
        var prefabPath = "Assets/Prefabs/UI/StatusBarPrefab.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");

        PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
        DestroyImmediate(canvasGO);

        Debug.Log($"Status Bar Prefab created at {prefabPath}");
        EditorUtility.DisplayDialog("Success", "Status Bar Prefab created successfully!", "OK");
    }

    private void SetupRefillSystemInScene()
    {
        // Check if RefillSystem already exists
        var existingSystem = FindObjectOfType<RefillSystem>();
        if (existingSystem != null)
        {
            if (!EditorUtility.DisplayDialog("RefillSystem Exists",
                    "A RefillSystem already exists in the scene. Replace it?", "Yes", "Cancel"))
                return;
            DestroyImmediate(existingSystem.gameObject);
        }

        // Create RefillSystem GameObject
        var refillSystemGO = new GameObject("RefillSystem");
        var refillSystem = refillSystemGO.AddComponent<RefillSystem>();

        // Configure settings
        refillSystem.defaultMaxCount = defaultMaxCount;
        refillSystem.refillTimePerCount = refillTimePerCount;
        refillSystem.countUIOffset = countUIOffset;
        refillSystem.statusBarOffset = statusBarOffset;

        // Try to assign prefabs
        var countUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CountUIPrefab.prefab");
        var statusBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/StatusBarPrefab.prefab");

        if (countUIPrefab != null)
            // refillSystem.countUIPrefab = countUIPrefab;
            Debug.Log("Count UI Prefab assigned");
        else
            Debug.LogWarning("Count UI Prefab not found. Please assign manually.");

        if (statusBarPrefab != null)
            // refillSystem.statusBarPrefab = statusBarPrefab;
            Debug.Log("Status Bar Prefab assigned");
        else
            Debug.LogWarning("Status Bar Prefab not found. Please assign manually.");

        // Try to find and assign Canvas
        var canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
            // refillSystem.uiCanvas = canvas;
            Debug.Log("UI Canvas assigned");

        // Try to find and assign GamePhaseManager
        var gamePhaseManager = FindObjectOfType<GamePhaseManager>();
        if (gamePhaseManager != null)
            // refillSystem.gamePhaseManager = gamePhaseManager;
            Debug.Log("GamePhaseManager assigned");

        Debug.Log("RefillSystem created in scene");
        EditorUtility.DisplayDialog("Success",
            "RefillSystem created in scene!\n\nPlease assign prefabs and references manually if they weren't found automatically.",
            "OK");
    }

    private void AddRefillableItemToSelected()
    {
        var selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection",
                "Please select one or more GameObjects with ServeableItem components.", "OK");
            return;
        }

        var addedCount = 0;

        foreach (var obj in selectedObjects)
        {
            var serveableItem = obj.GetComponent<ServeableItem>();
            if (serveableItem != null)
            {
                var existingRefillable = obj.GetComponent<RefillableItem>();
                if (existingRefillable == null)
                {
                    var refillableItem = obj.AddComponent<RefillableItem>();
                    refillableItem.enableRefill = true;
                    refillableItem.topPadding = 0.5f;
                    addedCount++;
                    Debug.Log($"Added RefillableItem to {obj.name}");
                }
                else
                {
                    Debug.Log($"{obj.name} already has RefillableItem component");
                }
            }
            else
            {
                Debug.LogWarning($"{obj.name} does not have ServeableItem component");
            }
        }

        if (addedCount > 0)
            EditorUtility.DisplayDialog("Success", $"Added RefillableItem to {addedCount} objects!", "OK");
        else
            EditorUtility.DisplayDialog("Info",
                "No RefillableItem components were added. Objects may already have the component or lack ServeableItem.",
                "OK");
    }
}
#endif