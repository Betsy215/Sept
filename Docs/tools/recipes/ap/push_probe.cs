var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); var tl = UnityEngine.Object.FindObjectOfType<TableLayer>();
var sb = new System.Text.StringBuilder(); sb.Append("bounds ").Append(tl.InteractiveBounds.ToString("F2"));
ServeableItem st = null, co = null; foreach (var it in lm.serveableItems) { if (it == null) continue; if (it.GetFoodType() == "IceCream") st = it; if (it.GetFoodType() == "Coconut") co = it; }
if (st != null) { var sr = st.GetComponent<SpriteRenderer>(); sb.Append(" | station ").Append(st.transform.position.ToString("F2")).Append(" active ").Append(st.gameObject.activeInHierarchy).Append(" sr ").Append(sr != null ? sr.bounds.ToString("F2") : "none"); }
if (co != null) { var d = co.GetComponent<DraggableFood>(); sb.Append(" | coconut ").Append(co.transform.position.ToString("F2")).Append(" active ").Append(co.gameObject.activeInHierarchy).Append(" radius ").Append(d != null ? d.ItemRadius.ToString("F2") : "no drag"); }
FixedStation.PushOthersOff(tl, lm.serveableItems);
if (co != null) sb.Append(" | after push coconut ").Append(co.transform.position.ToString("F2"));
return sb.ToString();
