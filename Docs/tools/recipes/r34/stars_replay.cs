// r34 (24 Nov 2026): replays the day-end card's stars with a celebration style and a star count (__STYLE__ 0-3, __N__ 1-3)
StarCelebration.Current = (StarCelebration.Style)__STYLE__;
var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no LevelManager";
var row = GameObject.Find("StarsRow"); if (row == null) return "no row";
var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
lm.StopAllCoroutines();
var rays = row.transform.Find("Rays"); if (rays != null) UnityEngine.Object.DestroyImmediate(rays.gameObject);
foreach (Transform s in row.transform) { var g = s.Find("Gold"); if (g != null) { g.localScale = Vector3.zero; g.localRotation = Quaternion.identity; var sh = g.Find("Shine"); if (sh != null) UnityEngine.Object.DestroyImmediate(sh.gameObject); } }
var fx = row.GetComponentInParent<Canvas>().rootCanvas.transform.Find("StarFx"); if (fx != null) UnityEngine.Object.DestroyImmediate(fx.gameObject);
var e = (System.Collections.IEnumerator)typeof(LevelManager).GetMethod("PopStars", BF).Invoke(lm, new object[] { row.transform, __N__ });
typeof(LevelManager).GetField("starsPop", BF).SetValue(lm, lm.StartCoroutine(e));
return "replay style " + StarCelebration.Current + " stars " + __N__;
