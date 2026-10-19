var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop == null) return "no shop";
var beach = UnityEngine.Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/beachday SDF"); if (beach == null) return "no beachday";
var info = shop.popupItemInfo; info.font = beach; info.fontSize = SIZE; info.fontStyle = TMPro.FontStyles.Normal;
var m = info.fontMaterial; if (m != null && m.HasProperty("_FaceDilate")) m.SetFloat("_FaceDilate", 0f);
var note = info.transform.parent.Find("PopupNote"); if (note != null) { var n = note.GetComponent<TMPro.TextMeshProUGUI>(); n.font = beach; n.fontSize = SIZE - 14; n.fontStyle = TMPro.FontStyles.Normal; var nm = n.fontMaterial; if (nm != null && nm.HasProperty("_FaceDilate")) nm.SetFloat("_FaceDilate", 0f); }
return "beachday " + SIZE;
