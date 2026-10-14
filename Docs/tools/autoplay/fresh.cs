var sm = SessionManager.Instance; if (sm == null) return "no SM";
sm.StartNewSession(); return "fresh session, tutorial due " + TutorialDirector.IsDue(sm) + ", foods " + string.Join(",", sm.GetCurrentSession().purchasedFoodItems) + ", chars " + string.Join(",", sm.GetCurrentSession().purchasedCharacters) + ", money " + sm.GetTotalScore();
