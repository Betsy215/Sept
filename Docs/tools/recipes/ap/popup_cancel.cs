var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop == null) return "no shop"; shop.CancelPurchase(); return "popup closed";
