var sb = new System.Text.StringBuilder(); var cam = UnityEngine.Camera.main;
foreach (var r in UnityEngine.Object.FindObjectsOfType<UnityEngine.SpriteRenderer>()) {
    if (!r.enabled || !r.gameObject.activeInHierarchy) continue; var b = r.bounds; var p = cam.WorldToScreenPoint(b.center);
    if (b.size.y < 0.25f && b.size.x > 1.5f) sb.Append($"SR {r.name} parent={r.transform.parent?.name} sprite={r.sprite?.name} size={b.size.x:F2}x{b.size.y:F2} screen=({p.x:F0},{p.y:F0}) scale={r.transform.lossyScale} color={r.color}\n");
}
foreach (var l in UnityEngine.Object.FindObjectsOfType<UnityEngine.LineRenderer>()) sb.Append($"LR {l.name} parent={l.transform.parent?.name} enabled={l.enabled}\n");
foreach (var g in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Graphic>()) {
    if (!g.enabled || !g.gameObject.activeInHierarchy) continue; var rt = g.rectTransform; var c = new UnityEngine.Vector3[4]; rt.GetWorldCorners(c);
    var w = UnityEngine.Vector3.Distance(c[0], c[3]); var h = UnityEngine.Vector3.Distance(c[0], c[1]); var canvas = g.canvas; var sp = canvas != null && canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? cam.WorldToScreenPoint((c[0]+c[2])/2) : (c[0]+c[2])/2;
    if (h < 12f && w > 300f) sb.Append($"UI {g.name} parent={g.transform.parent?.name} type={g.GetType().Name} w={w:F0} h={h:F1} screen=({sp.x:F0},{sp.y:F0}) color={g.color}\n");
}
return sb.Length > 0 ? sb.ToString() : "nothing thin";
