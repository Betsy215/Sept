var moves = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, UnityEngine.Vector2>>();
moves.Add(new System.Collections.Generic.KeyValuePair<string, UnityEngine.Vector2>("Cake", new UnityEngine.Vector2(1.5f, -1.2f)));
moves.Add(new System.Collections.Generic.KeyValuePair<string, UnityEngine.Vector2>("Apple", new UnityEngine.Vector2(1.0f, -2.5f)));
moves.Add(new System.Collections.Generic.KeyValuePair<string, UnityEngine.Vector2>("Juice", new UnityEngine.Vector2(-0.2f, -3.3f)));
moves.Add(new System.Collections.Generic.KeyValuePair<string, UnityEngine.Vector2>("Choux", new UnityEngine.Vector2(-1.3f, -1.3f)));
ArrangeDemo.Start(moves, 40, 12); return "arrange demo started";
