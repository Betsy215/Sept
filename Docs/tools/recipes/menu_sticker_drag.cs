var mb = UnityEngine.Object.FindObjectOfType<MenuBook>(); var st = GameObject.Find("Sticker_" + System.IO.File.ReadAllText("/tmp/ap/drag_food.txt").Trim()); if (mb == null || st == null) return "none";
var d = st.GetComponent<MenuBook.StickerDrag>(); var p = System.IO.File.ReadAllText("/tmp/ap/drag_delta.txt").Trim().Split(',');
mb.StartCoroutine(d.DragBy(new Vector2(float.Parse(p[0]), float.Parse(p[1])), 24)); return "dragging " + st.name;
