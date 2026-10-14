var cm = UnityEngine.Object.FindObjectOfType<CoffeeMachine>(); if (cm == null) return "no machine";
var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
cm.OnPointerClick(ev); return "brewing";
