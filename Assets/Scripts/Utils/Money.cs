using System;
using System.Globalization;
using UnityEngine;

/// Money as the player reads it (owner, 26 November 2026: "no cents at all"): whole dollars, one look on every screen,
/// "$ 1814" (dollar sign, a space, plain digits: no comma, owner the same day: "remove the comma in money, just use numbers"). Every money text goes through Format; every amount that
/// reaches the wallet is rounded with Whole first, so what is shown always adds up.
public static class Money
{
    /// "$ 1814". Whole amounts are shown as they are; a part of a dollar (only the count-up on the day-end card has
    /// one) is cut off, never rounded up, so a label cannot show more than the wallet holds.
    public static string Format(float amount)
    {
        return "$ " + Mathf.FloorToInt(amount + 0.0001f).ToString(CultureInfo.InvariantCulture);
    }

    /// The nearest whole dollar, a half goes up (7.50 -> 8): tips when they are earned, and each paid amount on a replayed day.
    public static float Whole(float amount)
    {
        return (float)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
    }
}
