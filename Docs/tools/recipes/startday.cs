var g = UnityEngine.Object.FindObjectOfType<GamePhaseManager>(); if (g == null) return "no GamePhaseManager";
g.OnDoneButtonClicked(); return "day started";
