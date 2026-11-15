using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// The game's star picture inside a line of text, in place of the word "stars" (owner, 15 November 2026: "In star tab,
/// the 15 star item description, Use star icon instead of star text in shop"). A text marks the spot with Token; the
/// label keeps an invisible '@' there that holds the room, and a star picture is laid over it, the size of the letters.
/// Call Place after the label's text, size and alignment are final (it reads where the letters ended up).
public static class TextStars
{
    public const string Token = "[*]";
    private const string MarkTag = "<alpha=#00>@<alpha=#FF>";   // TMP has no </alpha>: the second tag brings the letters back
    private const string IconName = "TextStar";

    public static string Mark(string text) => text == null ? null : text.Replace(Token, MarkTag);

    public static void Place(TMP_Text label)
    {
        if (label == null) return;
        for (var i = label.transform.childCount - 1; i >= 0; i--)
        {
            var c = label.transform.GetChild(i); if (c.name == IconName) Object.Destroy(c.gameObject);
        }
        if (string.IsNullOrEmpty(label.text) || label.text.IndexOf(MarkTag) < 0) return;
        var sprite = Resources.Load<Sprite>("UI/star_icon"); if (sprite == null) return;
        label.ForceMeshUpdate();
        var info = label.textInfo; var rt = label.rectTransform; if (info == null || info.characterInfo == null) return;   // a label that is off has no letters laid out
        var pivotOff = new Vector2(rt.rect.width * (0.5f - rt.pivot.x), rt.rect.height * (0.5f - rt.pivot.y));
        for (var i = 0; i < info.characterCount; i++)
        {
            var ch = info.characterInfo[i]; if (ch.character != '@') continue;
            var size = ch.pointSize * 1.0f;
            var go = new GameObject(IconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(label.transform, false);
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
            var r = img.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f); r.sizeDelta = new Vector2(size, size);
            var middle = new Vector2((ch.origin + ch.xAdvance) * 0.5f, (ch.ascender + ch.descender) * 0.5f);
            r.anchoredPosition = middle - pivotOff;
        }
    }
}
