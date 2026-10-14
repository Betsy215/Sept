// The kitchen's apple tree as its own sprite (owner, 14 October 2026: chapter 2 swaps it for a coconut palm).
// bg -> kitchen_bg_notree (the owner's painting with the window refilled from its own sky and grass); a child Image
// "tree" (tree_apple.png, the cut-out) anchored to the same fractions of bg, so it stretches with it.
var k = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
GameObject bg = null; foreach (var go in k.GetRootGameObjects()) foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (t.name == "bg" && t.GetComponent<UnityEngine.UI.Image>() != null) { bg = t.gameObject; break; }
if (bg == null) { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(k, true); return "no bg"; }
var img = bg.GetComponent<UnityEngine.UI.Image>(); var was = img.sprite != null ? img.sprite.name : "-";
img.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Kitchen/kitchen_bg_notree.png");
var old = bg.transform.Find("tree"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
var tree = new GameObject("tree", typeof(RectTransform), typeof(UnityEngine.UI.Image)); tree.transform.SetParent(bg.transform, false); tree.transform.SetAsFirstSibling();
var rt = tree.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(540f / 1080f, 1f - 862f / 1920f); rt.anchorMax = new Vector2(816f / 1080f, 1f - 383f / 1920f); rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
var ti = tree.GetComponent<UnityEngine.UI.Image>(); ti.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Kitchen/tree_apple.png"); ti.raycastTarget = false;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(k); var saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(k);
var brt = bg.GetComponent<RectTransform>(); var info = "bg was " + was + " rect " + brt.rect.size + " anchors " + brt.anchorMin + brt.anchorMax + " children " + bg.transform.childCount;
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(k, true); return "tree added, saved " + saved + "; " + info;
