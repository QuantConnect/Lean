using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Conversion
{
    public interface IConversionRateProviderFactory
    {
        /// <summary>
        /// Will be CashBook.AccountCurrency most of the times
        /// </summary>
        Cash TargetCurrency { get; }

        /// <summary>
        /// All currencies that we will calculate it's conversion rate into target currency
        /// </summary>
        IReadOnlyList<Cash> SourceCurrencies { get; }

        /// <summary>
        /// All available securities that we will use for calculating conversion rates
        /// </summary>
        IReadOnlyList<Security> SubscribedPairs { get; }

        /// <summary>
        /// All providers that need to be updated actively?
        /// </summary>
        //IReadOnlyList<IConversionRateProvider> ActiveProviders { get; }

        /// <summary>
        /// Set target currency for pricing source currencies
        /// </summary>
        /// <param name="targetCurrency">Target currency</param>
        /// <returns>Returns true if any modification was done inside method, false if no change</returns>
        bool SetTargetCurrency(Cash targetCurrency);

        /// <summary>
        /// Add source currency
        /// </summary>
        /// <param name="cash">Source currency which we want to add</param>
        /// <returns>Returns true if any modification was done inside method, false if no change</returns>
        bool AddSourceCurrency(Cash newSourceCurrency);

        /// <summary>
        /// Remove source currency
        /// </summary>
        /// <param name="cash">Source currency which we want to remove</param>
        /// <returns>Returns true if any modification was done inside method, false if no change</returns>
        bool RemoveSourceCurrency(Cash oldSourceCurrency);

        /// <summary>
        /// Add new currency pair, that will be used in calculation
        /// </summary>
        /// <param name="pair">Pair to be added</param>
        /// <param name="market">Market, if null, then use default</param>
        /// <returns>Returns true if any modification was done inside method, false if no change</returns>
        bool AddPair(Security pair, string market = null);

        /// <summary>
        /// Remove existing currency pair
        /// </summary>
        /// <param name="pair">Pair to be removed</param>
        /// <param name="market">If market null, remove all exchanges from those pairs</param>
        /// <returns>Returns true if any modification was done inside method, false if no change</returns>
        bool RemovePair(Security pair, string market = null);

        // ensure if all needed securities are contained for conversion
        bool EnsureCompleteConversionPath();

        // update the rate provider, for one currency per brokerage
        decimal Update(string brokerage, Security security, decimal LastPrice, decimal Volume24);
        
        // Get price in target currency
        decimal GetConversionRate(Cash cash);

        //ConversionRate GetRate(Cash cash);

        //ConversionRatePath GetPath(Cash from, Cash to);
    }
}