using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpeechBubble : MonoBehaviour
{
    [Header("Bubble Components")]
    public SpriteRenderer bubbleBackground;
    public SpriteRenderer bubbleTail; // The little triangle pointing to character
    public Transform foodItemContainer; // Where food icons go
    
    [Header("Animation Settings")]
    public float appearDuration = 0.3f;
    public float disappearDuration = 0.2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public Vector3 bubbleOffset = new Vector3(0, 2f, 0); // Offset above customer
    
    [Header("Layout Configuration")]
    [Tooltip("Spacing between food items in the bubble (auto-syncs with OrderSystem if available)")]
    public float foodItemSpacing = 0.8f;
    [Tooltip("Maximum items per row in the bubble")]
    public int maxItemsPerRow = 3;
    [Tooltip("Scale multiplier for food items in bubble (relative to original prefab size)")]
    public Vector2 foodItemScale = new Vector2(0.6f, 0.6f);
    [Tooltip("Should spacing auto-sync with OrderSystem?")]
    public bool autoSyncSpacing = true;
    [Tooltip("Spacing multiplier when syncing (if OrderSystem spacing is too large for bubble)")]
    public float spacingMultiplier = 0.5f;
    
    [Header("Bubble Sprites")]
    public Sprite bubbleSprite; // White rounded rectangle
    public Sprite tailSprite; // Small triangle
    public Color bubbleColor = Color.white;
    
    [Header("Dynamic Bubble Sizing")]
    [Tooltip("Should the bubble background auto-resize based on content?")]
    public bool autoResizeBubble = true;
    [Tooltip("Padding around food items inside the bubble")]
    public Vector2 bubblePadding = new Vector2(0.5f, 0.5f);
    [Tooltip("Minimum bubble size")]
    public Vector2 minBubbleSize = new Vector2(1.5f, 1f);
    
    // Private variables
    private Transform targetCustomer;
    private List<GameObject> currentFoodItems = new List<GameObject>();
    private bool isVisible = false;
    private Coroutine animationCoroutine;
    private Vector3 originalScale;
    
    // Cache OrderSystem reference and sync configurations
    private OrderSystem orderSystemCache;
    private float actualSpacing;
    private Vector2 actualItemScale;
    
    void Awake()
    {
        originalScale = transform.localScale;
        
        // Cache OrderSystem reference and sync configurations
        CacheOrderSystemAndSyncConfigs();
        
        // Auto-setup components if not assigned
        SetupBubbleComponents();
        
        // Start hidden
        gameObject.SetActive(false);
        isVisible = false;
    }
    
    void CacheOrderSystemAndSyncConfigs()
    {
        orderSystemCache = FindObjectOfType<OrderSystem>();
        if (orderSystemCache == null)
        {
            Debug.LogError("SpeechBubble: OrderSystem not found in scene!");
            actualSpacing = foodItemSpacing;
            actualItemScale = foodItemScale;
            return;
        }
        
        // Sync spacing configuration with OrderSystem if enabled
        if (autoSyncSpacing)
        {
            // Use OrderSystem's spacing but scaled down for the bubble
            actualSpacing = orderSystemCache.itemSpacing * spacingMultiplier;
            Debug.Log($"SpeechBubble: Synced spacing with OrderSystem: {orderSystemCache.itemSpacing} * {spacingMultiplier} = {actualSpacing}");
        }
        else
        {
            actualSpacing = foodItemSpacing;
        }
        
        actualItemScale = foodItemScale;
        
        Debug.Log($"SpeechBubble: Using spacing={actualSpacing}, scale={actualItemScale}");
    }
    
    void SetupBubbleComponents()
    {
        // Create bubble background if not assigned
        if (bubbleBackground == null)
        {
            GameObject bgObj = new GameObject("BubbleBackground");
            bgObj.transform.SetParent(transform);
            bgObj.transform.localPosition = Vector3.zero;
            
            bubbleBackground = bgObj.AddComponent<SpriteRenderer>();
            bubbleBackground.sprite = bubbleSprite;
            bubbleBackground.color = bubbleColor;
            bubbleBackground.sortingOrder = 10;
        }
        
        // Create bubble tail if not assigned
        if (bubbleTail == null)
        {
            GameObject tailObj = new GameObject("BubbleTail");
            tailObj.transform.SetParent(transform);
            tailObj.transform.localPosition = new Vector3(0, -0.8f, 0);
            
            bubbleTail = tailObj.AddComponent<SpriteRenderer>();
            bubbleTail.sprite = tailSprite;
            bubbleTail.color = bubbleColor;
            bubbleTail.sortingOrder = 10;
        }
        
        // Create food container if not assigned
        if (foodItemContainer == null)
        {
            GameObject containerObj = new GameObject("FoodContainer");
            containerObj.transform.SetParent(transform);
            containerObj.transform.localPosition = Vector3.zero;
            foodItemContainer = containerObj.transform;
        }
    }
    
    // Show bubble above specified customer with given food items
    public void ShowBubble(Transform customer, List<string> foodTypes, System.Action onComplete = null)
    {
        if (customer == null)
        {
            Debug.LogError("SpeechBubble: No customer specified!");
            return;
        }
        
        targetCustomer = customer;
        
        // Clear any existing items
        ClearFoodItems();
        
        // Create food item sprites
        CreateFoodItems(foodTypes);
        
        // Auto-resize bubble if enabled
        if (autoResizeBubble)
        {
            ResizeBubbleToFitContent();
        }
        
        // Position bubble above customer
        UpdateBubblePosition();
        
        // Show with animation
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateBubbleAppear(onComplete));
    }
    
    // Hide the bubble with animation
    public void HideBubble(System.Action onComplete = null)
    {
        if (!isVisible) return;
        
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateBubbleDisappear(onComplete));
    }
    
    // Update bubble position to follow customer
    void LateUpdate()
    {
        if (isVisible && targetCustomer != null)
        {
            UpdateBubblePosition();
        }
    }
    
    void UpdateBubblePosition()
    {
        if (targetCustomer == null) return;
        
        // Simply position in world space above customer
        transform.position = targetCustomer.position + bubbleOffset;
    }
    
    void CreateFoodItems(List<string> foodTypes)
    {
        if (foodItemContainer == null) return;
        
        for (int i = 0; i < foodTypes.Count; i++)
        {
            // Get the display prefab for this food type from cached OrderSystem
            GameObject prefab = GetFoodDisplayPrefab(foodTypes[i]);
            if (prefab == null) continue;
            
            // Instantiate food item as world object
            GameObject foodItem = Instantiate(prefab, foodItemContainer);
            
            // Position in grid layout using configured spacing
            Vector3 gridPos = CalculateGridPosition(i, foodTypes.Count);
            foodItem.transform.localPosition = gridPos;
            foodItem.transform.localScale = new Vector3(actualItemScale.x, actualItemScale.y, 1f);
            
            // Ensure proper sorting order for visibility
            SpriteRenderer sr = foodItem.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 15; // Above bubble background
            }
            
            // Add to tracking list
            currentFoodItems.Add(foodItem);
        }
    }
    
    Vector3 CalculateGridPosition(int index, int totalItems)
    {
        int row = index / maxItemsPerRow;
        int col = index % maxItemsPerRow;
        
        // Calculate how many items are in this row
        int itemsInThisRow = Mathf.Min(maxItemsPerRow, totalItems - (row * maxItemsPerRow));
        
        // Center each row individually for better visual balance
        float rowWidth = (itemsInThisRow - 1) * actualSpacing;
        float startX = -rowWidth * 0.5f;
        
        float x = startX + (col * actualSpacing);
        float y = -row * actualSpacing * 0.7f; // Slightly compressed vertically
        
        return new Vector3(x, y, 0);
    }
    
    void ResizeBubbleToFitContent()
    {
        if (bubbleBackground == null || currentFoodItems.Count == 0) return;
        
        // Calculate bounds of all food items
        Bounds contentBounds = new Bounds();
        bool boundsInitialized = false;
        
        foreach (GameObject item in currentFoodItems)
        {
            if (item != null)
            {
                Renderer renderer = item.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (!boundsInitialized)
                    {
                        contentBounds = renderer.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        contentBounds.Encapsulate(renderer.bounds);
                    }
                }
            }
        }
        
        if (boundsInitialized)
        {
            // Convert world bounds to local space
            Vector3 localMin = transform.InverseTransformPoint(contentBounds.min);
            Vector3 localMax = transform.InverseTransformPoint(contentBounds.max);
            
            // Calculate desired bubble size with padding
            float bubbleWidth = Mathf.Max(minBubbleSize.x, (localMax.x - localMin.x) + bubblePadding.x * 2);
            float bubbleHeight = Mathf.Max(minBubbleSize.y, (localMax.y - localMin.y) + bubblePadding.y * 2);
            
            // Apply size to bubble background
            if (bubbleBackground.sprite != null)
            {
                // Scale the bubble to fit content
                Vector2 spriteSize = bubbleBackground.sprite.bounds.size;
                bubbleBackground.transform.localScale = new Vector3(
                    bubbleWidth / spriteSize.x,
                    bubbleHeight / spriteSize.y,
                    1f
                );
            }
            
            Debug.Log($"SpeechBubble: Resized to {bubbleWidth:F2} x {bubbleHeight:F2} for {currentFoodItems.Count} items");
        }
    }
    
    GameObject GetFoodDisplayPrefab(string foodType)
    {
        // Use cached OrderSystem reference
        if (orderSystemCache == null)
        {
            Debug.LogError("SpeechBubble: OrderSystem reference is null!");
            return null;
        }
        
        foreach (var food in orderSystemCache.availableFoods)
        {
            if (food != null && food.foodType == foodType)
            {
                return food.displayPrefab;
            }
        }
        
        Debug.LogWarning($"SpeechBubble: No display prefab found for food type: {foodType}");
        return null;
    }
    
    void ClearFoodItems()
    {
        foreach (GameObject item in currentFoodItems)
        {
            if (item != null)
                Destroy(item);
        }
        currentFoodItems.Clear();
    }
    
    IEnumerator AnimateBubbleAppear(System.Action onComplete)
    {
        gameObject.SetActive(true);
        isVisible = true;
        
        // Start small and scale up
        transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / appearDuration;
            float scale = scaleCurve.Evaluate(progress);
            
            transform.localScale = originalScale * scale;
            
            yield return null;
        }
        
        transform.localScale = originalScale;
        animationCoroutine = null;
        
        onComplete?.Invoke();
    }
    
    IEnumerator AnimateBubbleDisappear(System.Action onComplete)
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / disappearDuration;
            float scale = Mathf.Lerp(1f, 0f, progress);
            
            transform.localScale = startScale * scale;
            
            yield return null;
        }
        
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
        isVisible = false;
        
        // Clear items when hidden
        ClearFoodItems();
        targetCustomer = null;
        
        animationCoroutine = null;
        onComplete?.Invoke();
    }
    
    // Mark a food item as served (for visual feedback)
    public void MarkItemServed(string foodType)
    {
        // Find the food item and apply visual effect (fade out, checkmark, etc.)
        for (int i = 0; i < currentFoodItems.Count; i++)
        {
            GameObject item = currentFoodItems[i];
            if (item != null && item.name.Contains(foodType))
            {
                // Simple fade effect when served
                StartCoroutine(FadeOutItem(item));
                break;
            }
        }
    }
    
    IEnumerator FadeOutItem(GameObject item)
    {
        SpriteRenderer sr = item.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        
        Color originalColor = sr.color;
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0.3f, elapsed / duration);
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
    }
    
    // Check if bubble is currently visible
    public bool IsVisible()
    {
        return isVisible;
    }
    
    // Get current customer this bubble is following
    public Transform GetTargetCustomer()
    {
        return targetCustomer;
    }
    
    // CONFIGURATION SYNC METHODS
    
    /// <summary>
    /// Manually sync spacing configuration with OrderSystem
    /// Call this if OrderSystem spacing changes at runtime
    /// </summary>
    public void SyncSpacingWithOrderSystem()
    {
        if (orderSystemCache != null && autoSyncSpacing)
        {
            actualSpacing = orderSystemCache.itemSpacing * spacingMultiplier;
            Debug.Log($"SpeechBubble: Re-synced spacing: {actualSpacing}");
        }
    }
    
    /// <summary>
    /// Override spacing manually (disables auto-sync)
    /// </summary>
    public void SetCustomSpacing(float spacing)
    {
        actualSpacing = spacing;
        autoSyncSpacing = false;
        Debug.Log($"SpeechBubble: Custom spacing set to {spacing} (auto-sync disabled)");
    }
    
    /// <summary>
    /// Get current effective spacing being used
    /// </summary>
    public float GetCurrentSpacing()
    {
        return actualSpacing;
    }
    
    // INTEGRATION METHODS for your existing systems
    
    /// <summary>
    /// Show bubble for current customer with their order
    /// Call this from CustomerManager when customer reaches service point
    /// </summary>
    public void ShowCurrentCustomerOrder()
    {
        if (orderSystemCache == null)
        {
            Debug.LogError("SpeechBubble: Cannot show customer order - OrderSystem not found!");
            return;
        }
        
        // Get current customer from CustomerManager
        CustomerManager customerManager = FindObjectOfType<CustomerManager>();
        if (customerManager == null)
        {
            Debug.LogError("SpeechBubble: CustomerManager not found!");
            return;
        }
        
        CustomerController currentCustomer = customerManager.GetCurrentCustomer();
        if (currentCustomer == null)
        {
            Debug.LogWarning("SpeechBubble: No current customer to show order for");
            return;
        }
        
        // Get current order from OrderSystem
        List<string> currentOrder = orderSystemCache.GetCurrentOrderTypes();
        if (currentOrder == null || currentOrder.Count == 0)
        {
            Debug.LogWarning("SpeechBubble: No current order to display");
            return;
        }
        
        // Show bubble above customer
        ShowBubble(currentCustomer.transform, currentOrder);
    }
    
    /// <summary>
    /// Hide bubble when order is completed or customer leaves
    /// </summary>
    public void HideCurrentCustomerOrder()
    {
        HideBubble();
    }
    
    // Context menu for testing
    [ContextMenu("Test Show Bubble")]
    void TestShowBubble()
    {
        if (Application.isPlaying)
        {
            List<string> testFoods = new List<string> { "Bread", "Green" };
            Transform testTarget = transform; // Use self as target for testing
            ShowBubble(testTarget, testFoods);
        }
    }
    
    [ContextMenu("Test Hide Bubble")]
    void TestHideBubble()
    {
        if (Application.isPlaying)
        {
            HideBubble();
        }
    }
    
    [ContextMenu("Test Show Current Customer Order")]
    void TestShowCurrentCustomerOrder()
    {
        if (Application.isPlaying)
        {
            ShowCurrentCustomerOrder();
        }
    }
    
    [ContextMenu("Sync Spacing with OrderSystem")]
    void TestSyncSpacing()
    {
        SyncSpacingWithOrderSystem();
    }
}