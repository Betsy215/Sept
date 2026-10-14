var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameSceneOne.unity");
var old = GameObject.Find("TableDecor"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
var root = new GameObject("TableDecor");
System.Func<string, string, Vector3, float, float, GameObject> make = (name, path, pos, rot, scale) => {
    var g = new GameObject(name); g.transform.SetParent(root.transform, false);
    g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0f, 0f, rot); g.transform.localScale = Vector3.one * scale;
    var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
    sr.sortingLayerName = "FoodItems"; sr.sortingOrder = 1; return g; };
make("KitchenPots", "Assets/Images/decor/kitchen_pots.png", new Vector3(1.55f, 0.12f, 0f), 0f, 0.28f);
// the bouquet is out of the game scene (owner, 13 October)
// the truck button on the table gets the copy of its picture with a contact shadow baked in; the rect grows with the canvas
var kb = GameObject.Find("KitchenButton"); var msg = "";
if (kb != null) {
    var img = kb.GetComponent<UnityEngine.UI.Image>(); var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/decor/kit1_table.png");
    if (img != null && sp != null) {
        var rt = kb.GetComponent<RectTransform>(); var oldSp = img.sprite;
        if (oldSp != null && oldSp.name != sp.name) rt.sizeDelta = new Vector2(rt.sizeDelta.x * sp.rect.width / oldSp.rect.width, rt.sizeDelta.y * sp.rect.height / oldSp.rect.height);
        img.sprite = sp; rt.sizeDelta = new Vector2(214f, 130f); msg = " truck " + rt.sizeDelta;   // smaller (owner, 13 October)
    }
}
// the table edge shadow: the original straight strip (owner, 13 October: the edge straight like before)
var es = GameObject.Find("TableOuterShadow");
if (es != null) {
    var esr = es.GetComponent<SpriteRenderer>(); var cs = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/decor/tableedge_soft.png");
    if (esr != null && cs != null) { esr.sprite = cs; esr.sortingOrder = 1; es.transform.localScale = new Vector3(293.25f, 0.3f, 1f); es.transform.localPosition = new Vector3(0f, 4.777f, 0f); msg += " edge soft"; }
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
// the kitchen's cookie plate is out while Cookies are held back (owner, 13 October)
var ks = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity");
var ck = GameObject.Find("cookies"); if (ck == null) { foreach (var t in Resources.FindObjectsOfTypeAll<Transform>()) if (t.name == "cookies" && t.gameObject.scene == ks) { ck = t.gameObject; break; } }
if (ck != null) { ck.SetActive(false); msg += " kitchen cookies off"; }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ks); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(ks);
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
return "decor placed, scene saved" + msg;
