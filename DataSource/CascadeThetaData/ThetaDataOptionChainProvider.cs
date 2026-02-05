/*
 * Cascade Labs - ThetaData Option Chain Provider
 * Implements IOptionChainProvider to enumerate option contracts from ThetaData API
 */

using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.SubscriptionPlans;

namespace QuantConnect.Lean.DataSource.CascadeThetaData
{
    /// <summary>
    /// Option chain provider that fetches available option contracts from the ThetaData API
    /// </summary>
    public class ThetaDataOptionChainProvider : IOptionChainProvider, IDisposable
    {
        private readonly CascadeThetaDataRestClient _restClient;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThetaDataOptionChainProvider"/> class
        /// </summary>
        public ThetaDataOptionChainProvider()
        {
            var subscriptionPlan = GetSubscriptionPlan();
            _restClient = new CascadeThetaDataRestClient(subscriptionPlan.RateGate!);
            Log.Trace("ThetaDataOptionChainProvider: Initialized");
        }

        /// <summary>
        /// Initializes a new instance with an existing REST client
        /// </summary>
        /// <param name="restClient">The REST client to use for API calls</param>
        public ThetaDataOptionChainProvider(CascadeThetaDataRestClient restClient)
        {
            _restClient = restClient;
            Log.Trace("ThetaDataOptionChainProvider: Initialized with provided REST client");
        }

        /// <summary>
        /// Gets the list of option contracts for a given underlying symbol
        /// </summary>
        /// <param name="symbol">The option or the underlying symbol to get the option chain for</param>
        /// <param name="date">The date for which to request the option chain</param>
        /// <returns>The list of option contracts</returns>
        public IEnumerable<Symbol> GetOptionContractList(Symbol symbol, DateTime date)
        {
            // Get the underlying symbol
            var underlying = symbol.SecurityType.IsOption() ? symbol.Underlying : symbol;

            if (underlying == null)
            {
                Log.Error($"ThetaDataOptionChainProvider: Unable to determine underlying for {symbol}");
                yield break;
            }

            var ticker = underlying.Value;
            Log.Debug($"ThetaDataOptionChainProvider: Fetching option contracts for {ticker} on {date:yyyy-MM-dd}");

            var contractCount = 0;
            foreach (var contract in _restClient.GetOptionContracts(ticker, date))
            {
                // Convert strike from 1/10 cents to dollars
                var strikeDollars = contract.Strike / 1000m;
                var optionRight = contract.Right.ToUpperInvariant() == "C" ? OptionRight.Call : OptionRight.Put;

                var optionSymbol = Symbol.CreateOption(
                    underlying,
                    Market.USA,
                    OptionStyle.American,
                    optionRight,
                    strikeDollars,
                    contract.Expiry);

                contractCount++;
                yield return optionSymbol;
            }

            Log.Debug($"ThetaDataOptionChainProvider: Found {contractCount} option contracts for {ticker}");
        }

        /// <summary>
        /// Gets the subscription plan for the REST client
        /// </summary>
        private static ISubscriptionPlan GetSubscriptionPlan()
        {
            var pricePlan = Config.Get("thetadata-subscription-plan", "Pro");

            return pricePlan.ToLowerInvariant() switch
            {
                "free" => new FreeSubscriptionPlan(),
                "value" => new ValueSubscriptionPlan(),
                "standard" => new StandardSubscriptionPlan(),
                "pro" => new ProSubscriptionPlan(),
                _ => new StandardSubscriptionPlan()
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _restClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
