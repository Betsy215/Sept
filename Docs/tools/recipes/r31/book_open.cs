var b = GameObject.Find("CookieBookButton"); if (b == null) return "no book button"; b.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "book open";
