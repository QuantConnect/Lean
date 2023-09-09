using System;
using System.Collections.Generic;
using QuantConnect.Benchmarks;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Util;

namespace QuantConnect.Brokerages;

/// <summary>
/// Provides Bybit futures specific properties
/// </summary>
public class BybitFuturesBrokerageModel : BybitBrokerageModel
{
    /// <summary>
    /// Gets a map of the default markets to be used for each security type
    /// </summary>
    public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets(Market.Bybit);
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BybitFuturesBrokerageModel"/> class
    /// </summary>
    /// <param name="accountType">The type of account to be modeled, defaults to <see cref="AccountType.Margin"/></param>
    public BybitFuturesBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
    {
        if (accountType == AccountType.Cash)
        {
            throw new InvalidOperationException(
                $"{SecurityType.CryptoFuture} can only be traded using a {AccountType.Margin} account type");
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
    /// Provides Bybit Futures fee model
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
        // only applies for perpetual futures
        if (security.Symbol.SecurityType == SecurityType.CryptoFuture &&
            security.Symbol.ID.Date == SecurityIdentifier.DefaultDate)
        {
            return new BybitFutureMarginInterestRateModel();
        }

        return base.GetMarginInterestRateModel(security);
    }
    
    private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets(string marketName)
    {
        var map = DefaultMarketMap.ToDictionary();
        map[SecurityType.CryptoFuture] = marketName;
        map[SecurityType.Crypto] = marketName; //Todo bybit futures has pairs which are not available on bybit spot, therefore we're getting conversion errors when running an algo
        return map.ToReadOnlyDictionary();
    }
}
