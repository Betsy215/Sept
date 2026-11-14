var sm = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (sm == null) return "no sm";
var m = typeof(ShopManager).GetMethod("SetTab", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
var en = typeof(ShopManager).GetNestedType("ShopTab", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public) ?? System.Type.GetType("ShopTab") ?? typeof(ShopManager).Assembly.GetType("ShopTab");
if (m == null || en == null) return "no SetTab/enum " + (m==null) + (en==null);
m.Invoke(sm, new object[]{ System.Enum.Parse(en, "Stars") }); return "tab Stars";
