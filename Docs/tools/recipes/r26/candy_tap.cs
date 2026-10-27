var jar = UnityEngine.Object.FindObjectOfType<CandyJar>(); if (jar == null) return "no jar"; var ok = jar.Tap(); return "candy given " + ok + ", left " + CandyJar.Candies;
