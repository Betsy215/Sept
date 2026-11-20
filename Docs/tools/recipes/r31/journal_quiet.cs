// r31 (20 Nov 2026): store pictures of the shop without a journal page flying in: every page due is marked as shown
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager";
var due = sm.JournalPagesToShow(); foreach (var id in due) sm.MarkJournalShown(id);
return "quiet " + due.Count;
