// r33: clicks a named button on the map ("Day9", "NextChapter", "Day3" ...); name in the first line's constant
const string NAME = "__NAME__";
foreach (var b in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>()) if (b.name == NAME) { b.onClick.Invoke(); return "clicked " + NAME; }
return "no button " + NAME;
