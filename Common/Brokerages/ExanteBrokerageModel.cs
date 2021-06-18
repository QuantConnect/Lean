using QuantConnect.Benchmarks;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;


namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Exante Brokerage Model Implementation for Back Testing.
    /// </summary>
    public class ExanteBrokerageModel : DefaultBrokerageModel
    {
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
    }
}
