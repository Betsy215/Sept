// Import settings for the taped note (27 November 2026): sprites at 200 ppu, the paper 9-sliced (L/R 120 px, T/B 100 px).
foreach (var p in new[] { "Assets/Resources/Tutorial/note_paper.png", "Assets/Resources/Tutorial/note_tape.png" })
{
    var ti = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(p); if (ti == null) return "missing " + p;
    ti.textureType = UnityEditor.TextureImporterType.Sprite; ti.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    ti.spritePixelsPerUnit = 200f; ti.mipmapEnabled = false; ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp;
    var st = new UnityEditor.TextureImporterSettings(); ti.ReadTextureSettings(st); st.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(st);
    ti.spriteBorder = p.Contains("paper") ? new Vector4(120f, 100f, 120f, 100f) : Vector4.zero;
    ti.SaveAndReimport();
}
var s = Resources.Load<Sprite>("Tutorial/note_paper"); return "paper " + (s != null ? s.border + " " + s.rect + " ppu " + s.pixelsPerUnit : "null");
