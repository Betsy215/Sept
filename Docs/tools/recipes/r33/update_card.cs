// r33 (23 Nov 2026): the update gate's card, and its version checks
var now = new System.DateTime(2026, 12, 1, 0, 0, 0, System.DateTimeKind.Utc);
string J(string v, string d) => "{\"resultCount\":1,\"results\":[{\"version\":\"" + v + "\",\"currentVersionReleaseDate\":\"" + d + "\"}]}";
var r = "newer+old enough " + UpdateGate.NeedsUpdate(J("2026.12.05", "2026-11-28T08:00:00Z"), "2026.11.23", now)
      + " | newer but fresh " + UpdateGate.NeedsUpdate(J("2026.12.05", "2026-11-30T20:00:00Z"), "2026.11.23", now)
      + " | same " + UpdateGate.NeedsUpdate(J("2026.11.23", "2026-11-01T08:00:00Z"), "2026.11.23", now)
      + " | store older " + UpdateGate.NeedsUpdate(J("2026.11.20", "2026-11-01T08:00:00Z"), "2026.11.23", now)
      + " | old scheme vs new " + UpdateGate.Compare("26.11.22", "2026.11.18") + " | garbage " + UpdateGate.NeedsUpdate("<html>", "2026.11.23", now) + " | empty " + UpdateGate.NeedsUpdate("{\"resultCount\":0,\"results\":[]}", "2026.11.23", now);
UpdateGate.ShowForTest(); return r;
