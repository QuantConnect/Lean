using System;
using QuantConnect.Benchmarks;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;

namespace QuantConnect.Brokerages;

public class BybitFuturesBrokerageModel : BybitBrokerageModel
{
    public BybitFuturesBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
    {
        if (accountType == AccountType.Cash)
        {
            throw new InvalidOperationException($"{SecurityType.CryptoFuture} can only be traded using a {AccountType.Margin} account type");
        }
    }
    
    /// <summary>
    /// Get the benchmark for this model
    /// </summary>
    /// <param name="securities">SecurityService to create the security with if needed</param>
    /// <returns>The benchmark for this brokerage</returns>
    public override IBenchmark GetBenchmark(SecurityManager securities)
    {
        var symbol = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, MarketName);
        return SecurityBenchmark.CreateInstance(securities, symbol);
    }
    
    /// <summary>
    /// Provides Binance Futures fee model
    /// </summary>
    /// <param name="security">The security to get a fee model for</param>
    /// <returns>The new fee model for this brokerage</returns>
    public override IFeeModel GetFeeModel(Security security)
    {
        return new BybitFuturesFeeModel();
    }
    /// <summary>
    /// Gets a new margin interest rate model for the security
    /// </summary>
    /// <param name="security">The security to get a margin interest rate model for</param>
    /// <returns>The margin interest rate model for this brokerage</returns>
    public override IMarginInterestRateModel GetMarginInterestRateModel(Security security)
    {
        //todo is there something else needed to support this?
        // only applies for perpetual futures
        if (security.Symbol.SecurityType == SecurityType.CryptoFuture && security.Symbol.ID.Date == SecurityIdentifier.DefaultDate)
        {
           return new BybitFutureMarginInterestRateModel();
        }
        return base.GetMarginInterestRateModel(security);
    }
}
