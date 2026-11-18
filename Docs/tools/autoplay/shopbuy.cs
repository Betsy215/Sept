// buy the first affordable item in the natural order through the real shop popup
var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop == null) return "B|noshop";
var order = new[]{"Juice","Grandma","Chef","Choux","Cake","Yoga","Businessman","CoffeeMachine","Coffee","Apple","Cookies","Coconut","PotFlowers","PotGreen"};
var cards = UnityEngine.Object.FindObjectsOfType<ShopItemController>();
var sm = SessionManager.Instance;
foreach (var name in order) foreach (var c in cards) {
    var key = c.itemType == ItemType.Upgrade ? c.upgradeFoodType : c.itemName;
    if (key != name || c.isPurchased || !c.gameObject.activeInHierarchy) continue;
    if (SessionManager.IsLockedInShop(c.itemName) || SessionManager.IsLockedInShop(c.gameObject.name)) continue;   // a chapter 2 card in chapter 1: the shop refuses it
    if (!sm.CanAfford(c.price)) return "B|cannot afford " + name + " " + c.price + " have " + sm.GetTotalScore();
    var before = sm.GetTotalScore(); shop.ShowPurchasePopup(c); shop.ConfirmPurchase();
    if (sm.GetTotalScore() >= before) { shop.CancelPurchase(); return "B|refused " + name + " " + c.price + " have " + before; }   // nothing was taken: the shop said no
    return "B|bought " + name + " " + c.price + " " + before + " -> " + sm.GetTotalScore();
}
return "B|nothing left";
