var locked = UnityEngine.GameObject.Find("NextChapterLocked"); var open = UnityEngine.GameObject.Find("NextChapter");
var today = UnityEngine.GameObject.Find("Day1"); var tb = today != null ? today.GetComponent<MapTodayBounce>() : null;
return "V|map|locked=" + (locked != null) + "|open=" + (open != null) + "|day1today=" + (tb != null) + "|done=" + SessionManager.Instance.GetLevelsCompleted();
