var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); var sm = SessionManager.Instance;
var sc = UnityEngine.Object.FindObjectOfType<ScoreManager>();
var stars = sc != null && sc.starProgressBar != null ? sc.starProgressBar.GetEarnedStarsCount() : -1;
return "ST|" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "|lvl " + (sm != null ? sm.GetCurrentLevelIndex() : -1) + "|money " + (sm != null ? sm.GetTotalScore().ToString("F2") : "-") + "|stars " + stars + "|done " + (sm != null ? sm.GetLevelsCompleted() : -1) + "|panel " + (lm != null && lm.levelCompletePanel != null && lm.levelCompletePanel.activeSelf);
