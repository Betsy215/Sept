
var sp0 = GameObject.Find("Spot0"); var sp1 = GameObject.Find("Spot1");
if (sp0 != null) sp0.transform.position = new Vector3(0.10000000000000009f, -0.25f, 0f);
if (sp1 != null) sp1.transform.position = new Vector3(-1.45f, -0.25f, 0f);
foreach (var c in UnityEngine.Object.FindObjectsOfType<CustomerController>()) c.transform.localScale = c.transform.localScale / 0.9f * 0.85f;
var o0 = GameObject.Find("OrderSlot0"); var o1 = GameObject.Find("OrderSlot1");
if (o0 != null) { o0.transform.position = new Vector3(1.3382f, 1.6232000000000002f, 0f); o0.transform.localScale = Vector3.one * 0.62f; }
if (o1 != null) { o1.transform.position = new Vector3(-0.2617999999999999f, 1.6232000000000002f, 0f); o1.transform.localScale = Vector3.one * 0.62f; }
var m0 = GameObject.Find("PatienceMeter0"); var m1 = GameObject.Find("PatienceMeter1");
if (m0 != null) { m0.transform.position = new Vector3(0.65f, 3.7498000000000005f, 0f); m0.transform.localScale = Vector3.one * 0.75f; }
if (m1 != null) { m1.transform.position = new Vector3(-0.95f, 3.7498000000000005f, 0f); m1.transform.localScale = Vector3.one * 0.75f; }
return "layout C: spots " + (sp0 != null) + (sp1 != null) + " slots " + (o0 != null) + (o1 != null) + " meters " + (m0 != null) + (m1 != null);
