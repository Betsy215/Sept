
var sp0 = GameObject.Find("Spot0"); var sp1 = GameObject.Find("Spot1");
if (sp0 != null) sp0.transform.position = new Vector3(0.19999999999999996f, -0.3f, 0f);
if (sp1 != null) sp1.transform.position = new Vector3(-1.5f, -0.3f, 0f);
foreach (var c in UnityEngine.Object.FindObjectsOfType<CustomerController>()) c.transform.localScale = c.transform.localScale / 0.9f * 0.9f;
var o0 = GameObject.Find("OrderSlot0"); var o1 = GameObject.Find("OrderSlot1");
if (o0 != null) { o0.transform.position = new Vector3(1.4215f, 1.559f, 0f); o0.transform.localScale = Vector3.one * 0.65f; }
if (o1 != null) { o1.transform.position = new Vector3(-0.27849999999999986f, 1.559f, 0f); o1.transform.localScale = Vector3.one * 0.65f; }
var m0 = GameObject.Find("PatienceMeter0"); var m1 = GameObject.Find("PatienceMeter1");
if (m0 != null) { m0.transform.position = new Vector3(0.7f, 3.7885f, 0f); m0.transform.localScale = Vector3.one * 0.8f; }
if (m1 != null) { m1.transform.position = new Vector3(-1.0f, 3.7885f, 0f); m1.transform.localScale = Vector3.one * 0.8f; }
return "layout B: spots " + (sp0 != null) + (sp1 != null) + " slots " + (o0 != null) + (o1 != null) + " meters " + (m0 != null) + (m1 != null);
