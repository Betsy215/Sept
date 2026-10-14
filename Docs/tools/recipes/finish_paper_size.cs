// Level complete paper with room left and right (owner, 14 October): FinalPaper was 1150 x 1400 on a 977-wide canvas
// (wider than the screen). Now 860 wide at the paper's own shape (984 x 1198).
var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameSceneOne.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); var p = lm.levelCompletePanel.transform.Find("FinalPaper") as RectTransform;
var was = p.sizeDelta + " at " + p.anchoredPosition; p.sizeDelta = new Vector2(860f, 860f * 1198f / 984f);
var sb = new System.Text.StringBuilder("paper " + was + " -> " + p.sizeDelta + "\n");
foreach (var t in lm.levelCompletePanel.GetComponentsInChildren<TMPro.TMP_Text>(true)) sb.Append(t.name + " " + ((RectTransform)t.transform).anchoredPosition + " " + ((RectTransform)t.transform).sizeDelta + " parent " + t.transform.parent.name + "\n");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s); sb.Append("saved " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s));
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(s, true); return sb.ToString();
