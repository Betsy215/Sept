var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet"; sm.StartNewSession(); sm.MarkTutorialSeen(); sm.AddScoreImmediately(500f); return "fresh session";
