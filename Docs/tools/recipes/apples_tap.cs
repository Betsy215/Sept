var a = UnityEngine.Object.FindObjectOfType<AppleInKitchen>(); if (a == null) return "no apples";
var ok = a.Take(); return "apple taken " + ok + ", left on the tree " + AppleInKitchen.Count;
