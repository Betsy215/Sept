// r34: the table's geometry for the bounds picture: camera, screen, cloth, padding, the drag bounds, every food's drag rect
var tl = UnityEngine.Object.FindObjectOfType<TableLayer>(); if (tl == null) return "no TableLayer";
var cam = Camera.main; var sb = new System.Text.StringBuilder();
var cs = tl.tableClothSprite; var cb = cs.bounds;
sb.Append("{\"cam\":[" + cam.transform.position.x + "," + cam.transform.position.y + "," + cam.orthographicSize + "," + cam.aspect + "],");
sb.Append("\"screen\":[" + Screen.width + "," + Screen.height + "],\"safe\":[" + Screen.safeArea.x + "," + Screen.safeArea.y + "," + Screen.safeArea.width + "," + Screen.safeArea.height + "],");
sb.Append("\"cloth\":[" + cb.min.x + "," + cb.min.y + "," + cb.max.x + "," + cb.max.y + "],");
sb.Append("\"pad\":[" + tl.tableBoundsPadding + "," + tl.topReserve + "," + tl.bottomPadding + "," + tl.screenCoveragePercent + "],");
var b = tl.InteractiveBounds; sb.Append("\"bounds\":[" + b.min.x + "," + b.min.y + "," + b.max.x + "," + b.max.y + "],\"items\":[");
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var first = true;
foreach (var d in UnityEngine.Object.FindObjectsOfType<DraggableFood>())
{
    if (!d.gameObject.activeInHierarchy) continue;
    var hs = (Vector2)typeof(DraggableFood).GetField("halfSize", FL).GetValue(d); var co = (Vector2)typeof(DraggableFood).GetField("centreOffset", FL).GetValue(d);
    var p = d.transform.position;
    sb.Append((first ? "" : ",") + "[\"" + d.name + "\"," + (p.x + co.x - hs.x) + "," + (p.y + co.y - hs.y) + "," + (p.x + co.x + hs.x) + "," + (p.y + co.y + hs.y) + "]"); first = false;
}
sb.Append("],\"chapter\":" + SessionManager.CurrentChapter + "}");
return sb.ToString();
