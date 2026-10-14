// Level complete panel preview (14 October 2026): swap FinalPaper's sprite for a PNG on disk while the finish card is
// up. Args in /tmp/ap/paper_args.txt: path|textColour (hex, empty = keep). Also gives the card three earned stars.
var args = System.IO.File.ReadAllText("/tmp/ap/paper_args.txt").Trim().Split('|');
var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no LevelManager";
var paper = lm.levelCompletePanel.transform.Find("FinalPaper")?.GetComponent<UnityEngine.UI.Image>(); if (paper == null) return "no FinalPaper";
var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.LoadImage(System.IO.File.ReadAllBytes(args[0]));
paper.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
if (args.Length > 1 && args[1] != "" && ColorUtility.TryParseHtmlString(args[1], out var c))
    foreach (var t in lm.levelCompletePanel.GetComponentsInChildren<TMPro.TMP_Text>(true)) { t.color = c; t.fontSharedMaterial = t.fontSharedMaterial; }
return "paper swapped";
