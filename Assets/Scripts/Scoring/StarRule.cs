/// The day's stars (owner, 14 November 2026): from the customers served, no longer from money.
/// "If all customers are served with all items then we consider it three star." "For two star, let's do a ratio, for
/// example 90 percent (rounded to customer count) is served full order." One star at 70 percent and none below is a
/// gap-fill until the owner says otherwise.
/// A customer counts when their order closes: served whole, or lost (left with nothing, or with only part of it).
/// The share is rounded to the nearest whole customer and a half rounds down, so two stars always allow one customer
/// lost (15 customers: 13.5 becomes 13). Before any order has closed the day stands at three stars.
public static class StarRule
{
    public const int TwoStarPercent = 90, OneStarPercent = 70;

    /// Customers that must be served whole for that share of the day's customers (whole numbers only, no float edge)
    public static int Needed(int customers, int percent) => (customers * percent + 49) / 100;

    public static int Stars(int served, int lost)
    {
        if (lost <= 0) return 3;
        var customers = served + lost;
        if (served >= Needed(customers, TwoStarPercent)) return 2;
        if (served >= Needed(customers, OneStarPercent)) return 1;
        return 0;
    }
}
