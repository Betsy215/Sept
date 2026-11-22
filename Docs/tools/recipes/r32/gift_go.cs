var g = GameObject.Find("FlowerGift"); if (g == null) return "no card";
foreach (var b in g.GetComponentsInChildren<UnityEngine.UI.Button>()) b.onClick.Invoke();
return "tapped, gift due now " + SessionManager.Instance.Chapter2GiftDue;
