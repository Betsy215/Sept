using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Items")]
    public List<ShopItem> availableItems = new List<ShopItem>();
    
    [Header("UI References")]
    public GameObject shopPanel;           // The white sliding panel
    public GameObject itemContainer;       // Parent for shop item UI elements
    public GameObject shopItemPrefab;      // Prefab for individual shop items
    
    [Header("Shop Controls")]
    public Button nextLevelButton;
    public Button mainMenuButton;
    public TextMeshProUGUI playerScoreText; 
    [Header("Animation")]
    public float slideAnimationDuration = 1f;
    
    // Internal references
    private int playerScore;
    private const string SHOP_SAVE_KEY = "ShopData";
    
    void Start()
    {
        InitializeShop();
    }
    
    void InitializeShop()
    {
        // Get player's current score from SessionManager
        if (SessionManager.Instance != null)
        {
            playerScore = SessionManager.Instance.GetTotalScore();
        }
    
        // Update score display
        UpdateScoreDisplay();
    
        // Load previously purchased items
        LoadShopData();
    
        // Create the shop items (if using script)
        // CreateShopItems();
    
        // Start the slide-in animation
        StartShopAnimation();
    }
    
    void UpdateScoreDisplay()
    {
        if (playerScoreText != null)
        {
            playerScoreText.text = $" {playerScore}";
        }
    }
    
    void LoadShopData()
    {
        // TODO: Load purchased items from PlayerPrefs
    }
    
    void CreateShopItems()
    {
        // TODO: Instantiate UI elements for each shop item
    }
    
    void StartShopAnimation()
    {
        // Get the ShopPanel RectTransform
        RectTransform panelRect = shopPanel.GetComponent<RectTransform>();
    
        // Start position (off-screen left)
        Vector2 startPos = new Vector2(-1284, 0);
        Vector2 endPos = new Vector2(0, 0); // Center position
    
        // Start the slide animation
        StartCoroutine(SlidePanel(panelRect, startPos, endPos, slideAnimationDuration));
    }
    
    IEnumerator SlidePanel(RectTransform panel, Vector2 start, Vector2 end, float duration)
    {
        float elapsed = 0f;
    
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
        
            // Smooth easing
            t = Mathf.SmoothStep(0f, 1f, t);
        
            // Lerp position
            panel.anchoredPosition = Vector2.Lerp(start, end, t);
        
            yield return null;
        }
    
        // Ensure final position
        panel.anchoredPosition = end;
    }
    
    public void PurchaseItem(ShopItem item)
    {
        // TODO: Handle item purchase logic
    }
    
    public void OnNextLevelClicked()
    {
        // TODO: Save shop data and load next level
    }
    
    public void OnMainMenuClicked()
    {
        // TODO: Save shop data and return to main menu
    }
}