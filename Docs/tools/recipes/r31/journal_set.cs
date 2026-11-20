// r31 (20 Nov 2026): a journal with the chapter 1 pages this save has really earned (opening day, three stars, cake,
// choux), each with its day, none new and none still to show, for the store pictures and the preview's book part
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager";
var s = sm.GetCurrentSession();
s.journal = new System.Collections.Generic.List<string> { "open|1", "star3|1", "cake|3", "choux|5" };
s.journalFilled = true; s.journalNew = new System.Collections.Generic.List<string>();
s.journalShown = new System.Collections.Generic.List<string> { "open", "star3", "cake", "choux" };
return "journal " + s.journal.Count;
