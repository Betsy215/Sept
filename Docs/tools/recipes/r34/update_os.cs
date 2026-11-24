// r34 (23 Nov 2026): the update card never asks for a version this phone's iOS cannot install
var now = new System.DateTime(2026, 12, 1, 12, 0, 0, System.DateTimeKind.Utc);
System.Func<string, string, string> J = (v, os) => "{\"resultCount\":1,\"results\":[{\"version\":\"" + v + "\",\"currentVersionReleaseDate\":\"2026-11-28T08:00:00Z\",\"minimumOsVersion\":\"" + os + "\"}]}";
return "store needs 13.0, phone 18.7.8: " + UpdateGate.NeedsUpdate(J("2026.12.05", "13.0"), "2026.11.23", now, "18.7.8")
     + " | store needs 26.0, phone 18.7.8: " + UpdateGate.NeedsUpdate(J("2026.12.05", "26.0"), "2026.11.23", now, "18.7.8")
     + " | store needs 18.7.8, phone 18.7.8: " + UpdateGate.NeedsUpdate(J("2026.12.05", "18.7.8"), "2026.11.23", now, "18.7.8")
     + " | os unknown: " + UpdateGate.NeedsUpdate(J("2026.12.05", "26.0"), "2026.11.23", now, "")
     + " | no minimum in answer: " + UpdateGate.NeedsUpdate("{\"resultCount\":1,\"results\":[{\"version\":\"2026.12.05\",\"currentVersionReleaseDate\":\"2026-11-28T08:00:00Z\"}]}", "2026.11.23", now, "18.7.8");
