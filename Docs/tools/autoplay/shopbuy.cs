// buy the first affordable item in the natural order through the real shop popup
var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop == null) return "B|noshop";
var order = new[]{"Juice","Grandma","Chef","Choux","Cake","Yoga","Businessman","CoffeeMachine","Coffee","Apple"};
var cards = UnityEngine.Object.FindObjectsOfType<ShopItemController>();
var sm = SessionManager.Instance;
foreach (var name in order) foreach (var c in cards) {
    var key = c.itemType == ItemType.Upgrade ? c.upgradeFoodType : c.itemName;
    if (key != name || c.isPurchased || !c.gameObject.activeInHierarchy) continue;
    if (!sm.CanAfford(c.price)) return "B|cannot afford " + name + " " + c.price + " have " + sm.GetTotalScore();
    var before = sm.GetTotalScore(); shop.ShowPurchasePopup(c); shop.ConfirmPurchase();
    return "B|bought " + name + " " + c.price + " " + before + " -> " + sm.GetTotalScore();
}
return "B|nothing left";
