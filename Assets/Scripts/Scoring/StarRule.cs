/// The day's stars (owner, 14 November 2026): from the customers served, no longer from money.
/// "If all customers are served with all items then we consider it three star." "For two star, let's do a ratio, for
/// example 90 percent (rounded to customer count) is served full order." "For one star, if more than 50% customers are
/// served full then give 1 star" (same day; it replaces the 70 percent gap-fill). Exactly half or less: no star.
/// A customer counts when their order closes: served whole, or lost (left with nothing, or with only part of it).
/// The share is rounded to the nearest whole customer and a half rounds down (10 customers: 8 for two stars at 80 %;
/// at 90 %, before 19 November, 9). Before any order has closed the day stands at three stars.
public static class StarRule
{
    public const int TwoStarPercent = 80;   // 80 since 19 November 2026 (owner, after 8 of 10 gave one star: "Make 2 stars 80 %"); was 90

    /// Customers that must be served whole for that share of the day's customers (whole numbers only, no float edge)
    public static int Needed(int customers, int percent) => (customers * percent + 49) / 100;

    public static int Stars(int served, int lost)
    {
        if (lost <= 0) return 3;
        var customers = served + lost;
        if (served >= Needed(customers, TwoStarPercent)) return 2;
        if (served * 2 > customers) return 1;   // more than half
        return 0;
    }

    /// While the day runs, the hen's stars show what the day will end on if everyone still to come is served whole.
    /// Counting only the customers so far was wrong early in a day: the first customer lost showed no star and one
    /// lost among the first four showed one, though such a day ends on two (owner's play test, 14 November 2026).
    public static int StarsSoFar(int served, int lost, int stillToCome) => Stars(served + (stillToCome > 0 ? stillToCome : 0), lost);

    /// Customers the day will still see: those waiting now, plus those the clock has room for at the pace so far.
    public static int StillToCome(int served, int lost, int waiting, float elapsed, float timeLeft)
    {
        var started = served + lost + waiting;
        if (started <= 0 || elapsed <= 0f || timeLeft <= 0f) return waiting;
        return waiting + (int)(timeLeft / (elapsed / started));
    }
}
