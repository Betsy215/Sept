// Economy tune (14 October 2026): star lines from the 13 October perfect-run earnings, smoothed over three days and
// never falling (days 1 and 5 eased by hand after the 14 October run: 140 and 373 scored): 1 star = 30 %, 2 = 55 %, 3 = 80 % of what a fast perfect player earns. Sets Level1..16Data.
var table = new System.Collections.Generic.Dictionary<int,int[]> { {1, new[]{50,90,130}}, {2, new[]{70,130,190}}, {3, new[]{90,170,250}}, {4, new[]{120,220,320}}, {5, new[]{130,230,340}}, {6, new[]{230,430,620}}, {7, new[]{270,490,720}}, {8, new[]{270,490,720}}, {9, new[]{270,490,720}}, {10, new[]{270,490,720}}, {11, new[]{280,510,740}}, {12, new[]{300,550,810}}, {13, new[]{310,570,830}}, {14, new[]{330,610,880}}, {15, new[]{330,610,880}}, {16, new[]{330,610,880}} };
var sb = new System.Text.StringBuilder();
foreach (var kv in table) {
    var ld = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Entity/Level" + kv.Key + "Data.asset"); if (ld == null) { sb.Append("missing " + kv.Key + "; "); continue; }
    ld.starThreshold1 = kv.Value[0]; ld.starThreshold2 = kv.Value[1]; ld.starThreshold3 = kv.Value[2]; UnityEditor.EditorUtility.SetDirty(ld);
}
UnityEditor.AssetDatabase.SaveAssets(); return "set " + table.Count + " days " + sb;