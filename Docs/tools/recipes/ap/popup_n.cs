var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop == null) return "no shop";
var cards = new System.Collections.Generic.List<ShopItemController>(UnityEngine.Object.FindObjectsOfType<ShopItemController>(true));
cards.RemoveAll(c => !c.gameObject.activeInHierarchy); cards.Sort((a, b) => a.price != b.price ? a.price.CompareTo(b.price) : string.CompareOrdinal(a.itemName, b.itemName));
if (INDEX >= cards.Count) return "END " + cards.Count;
shop.CancelPurchase(); shop.ShowPurchasePopup(cards[INDEX]); return "popup " + INDEX + " " + cards[INDEX].itemName + " of " + cards.Count;
