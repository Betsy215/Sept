// Chapter 2 star lines from the 17 October driver run (economy_tune_ch2.py): 30 / 55 / 80 % of a perfect day's score, smoothed, never falling
var table = new System.Collections.Generic.Dictionary<int,int[]> { {17, new[]{510,940,1370}}, {18, new[]{520,950,1380}}, {19, new[]{570,1040,1510}}, {20, new[]{570,1040,1510}}, {21, new[]{610,1120,1620}}, {22, new[]{640,1170,1700}}, {23, new[]{640,1170,1700}}, {24, new[]{640,1170,1700}}, {25, new[]{640,1170,1700}}, {26, new[]{640,1170,1700}}, {27, new[]{680,1250,1820}}, {28, new[]{750,1370,1990}}, {29, new[]{780,1430,2080}}, {30, new[]{800,1470,2140}}, {31, new[]{800,1470,2140}}, {32, new[]{800,1470,2140}} };
var sb = new System.Text.StringBuilder();
foreach (var kv in table) {
    var ld = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Entity/Level" + kv.Key + "Data.asset"); if (ld == null) { sb.Append("missing " + kv.Key + "; "); continue; }
    ld.starThreshold1 = kv.Value[0]; ld.starThreshold2 = kv.Value[1]; ld.starThreshold3 = kv.Value[2]; UnityEditor.EditorUtility.SetDirty(ld);
}
UnityEditor.AssetDatabase.SaveAssets(); return "set " + table.Count + " days " + sb;
