var wm = UnityEngine.Object.FindObjectOfType<WaffleAnim>(); if (wm == null) return "no waffle machine";
var rt = (UnityEngine.RectTransform)wm.transform; var c = new UnityEngine.Vector3[4]; rt.GetWorldCorners(c);
var img = wm.GetComponent<UnityEngine.UI.Image>();
return "corners " + c[0] + " " + c[2] + " children " + rt.childCount + " sprite " + (img.sprite != null ? img.sprite.name : "none");
