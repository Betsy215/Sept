var b = GameObject.Find("CookieBook"); if (b == null) return "no book";
foreach (var x in b.GetComponentsInChildren<UnityEngine.UI.Button>(true)) if (x.name == "PrevPage" && x.gameObject.activeInHierarchy) { x.onClick.Invoke(); return "previous page"; }
return "no next";
