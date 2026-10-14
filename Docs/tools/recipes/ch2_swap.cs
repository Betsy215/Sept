// Chapter 2 preview in the real game (14 October 2026): swaps the background, the table cloth and the day receipt
// for PNG files on disk while in play mode. Args in /tmp/ap/ch2_args.txt: bg|cloth|receipt|night  (empty = keep).
var args = System.IO.File.ReadAllText("/tmp/ap/ch2_args.txt").Trim().Split('|');
System.Func<string, Sprite, Sprite> load = (path, like) => {
    if (string.IsNullOrEmpty(path) || like == null) return null;
    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.LoadImage(System.IO.File.ReadAllBytes(path)); tex.wrapMode = TextureWrapMode.Clamp;
    var ppu = like.pixelsPerUnit * tex.width / like.rect.width;               // same world size as the sprite it replaces
    var piv = new Vector2(like.pivot.x / like.rect.width, like.pivot.y / like.rect.height);
    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), piv, ppu);
};
var log = "";
var bg = GameObject.Find("Canvas/bg")?.GetComponent<UnityEngine.UI.Image>();
if (bg != null && args.Length > 0 && args[0] != "") { bg.sprite = load(args[0], bg.sprite); log += "bg ok; "; }
var tl = UnityEngine.Object.FindObjectOfType<TableLayer>();
if (tl != null && args.Length > 1 && args[1] != "") { tl.tableClothSprite.sprite = load(args[1], tl.tableClothSprite.sprite); log += "cloth ok; "; }
var sm = UnityEngine.Object.FindObjectOfType<ScoreManager>();
if (sm != null && args.Length > 2 && args[2] != "") { sm.popupBackgroundSprite = load(args[2], sm.popupBackgroundSprite); log += "receipt ok; "; }
if (args.Length > 3 && args[3] == "1") {                                     // night picture: the picture is the night, no tint or chapter 1 sky
    var dn = UnityEngine.Object.FindObjectOfType<DayNightTint>(); if (dn != null) dn.enabled = false;
    var sky = GameObject.Find("Canvas/NightSky"); if (sky != null) sky.SetActive(false);
    if (bg != null) bg.color = Color.white; log += "night; ";
}
return log;
