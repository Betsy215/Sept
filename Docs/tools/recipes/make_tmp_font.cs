// Makes the TextMesh Pro font asset for Delius (owner's pick E, 19 November 2026), set up like the Gloria Hallelujah one:
// dynamic, 2048 atlas, point size 90, padding 9, the same characters filled in.
var src = "Assets/TextMesh Pro/Fonts/Delius/Delius-Regular.ttf"; var dst = "Assets/TextMesh Pro/Resources/Fonts & Materials/Delius SDF.asset";
var font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(src); if (font == null) return "no font at " + src;
if (UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(dst) != null) return "already there";
var fa = TMPro.TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, TMPro.AtlasPopulationMode.Dynamic, true);
fa.name = "Delius SDF";
UnityEditor.AssetDatabase.CreateAsset(fa, dst);
fa.material.name = "Delius SDF Material"; fa.atlasTexture.name = "Delius SDF Atlas";
UnityEditor.AssetDatabase.AddObjectToAsset(fa.material, fa); UnityEditor.AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
var old = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/GloriaHallelujah SDF");
var sb = new System.Text.StringBuilder(); for (var c = 32; c < 127; c++) sb.Append((char)c);
if (old != null) foreach (var ch in old.characterTable) if (ch.unicode >= 127 && ch.unicode < 0xFFFF) sb.Append((char)ch.unicode);
string missing; fa.TryAddCharacters(sb.ToString(), out missing);
UnityEditor.EditorUtility.SetDirty(fa); UnityEditor.EditorUtility.SetDirty(fa.material); UnityEditor.EditorUtility.SetDirty(fa.atlasTexture);
UnityEditor.AssetDatabase.SaveAssets(); UnityEditor.AssetDatabase.Refresh();
return "made " + fa.characterTable.Count + " characters, missing [" + missing + "], line height " + fa.faceInfo.lineHeight + " asc " + fa.faceInfo.ascentLine + " cap " + fa.faceInfo.capLine + " | gloria line " + (old != null ? old.faceInfo.lineHeight + " asc " + old.faceInfo.ascentLine + " cap " + old.faceInfo.capLine + " mean " + old.faceInfo.meanLine : "none") + " | delius mean " + fa.faceInfo.meanLine;
