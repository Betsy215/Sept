// Cookies back (14 October 2026): the kitchen 'cookies' plate object is inactive in KitchenScene since the 13 October
// hold-back; switch it on and save, so its KitchenFoodGate decides from the save (bought and switched on in the menu book).
var k = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/KitchenScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
GameObject target = null;
foreach (var go in k.GetRootGameObjects()) foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (t.name == "cookies" && t.GetComponent<KitchenFoodGate>() != null) target = t.gameObject;
if (target == null) { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(k, true); return "no cookies object"; }
var was = target.activeSelf; target.SetActive(true);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(k);
var saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(k);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(k, true);
return "cookies was active " + was + ", now true, saved " + saved;
