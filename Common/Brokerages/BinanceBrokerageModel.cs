using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides Binance specific properties
    /// </summary>
    public class BinanceBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets();

        public BinanceBrokerageModel(AccountType accountType = AccountType.Cash) : base(accountType)
        {
            if (accountType == AccountType.Margin)
            {
                throw new Exception("The Binance brokerage does not currently support Margin trading.");
            }
        }

        /// <summary>
        /// Gets a new buying power model for the security, returning the default model with the security's configured leverage.
        /// For cash accounts, leverage = 1 is used.
        /// Margin trading is not currently supported
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            return new CashBuyingPowerModel();
        }

        /// <summary>
        /// Binance global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            // margin trading is not currently supported by Binance
            return 1m;
        }

        /// <summary>
        /// Provides Binance fee model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new BinanceFeeModel();
        }

        private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets()
        {
            var map = DefaultMarketMap.ToDictionary();
            map[SecurityType.Crypto] = Market.Binance;
            return map.ToReadOnlyDictionary();
        }
    }
}
