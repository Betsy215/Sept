using UnityEngine;

public class Coffees : MonoBehaviour
{
    [Header("Coffee Display")] [SerializeField]
    private SpriteRenderer[] slots; // Slot1, Slot2, Slot3

    [SerializeField] private Sprite plateSprite;
    [SerializeField] private Sprite cupSprite;

    private RefillableItem refillableItem;

    private void Start()
    {
        // Get RefillableItem component
        refillableItem = GetComponent<RefillableItem>();

        if (refillableItem != null)
        {
            // Subscribe to count change events
            refillableItem.OnCountChanged += HandleCountChanged;

            // Initial visual update
            HandleCountChanged(refillableItem.GetCurrentCount(), refillableItem.GetMaxCount());
        }

        // Auto-find slots if not assigned
        if (slots == null || slots.Length == 0) slots = GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (refillableItem != null) refillableItem.OnCountChanged -= HandleCountChanged;
    }

    // Event handler - called whenever RefillableItem count changes
    private void HandleCountChanged(int currentCount, int maxCount)
    {
        UpdateCoffeeVisuals(currentCount, maxCount);
        Debug.Log($"Coffee count changed: {currentCount}/{maxCount}");
    }

    private void UpdateCoffeeVisuals(int currentCount, int maxCount)
    {
        // Show active slots based on maxCount
        for (var i = 0; i < maxCount && i < slots.Length; i++)
            if (slots[i] != null)
            {
                // Show cup if we have coffee in this slot, otherwise show plate
                var hasCoffee = i < currentCount;
                slots[i].sprite = hasCoffee ? cupSprite : plateSprite;
                slots[i].gameObject.SetActive(true);
            }

        // Hide unused slots beyond maxCount
        for (var i = maxCount; i < slots.Length; i++)
            if (slots[i] != null)
                slots[i].gameObject.SetActive(false);
    }
}