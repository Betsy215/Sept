var sh = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (sh == null) return "no shop"; sh.LoadNextGameLevel(); return "shop: next";
