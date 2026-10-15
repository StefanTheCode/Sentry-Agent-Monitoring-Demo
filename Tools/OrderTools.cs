using System.ComponentModel;

namespace OrderSupportAgent.Tools;

public record Order(int Id, string Customer, string Item, decimal Total, DateOnly ShippedOn);

public static class OrderTools
{
    private static readonly Dictionary<int, Order> Orders = new()
    {
        [1042] = new Order(1042, "Ana Kovac",   "Mechanical Keyboard", 129.00m, Today(-14)), // 14 days -> late (legacy)
        [1055] = new Order(1055, "Marko Ilic",  "USB-C Hub",            39.99m, Today(-3)),  // 3 days  -> on time
        [1063] = new Order(1063, "Ivana Peric", "27\" Monitor",        249.50m, Today(-22)), // 22 days -> late (legacy)
        [1071] = new Order(1071, "Luka Novak",  "Webcam",               59.00m, Today(-1)),  // 1 day   -> on time
    };

    [Description("Look up an order by its numeric id and return a summary (customer, item, total).")]
    public static string GetOrder(
        [Description("The numeric order id, e.g. 1042.")] int orderId)
    {
        if (!Orders.TryGetValue(orderId, out var o))
            return $"No order found with id {orderId}.";

        return $"Order {o.Id}: {o.Customer} bought '{o.Item}' for ${o.Total:0.00}.";
    }

    [Description("Get the shipping status for an order, including how many days ago it shipped.")]
    public static string GetShippingStatusBuggy(
        [Description("The order id, e.g. 1042.")] int orderId)
    {
        if (!Orders.TryGetValue(orderId, out var o))
            return $"No order found with id {orderId}.";

        var daysAgo = Today(0).DayNumber - o.ShippedOn.DayNumber;

        if (daysAgo > 10)
            throw new InvalidOperationException(
                $"Could not parse legacy tracking record for order {orderId} (shippedDaysAgo={daysAgo}).");

        return $"Order {o.Id}: shipped {daysAgo} days ago.";
    }

    [Description("Get the shipping status for an order, including how many days ago it shipped.")]
    public static string GetShippingStatusFixed(
        [Description("The order id, e.g. 1042.")] int orderId)
    {
        if (!Orders.TryGetValue(orderId, out var o))
            return $"No order found with id {orderId}.";

        var daysAgo = Today(0).DayNumber - o.ShippedOn.DayNumber;
        return $"Order {o.Id}: shipped {daysAgo} days ago.";
    }

    [Description("Issue a refund for an order. Only call this after confirming the order qualifies.")]
    public static string IssueRefund(
        [Description("The order id to refund.")] int orderId,
        [Description("The refund amount in USD.")] decimal amount)
    {
        if (!Orders.TryGetValue(orderId, out var o))
            return $"Cannot refund: no order found with id {orderId}.";

        return $"Refund of ${amount:0.00} issued for order {o.Id} ({o.Customer}). Confirmation #RF-{o.Id}.";
    }

    private static DateOnly Today(int offsetDays) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offsetDays);
}
