var a = UnityEngine.Object.FindObjectOfType<AppleInKitchen>(); if (a == null) return "no apples";
a.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)); return "apples";
