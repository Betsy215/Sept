var j = UnityEngine.Object.FindObjectOfType<JuiceInKitchen>(); if (j == null) return "no juice";
j.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)); return "juice";
