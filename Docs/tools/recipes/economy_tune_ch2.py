"""Chapter 2 star lines from a driver run (17 October 2026): reads /tmp/ap/days_ch2.tsv (days 17 to 32 as day index
16..31 with the perfect run's score), smooths each day's score over three days, never lets a line fall, and writes
/tmp/ap/economy_tune_ch2.cs, the editor recipe that sets Level17..32Data.starThreshold1..3 at 30 / 55 / 80 % of it,
the same rule as chapter 1 (Docs/tools/recipes/economy_tune.cs). Run: python3 economy_tune_ch2.py; then the recipe."""
import re, json
rows = {}
for line in open("/tmp/ap/days_ch2.tsv"):
    p = line.rstrip("\n").split("\t")
    if len(p) < 2: continue
    m = re.search(r"score (\d+)", p[1])
    if m: rows[int(p[0])] = int(m.group(1))
days = sorted(rows); print("measured", {d + 1: rows[d] for d in days})
sm = {}
for d in days:
    nb = [rows[x] for x in (d - 1, d, d + 1) if x in rows]; sm[d] = sum(nb) / len(nb)
table = {}; last = (0, 0, 0)
for d in days:
    s = sm[d]; t = (int(round(s * 0.30 / 10) * 10), int(round(s * 0.55 / 10) * 10), int(round(s * 0.80 / 10) * 10))
    t = tuple(max(a, b) for a, b in zip(t, last)); last = t; table[d + 1] = t
print("lines", table)
items = ", ".join("{%d, new[]{%d,%d,%d}}" % (k, *v) for k, v in table.items())
cs = """// Chapter 2 star lines from the 17 October driver run (economy_tune_ch2.py): 30 / 55 / 80 %% of a perfect day's score, smoothed, never falling
var table = new System.Collections.Generic.Dictionary<int,int[]> { %s };
var sb = new System.Text.StringBuilder();
foreach (var kv in table) {
    var ld = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Entity/Level" + kv.Key + "Data.asset"); if (ld == null) { sb.Append("missing " + kv.Key + "; "); continue; }
    ld.starThreshold1 = kv.Value[0]; ld.starThreshold2 = kv.Value[1]; ld.starThreshold3 = kv.Value[2]; UnityEditor.EditorUtility.SetDirty(ld);
}
UnityEditor.AssetDatabase.SaveAssets(); return "set " + table.Count + " days " + sb;
""" % items
open("/tmp/ap/economy_tune_ch2.cs", "w").write(cs); print("recipe written for", len(table), "days")
