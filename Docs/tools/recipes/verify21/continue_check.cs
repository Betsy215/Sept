var cc = UnityEngine.Object.FindObjectOfType<ClickContinue>(); if (cc == null) return "no ClickContinue";
cc.SendMessage("UpdateButtonState"); var b = cc.GetComponent<UnityEngine.UI.Button>();
var s = SessionManager.Instance.GetCurrentSession();
return "V|continue|interactable=" + (b != null && b.interactable) + "|tutorialSeen=" + s.tutorialSeen + "|done=" + s.levelsCompleted;
