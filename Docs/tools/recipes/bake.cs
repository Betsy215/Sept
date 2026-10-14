var o = GameObject.Find("oven2"); if (o == null) return "no oven2";
var b = o.GetComponent<OvenKitchenBase>(); if (b == null) return "no oven script";
b.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)); return "baking";
