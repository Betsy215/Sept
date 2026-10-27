// fills the jar (nine, CandyJar.Full) and refreshes the shop cards: the Candies card must grey out and refuse taps
var sm = SessionManager.Instance; if (sm == null) return "no session";
while (sm.BuyCandies()) { }
var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop != null) shop.RefreshScoreDisplay();
var s = "candies " + sm.Candies + " of " + CandyJar.Full;
foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>())
    if (c.itemName == "Candies") { var b = c.transform.Find("ItemIcon")?.GetComponent<UnityEngine.UI.Button>(); s += " | card tappable " + (b != null && b.interactable); }
return s;
