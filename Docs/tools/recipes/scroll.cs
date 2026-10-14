var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop == null || shop.itemContainer == null) return "no shop";
var rt = shop.itemContainer.GetComponent<RectTransform>();
var p = rt.anchoredPosition; p.y = SCROLL; rt.anchoredPosition = p; return "scrolled to " + SCROLL;
