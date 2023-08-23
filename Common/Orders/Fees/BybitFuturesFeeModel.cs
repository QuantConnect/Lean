namespace QuantConnect.Orders.Fees;

public class BybitFuturesFeeModel : BybitFeeModel
{
    /// <summary>
    /// Tier 1 maker fees
    /// https://learn.bybit.com/bybit-guide/bybit-trading-fees/
    /// </summary>
    public const decimal MakerNoinVIPFee = 0.0002m;

    /// <summary>
    /// Tier 1 taker fees
    /// https://learn.bybit.com/bybit-guide/bybit-trading-fees/
    /// </summary>
    public const decimal TakerNonVIPFee = 0.00055m;

    public BybitFuturesFeeModel(decimal makerFee = MakerNoinVIPFee, decimal takerFee = TakerNonVIPFee) : base(makerFee,
        takerFee)
    {
        
    }
}
