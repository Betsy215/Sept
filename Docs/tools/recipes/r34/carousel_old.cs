// r34 (24 Nov 2026): the sticker carousel back at its old size (280, strip 300, overlap -90) for a before picture
var c = GameObject.Find("StickerCarousel"); if (c == null) return "no carousel";
var rt = (RectTransform)c.transform; rt.sizeDelta = new Vector2(rt.sizeDelta.x, 300f);
var content = c.transform.Find("Content"); content.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().spacing = -90f;
foreach (Transform t in content) ((RectTransform)t).sizeDelta = new Vector2(280f, 280f);
UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content); return "old sizes";
