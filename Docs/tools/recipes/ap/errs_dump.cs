var sb = new System.Text.StringBuilder(); var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); var sc = UnityEngine.Object.FindObjectOfType<ScoreManager>();
sb.Append("scene ").Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name).Append(" score ").Append(sc != null ? sc.GetCurrentScore().ToString("F0") : "-").Append(" panel ").Append(lm != null && lm.levelCompletePanel != null && lm.levelCompletePanel.activeSelf);
return sb.ToString();
