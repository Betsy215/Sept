var mb = UnityEngine.Object.FindObjectOfType<MenuBook>(); var p = GameObject.Find("MenuPaper"); if (mb == null || p == null) return "none";
var rt = (RectTransform)p.transform;
System.Collections.IEnumerator Go() {
    var start = rt.anchoredPosition; var path = new[] { new Vector2(-120, -110), new Vector2(60, -170), Vector2.zero };
    var from = start;
    foreach (var d in path) { var to = start + d; for (int i = 1; i <= 18; i++) { var k = i / 18f; k = k * k * (3 - 2 * k); rt.anchoredPosition = Vector2.Lerp(from, to, k); yield return null; } from = to; }
}
mb.StartCoroutine(Go()); return "dragging";
