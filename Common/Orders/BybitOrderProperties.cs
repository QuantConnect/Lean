namespace QuantConnect.Orders;

public class BybitOrderProperties : OrderProperties
{
    
    /// <summary>
    /// This flag will ensure the order executes only as a maker (no fee) order.
    /// If part of the order results in taking liquidity rather than providing,
    /// it will be rejected and no part of the order will execute.
    /// Note: this flag is only applied to Limit orders.
    /// </summary>
    public bool PostOnly { get; set; }
}
