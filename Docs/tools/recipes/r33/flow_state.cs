// r33: the kitchen's state after a day end
return "choux " + KitchenFlow.StageOf("Choux") + " cake " + KitchenFlow.StageOf("Cake") + " mixing " + KitchenFlow.Mixing + " cutting " + KitchenFlow.Cutting + " ovens " + OvenKitchenBase.Recipes.Count + " anyBaking " + OvenKitchenBase.AnyBaking() + " apples " + AppleInKitchen.Count;
