using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemController : MonoBehaviour
{
    [Header("Item Info")] public string itemName;
    public int price;
    public ItemType itemType;

    [Header("Upgrade")]
    [Tooltip("SessionManager key to upgrade (e.g. 'Coffee', 'CoffeeMachine'). Only used when ItemType = Upgrade.")]
    public string upgradeFoodType;

    [Header("Popup Display")] [TextArea(2, 4)]
    public string popupInfoText = "Fresh bread attracts more customers!";

    public string purchaseButtonText = "Buy for 50 Points";

    [Tooltip("A short special note under the description, in the popup's second colour (owner, 16 October); empty for none")] [TextArea(1, 3)]
    public string specialNote = "";

    [Header("State")] public bool isPurchased = false;

    private ShopManager shopManager;

    private void Awake()
    {
        // These are matched by exact string against SessionManager keys ("Bread", "CoffeeMachine");
        // a stray space typed in the inspector would silently make the purchase never register.
        itemName = itemName?.Trim();
        upgradeFoodType = upgradeFoodType?.Trim();
    }

    private void Start()
    {
        shopManager = FindObjectOfType<ShopManager>();
        SetupClickHandler();

        // FIX: Start with unaffordable state instead of affordable
        UpdateVisualState(false); // ← Changed from true to false
    }

    private void SetupClickHandler()
    {
        var iconButton = transform.Find("ItemIcon")?.GetComponent<Button>();
        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(OnItemClicked);
        }
    }

    private void OnItemClicked()
    {
        if (SessionManager.IsLockedInShop(itemName) || SessionManager.IsLockedInShop(gameObject.name)) return;   // locked until its chapter (owner, 16 October)
        if (shopManager != null && !isPurchased) shopManager.ShowPurchasePopup(this);
    }

    public void MarkAsPurchased()
    {
        isPurchased = true;
        UpdateVisualState(true);
    }

    // New simplified method - only takes canAfford parameter
    public void UpdateAffordability(bool canAfford)
    {
        UpdateVisualState(canAfford);
    }

    // The picture's look is set here alone. The Button on the picture also tints it by its own state (Unity's colour
    // tint: grey and half see-through when it cannot be tapped), and that second tint is not updated while the card is
    // hidden, so it could stay "disabled" on a card that had become affordable: an affordable card looked dimmed
    // (owner, 15 November 2026: "it is affordable but showing a dimmed version"; seen the other way round on another
    // card in the editor). The disabled tint is now neutral and reset on every change; the three looks below carry
    // what the tint used to add, so nothing looks different when all is in step.
    private static readonly Color OwnedLook = new Color(0f, 0.78f, 0f, 0.5f);       // was green under the grey half tint
    private static readonly Color CannotAffordLook = new Color(0f, 0f, 0f, 0.4f);   // was black 0.8 under the grey half tint (not used since 19 November)
    private static readonly Color CannotAffordPrice = new Color(0.86f, 0.14f, 0.12f);   // the price of a card that cannot be bought yet
    private Color priceColour; private bool priceColourKept;
    private static Color normalPrice; private static bool normalPriceKnown;

    private void UpdateVisualState(bool canAfford)
    {
        var iconButton = transform.Find("ItemIcon")?.GetComponent<Button>();
        var iconImage = iconButton?.GetComponent<Image>();
        if (iconButton != null)
        {
            var colours = iconButton.colors;
            if (colours.disabledColor != Color.white) { colours.disabledColor = Color.white; iconButton.colors = colours; }
        }

        if (isPurchased)
        {
            // Item is purchased - show as owned (green and non-clickable)
            if (iconButton != null)
                iconButton.interactable = false;
            if (iconImage != null)
                iconImage.color = OwnedLook;
        }
        else if (canAfford)
        {
            // Item is affordable - show as available (white and clickable)
            if (iconButton != null)
                iconButton.interactable = true;
            if (iconImage != null)
                iconImage.color = Color.white;
        }
        else
        {
            // Not affordable: the same picture as every card, not to be opened, and its price in red (owner, 19 November
            // 2026: "for all shop items, even not affordable, show the icons the same way, but unaffordable items are
            // not openable when clicked. and price show a red color"; it was a dark see-through shape)
            if (iconButton != null)
                iconButton.interactable = false;
            if (iconImage != null)
                iconImage.color = Color.white;
        }
        var priceLabel = transform.Find("PriceText") != null ? transform.Find("PriceText").GetComponent<TMPro.TextMeshProUGUI>() : null;
        if (priceLabel != null)
        {
            // the price's own colour, kept the first time; a card cloned from one that was red at that moment (the star
            // cards are cloned from the Candies card) takes the colour the cards really have
            if (!priceColourKept)
            {
                if (priceLabel.color != CannotAffordPrice) { priceColour = priceLabel.color; if (!normalPriceKnown) { normalPrice = priceColour; normalPriceKnown = true; } }
                else priceColour = normalPriceKnown ? normalPrice : Color.black;
                priceColourKept = true;
            }
            priceLabel.color = !isPurchased && !canAfford ? CannotAffordPrice : priceColour;
        }
        if (iconImage != null) iconImage.canvasRenderer.SetColor(Color.white);   // the button's own tint back to neutral, hidden or not
    }
}