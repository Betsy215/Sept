// Oven stickers follow the menu book (owner, 14 October 2026): a KitchenFoodGate on each sticky note, and a fourth note
// for Cookies cloned from sticker1, placed to the right of the cake note (sticker3). Args /tmp/ap/sticker_args.txt:
// "inspect" prints positions only; "apply" makes the change and saves KitchenScene.
var mode = System.IO.File.ReadAllText("/tmp/ap/sticker_args.txt").Trim();
var k = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
var sb = new System.Text.StringBuilder(); var find = new System.Func<string, GameObject>(n => { foreach (var go in k.GetRootGameObjects()) foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t.gameObject; return null; });
foreach (var n in new[] { "sticker1", "sticker2", "sticker3", "oven1", "oven2", "oven3", "cake", "cookies" }) {
    var go = find(n); if (go == null) { sb.Append(n + " missing\n"); continue; }
    var rt = go.GetComponent<RectTransform>(); sb.Append(n + " parent " + go.transform.parent.name + " pos " + rt.anchoredPosition + " size " + rt.sizeDelta + " world " + rt.position + " sibling " + go.transform.GetSiblingIndex() + " active " + go.activeSelf + "\n");
}
if (mode == "apply") {
    // the ovens keep their place; each oven's own gate hides its note and sleeps its baking when the food is off
    var ovenFood = new System.Collections.Generic.Dictionary<string, string> { { "oven1", "Choux" }, { "oven2", "Bread" }, { "oven3", "Cake" } };
    foreach (var kv in ovenFood) { var go = find(kv.Key); if (go == null) continue; var g = go.GetComponent<KitchenFoodGate>() ?? go.AddComponent<KitchenFoodGate>(); g.associatedFoodType = kv.Value; g.keepVisible = true; g.enableDebugLogs = false; }
    var src = find("sticker3"); var old = find("sticker4");
    if (src != null && old == null) {
        var clone = UnityEngine.Object.Instantiate(src, src.transform.parent); clone.name = "sticker4"; clone.transform.SetSiblingIndex(src.transform.GetSiblingIndex() + 1);
        var rt = clone.GetComponent<RectTransform>(); rt.anchoredPosition = src.GetComponent<RectTransform>().anchoredPosition + new Vector2(float.Parse(System.IO.File.ReadAllText("/tmp/ap/sticker_dx.txt").Trim()), 0f);
        var img = clone.GetComponent<UnityEngine.UI.Image>(); img.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Kitchen/ovens/sticker_cookies.png");
        var g = clone.GetComponent<KitchenFoodGate>() ?? clone.AddComponent<KitchenFoodGate>(); g.associatedFoodType = "Cookies"; g.enableDebugLogs = false;
        sb.Append("sticker4 made at " + rt.anchoredPosition + "\n");
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(k); sb.Append("saved " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(k) + "\n");
}
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(k, true); return sb.ToString();
