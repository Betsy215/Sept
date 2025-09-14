using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum GamePhase
{
    ARRANGEMENT,  // Player can drag items around
    PLAYING       // Normal gameplay - serving customers
}

public class GamePhaseManager : MonoBehaviour
{
    [Header("UI References")]
    public Button doneButton;
    
    [Header("Game References")]
    public LevelManager levelManager;
    public OrderSystem orderSystem;
    public CustomerManager customerManager;
    public TableLayer tableLayer;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Private variables
    private GamePhase currentPhase = GamePhase.ARRANGEMENT;
    private ServeableItem[] allFoodItems;
    
    void Start()
    {
        InitializeGamePhase();
    }
    
    void InitializeGamePhase()
    {
        // Get all food items from LevelManager
        if (levelManager != null)
        {
            allFoodItems = levelManager.serveableItems;
        }
        else
        {
            allFoodItems = FindObjectsOfType<ServeableItem>();
        }
        
      
        doneButton.onClick.AddListener(OnDoneButtonClicked);
        
        // Start in arrangement phase
        StartArrangementPhase();
        
        DebugLog("GamePhaseManager initialized");
    }
    
    public void StartArrangementPhase()
    {
        currentPhase = GamePhase.ARRANGEMENT;
        DebugLog("=== ARRANGEMENT PHASE STARTED ===");
        
        // Enable dragging on all food items
        EnableArrangementMode();
        
        // Disable gameplay systems
        DisableGameplaySystems();
        
        // Show done button
        if (doneButton != null)
        {
            doneButton.gameObject.SetActive(true);
        }
    }
    
    public void StartPlayPhase()
    {
        currentPhase = GamePhase.PLAYING;
        DebugLog("=== PLAY PHASE STARTED ===");
        
        // Disable dragging
        DisableArrangementMode();
        
        // Enable gameplay systems
        EnableGameplaySystems();
        
        // Hide done button
        if (doneButton != null)
        {
            doneButton.gameObject.SetActive(false);
        }
    }
    
    void EnableArrangementMode()
    {
        foreach (ServeableItem item in allFoodItems)
        {
            if (item != null && item.gameObject.activeInHierarchy)
            {
                // Add draggable component
                DraggableFood draggable = item.GetComponent<DraggableFood>();
                if (draggable == null)
                {
                    draggable = item.gameObject.AddComponent<DraggableFood>();
                }
            
                draggable.Initialize(tableLayer, this, allFoodItems);
                draggable.SetDraggingEnabled(true);
            
                // Disable serving during arrangement
                item.SetServingEnabled(false);
            
                DebugLog($"Enabled arrangement mode for {item.GetFoodType()}");
            }
        }
    }
    
    void DisableArrangementMode()
    {
        foreach (ServeableItem item in allFoodItems)
        {
            if (item != null)
            {
                // Disable dragging
                DraggableFood draggable = item.GetComponent<DraggableFood>();
                if (draggable != null)
                {
                    draggable.SetDraggingEnabled(false);
                }
            
                // Re-enable serving
                item.SetServingEnabled(true);
            }
        }
    }
    
    void DisableGameplaySystems()
    {
        if (orderSystem != null)
        {
            orderSystem.enabled = false;
        }
        
        if (customerManager != null)
        {
            customerManager.enabled = false;
        }
    }
    
    void EnableGameplaySystems()
    {
        if (orderSystem != null)
        {
            orderSystem.enabled = true;
        }
        
        if (customerManager != null)
        {
            customerManager.enabled = true;
        }
    }
    
    public void OnDoneButtonClicked()
    {
        DebugLog("Done button clicked - transitioning to play phase");
        StartPlayPhase();
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"GamePhaseManager: {message}");
        }
    }
}