using System;
using QuantConnect.Benchmarks;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;
using static QuantConnect.Util.SecurityExtensions;


namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Exante Brokerage Model Implementation for Back Testing.
    /// </summary>
    public class ExanteBrokerageModel : DefaultBrokerageModel
    {
        private const decimal _equityLeverage = 1.2m;

        /// <inheritdoc />
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.Bitfinex);
            return SecurityBenchmark.CreateInstance(securities, symbol);
        }

        /// <inheritdoc />
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;

            if (security.Type != SecurityType.Forex &&
                security.Type != SecurityType.Equity &&
                security.Type != SecurityType.Index &&
                security.Type != SecurityType.Option &&
                security.Type != SecurityType.Future &&
                security.Type != SecurityType.Cfd &&
                security.Type != SecurityType.Crypto &&
                security.Type != SecurityType.Index)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant(
                        $"The {nameof(ExanteBrokerageModel)} does not support {security.Type} security type.")
                );
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override IFeeModel GetFeeModel(Security security)
        {
            return new ExanteFeeModel(
                forexCommissionRate: 0.25m
            );
        }

        /// <summary>
        /// Exante global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            if (AccountType == AccountType.Cash || security.IsInternalFeed() || security.Type == SecurityType.Base)
            {
                return 1m;
            }

            if (security.Type == SecurityType.Equity)
            {
                return _equityLeverage;
            }

            throw new ArgumentException($"Invalid security type: {security.Type}", nameof(security));
        }
    }
}
