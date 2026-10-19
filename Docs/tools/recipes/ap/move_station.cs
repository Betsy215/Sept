// in the arrangement: drag the fixed station handle to the table's upper right by code, then report
var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); var tl = UnityEngine.Object.FindObjectOfType<TableLayer>();
Transform handle = null; foreach (var it in lm.serveableItems) if (it != null && it.gameObject.activeInHierarchy && FixedStation.IsHandle(it)) handle = it.transform;
var cm = UnityEngine.Object.FindObjectOfType<CoffeeMachine>(); if (cm != null && cm.gameObject.activeInHierarchy) handle = cm.transform;
if (handle == null) return "no handle";
var b = tl.InteractiveBounds; var target = new Vector3(b.max.x - 1.2f, b.max.y - 0.9f, handle.position.z);
var d = handle.GetComponent<DraggableFood>(); if (d != null) d.SetRestPosition(target); else handle.position = target;
return "handle " + handle.name + " moved to " + target.ToString("F2");
