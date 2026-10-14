var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop == null) return "no shop";
var m = typeof(ShopManager).GetMethod("UpdateScrollButtons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
m.Invoke(shop, null); return "scroll buttons updated";
