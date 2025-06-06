using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class OrderItem
{
    public string foodType;
    public GameObject displayPrefab; // Prefab to show in the order display
}

public class OrderSystem : MonoBehaviour
{
    [Header("Order Settings")]
    public float orderDisplayTime = 5f; // How long each order is shown
    public float timeBetweenOrders = 2f; // Time between orders
    public int minOrderItems = 1; // Minimum items in an order
    public int maxOrderItems = 4; // Maximum items in an order
    
    [Header("Order Display UI")]
    public Transform orderContainer; // Parent object to hold order items
    public Text orderTitleText; // Text showing "Order:" or similar
    public Text orderTimerText; // Text showing remaining time
    public GameObject orderPanel; // Panel containing the entire order display
    
    [Header("Available Food Items")]
    public OrderItem[] availableFoods = new OrderItem[4]; // Burger, Fries, Drink, Dessert
    
    [Header("Layout Settings")]
    public float itemSpacing = 1.5f; // Space between order items
    public Vector3 startPosition = Vector3.zero; // Starting position for first item
    
    [Header("Serve Plate Reference")]
    public ServePlate servePlate; // Reference to check served items
    
    // Private variables
    private List<string> currentOrder = new List<string>();
    private List<GameObject> orderDisplayItems = new List<GameObject>();
    private bool orderActive = false;
    private float orderTimer = 0f;
    private Coroutine orderCoroutine;
    
    void Start()
    {
        InitializeOrderSystem();
    }
    
    void InitializeOrderSystem()
    {
        // Hide order panel initially
        if (orderPanel != null)
            orderPanel.SetActive(false);
            
        // Start the order cycle
        StartOrderCycle();
    }
    
    void StartOrderCycle()
    {
        if (orderCoroutine != null)
            StopCoroutine(orderCoroutine);
            
        orderCoroutine = StartCoroutine(OrderCycleCoroutine());
    }
    
    IEnumerator OrderCycleCoroutine()
    {
        while (true)
        {
            // Generate and display new order
            GenerateNewOrder();
            DisplayOrder();
            
            // Wait for order duration
            orderTimer = orderDisplayTime;
            orderActive = true;
            
            // Update timer every frame
            while (orderTimer > 0 && orderActive)
            {
                orderTimer -= Time.deltaTime;
                UpdateTimerDisplay();
                
                // Check if order was served (you can implement this check)
                if (CheckIfOrderServed())
                {
                    OnOrderServed();
                  
                    break;
                }
                
                yield return null;
            }
            
            // Order expired or was served
            if (orderActive)
            {
                OnOrderExpired();
            }
            
            // Hide order and wait before next one
            HideOrder();
            yield return new WaitForSeconds(timeBetweenOrders);
        }
    }
    
    void GenerateNewOrder()
    {
        currentOrder.Clear();
        
        // Random number of items (1-4)
        int orderSize = Random.Range(minOrderItems, maxOrderItems + 1);
        
        // Generate random food items
        for (int i = 0; i < orderSize; i++)
        {
            int randomFoodIndex = Random.Range(0, availableFoods.Length);
            string foodType = availableFoods[randomFoodIndex].foodType;
            currentOrder.Add(foodType);
        }
        
        Debug.Log($"Generated order: {string.Join(", ", currentOrder)}");
    }
    
    void DisplayOrder()
    {
        // Clear previous order display
        ClearOrderDisplay();
        
        // Show order panel
        if (orderPanel != null)
            orderPanel.SetActive(true);
        
        // Update order title
        if (orderTitleText != null)
            orderTitleText.text = "Order:";
        
        // Create visual items for the order
        for (int i = 0; i < currentOrder.Count; i++)
        {
            CreateOrderDisplayItem(currentOrder[i], i);
        }
        
        orderActive = true;
        orderTimer = orderDisplayTime;
    }
    
    void CreateOrderDisplayItem(string foodType, int index)
    {
        // Find the matching food item
        OrderItem orderItem = System.Array.Find(availableFoods, item => item.foodType == foodType);
        
        if (orderItem != null && orderItem.displayPrefab != null)
        {
            // Calculate position for this item
            Vector3 itemPosition = CalculateOrderItemPosition(index);
            
            // Create the display item
            GameObject displayItem = Instantiate(orderItem.displayPrefab, orderContainer);
            displayItem.transform.localPosition = itemPosition;
            
            // Add to tracking list
            orderDisplayItems.Add(displayItem);
        }
        else
        {
            Debug.LogWarning($"No display prefab found for food type: {foodType}");
        }
    }
    
    Vector3 CalculateOrderItemPosition(int index)
    {
        // Arrange items left to right, similar to serve plate
        float x = startPosition.x + (index * itemSpacing);
        return new Vector3(x, startPosition.y, startPosition.z);
    }
    
    void ClearOrderDisplay()
    {
        // Destroy all current order display items
        foreach (GameObject item in orderDisplayItems)
        {
            if (item != null)
                Destroy(item);
        }
        orderDisplayItems.Clear();
    }
    
    void UpdateTimerDisplay()
    {
        if (orderTimerText != null)
        {
            int seconds = Mathf.CeilToInt(orderTimer);
            orderTimerText.text = $"Time: {seconds}s";
        }
    }
    
    bool CheckIfOrderServed()
    {
        // This will be expanded later when we integrate with scoring
        // For now, just return false
        return false;
    }
    
    void OnOrderServed()
    {
        Debug.Log("Order served successfully!");
        orderActive = false;
        // Scoring logic will go here later
    }
    
    void OnOrderExpired()
    {
        Debug.Log("Order expired!");
        orderActive = false;
        // Penalty logic could go here later
    }
    
    void HideOrder()
    {
        // Hide order panel
        if (orderPanel != null)
            orderPanel.SetActive(false);
        
        // Clear display items
        ClearOrderDisplay();
        
        orderActive = false;
    }
    
    // Public methods for external access
    public List<string> GetCurrentOrder()
    {
        return new List<string>(currentOrder); // Return a copy
    }
    
    public bool IsOrderActive()
    {
        return orderActive;
    }
    
    public float GetRemainingTime()
    {
        return orderTimer;
    }
    
    // Method to manually complete an order (for testing or serve button integration)
    public void CompleteOrder()
    {
        if (orderActive)
        {
            OnOrderServed();
        }
    }
    
    // Method to stop the order system
    public void StopOrderSystem()
    {
        if (orderCoroutine != null)
        {
            StopCoroutine(orderCoroutine);
            orderCoroutine = null;
        }
        HideOrder();
    }
}