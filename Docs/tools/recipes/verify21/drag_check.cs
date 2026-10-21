var sm = SessionManager.Instance; var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no LM";
string why; var ok = sm.SetFoodEnabled("Choux", true);
lm.ReapplyServeableItems();
ServeableItem choux = null, coco = null;
foreach (var it in UnityEngine.Object.FindObjectsOfType<ServeableItem>(true)) { if (it.GetFoodType() == "Choux") choux = it; if (it.GetFoodType() == "Coconut") coco = it; }
if (choux == null) return "no Choux item";
var d = choux.GetComponent<DraggableFood>();
var f = typeof(DraggableFood).GetField("isDraggingEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var en = d != null && (bool)f.GetValue(d);
var cocoOn = sm.IsFoodItemActive("Coconut"); var cocoCan = sm.CanEnableFood("Coconut", out why);
return "V|drag|chouxOn=" + ok + "|active=" + choux.gameObject.activeInHierarchy + "|draggable=" + en + "|coconutActive=" + cocoOn + "|coconutOnTable=" + (coco != null && coco.gameObject.activeInHierarchy) + "|coconutCanEnable=" + cocoCan + "(" + why + ")";
