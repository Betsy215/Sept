var asm = typeof(Editor).Assembly;
var sizesType = asm.GetType("UnityEditor.GameViewSizes");
var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { sizesType.GetProperty("currentGroupType").GetValue(instance) });
var groupClass = asm.GetType("UnityEditor.GameViewSizeGroup");
var texts = (string[])groupClass.GetMethod("GetDisplayTexts").Invoke(group, null);
int idx = System.Array.FindIndex(texts, t => t.Contains("iPhone16ProMax"));
if (idx < 0) {
    var sizeType = asm.GetType("UnityEditor.GameViewSize"); var kind = asm.GetType("UnityEditor.GameViewSizeType");
    var size = sizeType.GetConstructor(new[] { kind, typeof(int), typeof(int), typeof(string) })
        .Invoke(new object[] { System.Enum.Parse(kind, "FixedResolution"), 1320, 2868, "iPhone16ProMax" });
    groupClass.GetMethod("AddCustomSize").Invoke(group, new[] { size });
    idx = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null) - 1;
}
var gvType = asm.GetType("UnityEditor.GameView"); var gv = EditorWindow.GetWindow(gvType);
gvType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(gv, idx);
return "game view size index " + idx;
