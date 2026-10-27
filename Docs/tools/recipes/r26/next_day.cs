var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no LevelManager"; lm.LoadNextLevel(); return "next level";
