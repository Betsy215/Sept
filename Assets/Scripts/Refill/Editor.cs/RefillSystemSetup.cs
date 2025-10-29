using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

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

        // Diagnostic Section (at the top for troubleshooting)
        GUILayout.Label("🔍 Troubleshooting", EditorStyles.boldLabel);
        if (GUILayout.Button("Diagnose Script Issues", GUILayout.Height(30))) DiagnoseScriptIssues();

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

        if (GUILayout.Button("Enable Refill on All Items", GUILayout.Height(30))) EnableRefillOnAllItems();

        GUILayout.Space(20);

        EditorGUILayout.HelpBox(
            "1. Click 'Diagnose Script Issues' first to check if all scripts compile\n" +
            "2. Create both prefabs\n" +
            "3. Setup RefillSystem in scene\n" +
            "4. Select food items and add RefillableItem components\n" +
            "5. Use 'Enable Refill on All Items' to ensure all items have refill enabled\n" +
            "6. Replace existing scripts with updated versions",
            MessageType.None);
    }

    private void CreateCountUIPrefab()
    {
        // Create Canvas
        var canvasGO = new GameObject("CountUIPrefab");
        canvasGO.SetActive(true); // Ensure prefab is active
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Changed to Overlay for reliability

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

        // Configure RectTransforms with proper sizes
        var canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100, 100); // Give the canvas a size

        var displayRect = countDisplay.GetComponent<RectTransform>();
        displayRect.sizeDelta = new Vector2(40, 40);
        displayRect.anchoredPosition = Vector2.zero;

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // Set initial position for the canvas to screen center (will be repositioned by RefillCountUI)
        canvasRect.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);

        // Add RefillCountUI component to the root Canvas GameObject
        // First check if the RefillCountUI script exists
        var refillCountUIType = System.Type.GetType("RefillCountUI");
        if (refillCountUIType == null)
        {
            Debug.LogError(
                "❌ RefillCountUI script not found! Make sure RefillCountUI.cs is in your project and compiles correctly.");
            EditorUtility.DisplayDialog("Error",
                "RefillCountUI script not found! Please ensure RefillCountUI.cs is in your project.", "OK");
            DestroyImmediate(canvasGO);
            return;
        }

        var countUI = canvasGO.AddComponent<RefillCountUI>();
        countUI.countText = text;
        countUI.backgroundImage = backgroundImage;
        countUI.inStockColor = countTextColor;
        countUI.backgroundInStockColor = countBackgroundColor;

        Debug.Log("✅ RefillCountUI component added to canvas GameObject");

        // Save as prefab
        var prefabPath = "Assets/Prefabs/UI/CountUIPrefab.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");

        var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);

        // Verify the prefab was created correctly
        var verifyComponent = prefab.GetComponent<RefillCountUI>();
        if (verifyComponent != null)
            Debug.Log("✅ RefillCountUI component verified on prefab");
        else
            Debug.LogError("❌ RefillCountUI component missing from prefab!");

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

        // Add RefillStatusBar component to the root Canvas GameObject
        // First check if the RefillStatusBar script exists
        var refillStatusBarType = System.Type.GetType("RefillStatusBar");
        if (refillStatusBarType == null)
        {
            Debug.LogError(
                "❌ RefillStatusBar script not found! Make sure RefillStatusBar.cs is in your project and compiles correctly.");
            EditorUtility.DisplayDialog("Error",
                "RefillStatusBar script not found! Please ensure RefillStatusBar.cs is in your project.", "OK");
            DestroyImmediate(canvasGO);
            return;
        }

        var statusBarComponent = canvasGO.AddComponent<RefillStatusBar>();
        statusBarComponent.fillImage = fillImage;
        statusBarComponent.backgroundImage = backgroundImage;
        statusBarComponent.fillColor = statusBarFillColor;
        statusBarComponent.backgroundColor = statusBarBackgroundColor;

        Debug.Log("✅ RefillStatusBar component added to canvas GameObject");

        // Save as prefab
        var prefabPath = "Assets/Prefabs/UI/StatusBarPrefab.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");

        var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);

        // Verify the prefab was created correctly
        var verifyComponent = prefab.GetComponent<RefillStatusBar>();
        if (verifyComponent != null)
            Debug.Log("✅ RefillStatusBar component verified on prefab");
        else
            Debug.LogError("❌ RefillStatusBar component missing from prefab!");

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

    private void DiagnoseScriptIssues()
    {
        Debug.Log("=== REFILL SYSTEM SCRIPT DIAGNOSIS ===");

        // Check if all required scripts exist and compile correctly
        string[] requiredScripts =
        {
            "RefillSystem",
            "RefillableItem",
            "RefillCountUI",
            "RefillStatusBar"
        };

        var allScriptsFound = true;

        foreach (var scriptName in requiredScripts)
        {
            var scriptType = System.Type.GetType(scriptName);
            if (scriptType != null)
            {
                Debug.Log($"✅ {scriptName} script found and compiled");
            }
            else
            {
                Debug.LogError($"❌ {scriptName} script missing or failed to compile!");
                allScriptsFound = false;
            }
        }

        // Check if prefabs exist
        string[] prefabPaths =
        {
            "Assets/Prefabs/UI/CountUIPrefab.prefab",
            "Assets/Prefabs/UI/StatusBarPrefab.prefab"
        };

        foreach (var prefabPath in prefabPaths)
            if (System.IO.File.Exists(prefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    Debug.Log($"✅ Prefab found: {prefabPath}");

                    // Check components on prefab
                    if (prefabPath.Contains("CountUI"))
                    {
                        var countUI = prefab.GetComponent<RefillCountUI>();
                        Debug.Log($"  RefillCountUI component: {(countUI != null ? "✅ Found" : "❌ Missing")}");
                    }
                    else if (prefabPath.Contains("StatusBar"))
                    {
                        var statusBar = prefab.GetComponent<RefillStatusBar>();
                        Debug.Log($"  RefillStatusBar component: {(statusBar != null ? "✅ Found" : "❌ Missing")}");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Failed to load prefab: {prefabPath}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Prefab not found: {prefabPath}");
            }

        if (allScriptsFound)
            EditorUtility.DisplayDialog("Diagnosis Complete",
                "All required scripts found and compiled successfully! Check Console for detailed results.", "OK");
        else
            EditorUtility.DisplayDialog("Script Issues Found",
                "Some required scripts are missing or failed to compile! Check Console for details.", "OK");

        Debug.Log("=== DIAGNOSIS COMPLETE ===");
    }

    private void EnableRefillOnAllItems()
    {
        var allRefillableItems = FindObjectsOfType<RefillableItem>();

        if (allRefillableItems.Length == 0)
        {
            EditorUtility.DisplayDialog("No Items Found", "No RefillableItem components found in the scene.", "OK");
            return;
        }

        var enabledCount = 0;

        foreach (var item in allRefillableItems)
            if (!item.enableRefill)
            {
                item.enableRefill = true;
                enabledCount++;
                Debug.Log($"Enabled refill for {item.name}");
            }

        if (enabledCount > 0)
            EditorUtility.DisplayDialog("Success",
                $"Enabled refill on {enabledCount} items!\n\nTotal RefillableItems in scene: {allRefillableItems.Length}",
                "OK");
        else
            EditorUtility.DisplayDialog("Info",
                $"All {allRefillableItems.Length} RefillableItems already have refill enabled.", "OK");
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
                    refillableItem.enableRefill = true; // Explicitly set to true
                    refillableItem.topPadding = 0.5f;
                    addedCount++;
                    Debug.Log($"Added RefillableItem to {obj.name}");
                }
                else
                {
                    // Also ensure existing components have enableRefill set to true
                    if (!existingRefillable.enableRefill)
                    {
                        existingRefillable.enableRefill = true;
                        Debug.Log($"Enabled refill for existing RefillableItem on {obj.name}");
                    }
                    else
                    {
                        Debug.Log($"{obj.name} already has RefillableItem component with refill enabled");
                    }
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