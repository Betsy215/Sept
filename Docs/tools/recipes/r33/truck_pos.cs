// r33: where the map's truck is, whether a day card is up, the scene
var t = GameObject.Find("Truck"); var card = GameObject.Find("DayTitle");
return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " truck " + (t != null ? ((RectTransform)t.transform).anchoredPosition.ToString("F0") + " sx " + t.transform.localScale.x : "none") + " card " + (card != null ? card.GetComponent<TMPro.TextMeshProUGUI>().text : "none");
