using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ServePlate : MonoBehaviour
{
    [Header("Plate Settings")]
    public int maxCapacity = 4; // Maximum items on plate
    public float itemSpacing = 1.5f; // Space between items on plate
    
    [Header("Visual Elements")]
    public Transform itemContainer; // Parent object to hold served items
    public Button serveButton; // Button to serve/clear the plate
    public Text plateCountText; // Text to display current item count
    
    [Header("Layout Settings")]
    public Vector3 startPosition = Vector3.zero; // Starting position for first item
    
    [Header("Score System")]
    public ScoreManager scoreManager; // Reference to score manager
    
    // Private variables
    private List<GameObject> servedItems = new List<GameObject>();
    private List<string> servedItemTypes = new List<string>();
    
    void Start()
    {
        InitializePlate();
        
        // Set up serve button click event
        if (serveButton != null)
        {
            serveButton.onClick.AddListener(Serve);
        }
    }
    
    void InitializePlate()
    {
        servedItems.Clear();
        servedItemTypes.Clear();
        UpdateUI();
    }
    
    public bool AddItem(GameObject itemPrefab, string itemType)
    {
        // Check if plate has room
        if (servedItems.Count >= maxCapacity)
        {
        
            return false;
        }
        
        // Create item on the serve plate
        Vector3 itemPosition = CalculateItemPosition(servedItems.Count);
        GameObject newItem = Instantiate(itemPrefab, itemContainer);
        newItem.transform.localPosition = itemPosition;
        
        // Add served item script for individual clicking
        ServedItem servedItemScript = newItem.GetComponent<ServedItem>();
        if (servedItemScript == null)
            servedItemScript = newItem.AddComponent<ServedItem>();
        
        servedItemScript.Initialize(this, itemType, servedItems.Count);
        
        // Add to our tracking lists
        servedItems.Add(newItem);
        servedItemTypes.Add(itemType);
        
        UpdateUI();
        
      
        return true;
    }
    
    Vector3 CalculateItemPosition(int index)
    {
        // Arrange items left to right in a single row
        float x = startPosition.x + (index * itemSpacing);
        return new Vector3(x, startPosition.y, startPosition.z);
    }
    
    public void RemoveItem(int itemIndex)
    {
        if (itemIndex >= 0 && itemIndex < servedItems.Count)
        {
            // Destroy the item
            if (servedItems[itemIndex] != null)
            {
                Destroy(servedItems[itemIndex]);
            }
            
            // Remove from lists
            servedItems.RemoveAt(itemIndex);
            servedItemTypes.RemoveAt(itemIndex);
            
            // Rearrange remaining items
            RearrangeItems();
            
            UpdateUI();
            
         
        }
    }
    
    void RearrangeItems()
    {
        // Reposition all remaining items and update their indices
        for (int i = 0; i < servedItems.Count; i++)
        {
            if (servedItems[i] != null)
            {
                // Update position
                servedItems[i].transform.localPosition = CalculateItemPosition(i);
                
                // Update the served item script with new index
                ServedItem servedItemScript = servedItems[i].GetComponent<ServedItem>();
                if (servedItemScript != null)
                {
                    servedItemScript.UpdateIndex(i);
                }
            }
        }
    }
    
    public void Serve()
    {
        // Calculate score BEFORE clearing the plate
        if (scoreManager != null)
        {
            scoreManager.CalculateAndAwardScore();
        }
    
        // Complete the current order (remove it from display)
        if (scoreManager != null && scoreManager.orderSystem != null)
        {
            scoreManager.orderSystem.CompleteCurrentOrder();
        }
    
        // Clear all items from the plate
        foreach (GameObject item in servedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
    
        servedItems.Clear();
        servedItemTypes.Clear();
    
        UpdateUI();
    
      
    
        OnPlateServed();
    }
    
    void OnPlateServed()
    {
        // Override this method or add events here for when plate is served
        // For example: update score, check if order matches customer request, etc.
    }
    
    void UpdateUI()
    {
        if (plateCountText != null)
            plateCountText.text = servedItems.Count + "/" + maxCapacity;
    }
    
    // Public methods for checking plate status
    public bool IsFull()
    {
        return servedItems.Count >= maxCapacity;
    }
    
    public bool IsEmpty()
    {
        return servedItems.Count == 0;
    }
    
    public int GetItemCount()
    {
        return servedItems.Count;
    }
    
    public List<string> GetServedItemTypes()
    {
        return new List<string>(servedItemTypes); // Return a copy
    }
}

// Script for individual items on the serve plate
public class ServedItem : MonoBehaviour
{
    private ServePlate parentPlate;
    private string itemType;
    private int itemIndex;
    
    public void Initialize(ServePlate plate, string type, int index)
    {
        parentPlate = plate;
        itemType = type;
        itemIndex = index;
        
        // Add collider for clicking
        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();
    }
    
    public void UpdateIndex(int newIndex)
    {
        itemIndex = newIndex;
    }
    
    void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (parentPlate != null)
        {
            parentPlate.RemoveItem(itemIndex);
        }
    }
    
    // Alternative method using UI Button (if items are UI elements)
    public void OnButtonClick()
    {
        if (parentPlate != null)
        {
            parentPlate.RemoveItem(itemIndex);
        }
    }
}