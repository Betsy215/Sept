var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); var sb = new System.Text.StringBuilder();
foreach (var it in lm.serveableItems) if (it != null && it.gameObject.activeInHierarchy && (it.GetFoodType() == "IceCream" || it.GetFoodType() == "Coffee")) sb.Append(it.GetFoodType()).Append(' ').Append(it.transform.position.ToString("F2")).Append(" | ");
var cm = UnityEngine.Object.FindObjectOfType<CoffeeMachine>(); if (cm != null && cm.gameObject.activeInHierarchy) sb.Append("machine ").Append(cm.transform.position.ToString("F2")).Append(" | ");
var tray = GameObject.Find("FixedStation"); if (tray != null) sb.Append("tray ").Append(tray.transform.position.ToString("F2"));
var h = GameObject.Find("Holder"); if (h != null) sb.Append(" | holder ").Append(h.transform.position.ToString("F2"));
return sb.ToString();
