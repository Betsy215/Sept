if (!UnityEditor.EditorApplication.isPlaying) { UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity"); UnityEditor.EditorApplication.isPlaying = true; return "play started"; }
return "already playing";
