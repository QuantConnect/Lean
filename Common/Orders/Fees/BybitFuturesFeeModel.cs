namespace QuantConnect.Orders.Fees;

/// <summary>
/// Bybit futures fee model implementation
/// </summary>
public class BybitFuturesFeeModel : BybitFeeModel
{
    /// <summary>
    /// Tier 1 maker fees
    /// https://learn.bybit.com/bybit-guide/bybit-trading-fees/
    /// </summary>
    public new const decimal MakerNonVIPFee = 0.0002m;

    /// <summary>
    /// Tier 1 taker fees
    /// https://learn.bybit.com/bybit-guide/bybit-trading-fees/
    /// </summary>
    public new const decimal TakerNonVIPFee = 0.00055m;

    /// <summary>
    /// Initializes a new instance of the <see cref="BybitFuturesFeeModel"/> class
    /// </summary>
    /// <param name="makerFee">The accounts maker fee</param>
    /// <param name="takerFee">The accounts taker fee</param>
    public BybitFuturesFeeModel(decimal makerFee = MakerNonVIPFee, decimal takerFee = TakerNonVIPFee)
        : base(makerFee, takerFee)
    {
    }
}
