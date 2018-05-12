using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Kraken;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using System.Reflection;
using Moq;
using QuantConnect.Brokerages;
using RestSharp;

namespace QuantConnect.Tests.Brokerages.Kraken
{
    [TestFixture, Ignore("This test requires a configured and active account")]
    public class KrakenBrokerageIntegrationTests : BrokerageTests
    {
        #region Properties
        protected override Symbol Symbol
        {
            get { return Symbol.Create("ETHBTC", SecurityType, Market.Kraken); }
        }

        /// <summary>
        ///     Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType
        {
            get { return SecurityType.Crypto; }
        }

        /// <summary>
        ///     Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        protected override decimal HighPrice
        {
            get { return 0.2m; }
        }

        /// <summary>
        /// Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice
        {
            get { return 0.0001m; }
        }

        protected override decimal GetDefaultQuantity()
        {
            return 0.1m;
        }
        #endregion

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            /* var algorithm = new Mock<IAlgorithm>();
               algorithm.Setup(a => a.BrokerageModel).Returns(new KrakenBrokerageModel(AccountType.Cash)); */

            string apiKey    = Config.Get("Kraken-api-key");
            string apiSecret = Config.Get("Kraken-api-secret");

            return new KrakenBrokerage(apiKey, apiSecret);
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            var tick = ((KrakenBrokerage) this.Brokerage).GetTick(symbol);
            return tick.AskPrice;
        }

        protected override void ModifyOrderUntilFilled(Order order, OrderTestParameters parameters, double secondsTimeout = 90)
        {
            Assert.Pass("Order update not supported");
        }

        // no stop limit support
        public override TestCaseData[] OrderParameters
        {
            get
            {
                return new[]
                {
                    new TestCaseData(new MarketOrderTestParameters(Symbol)).SetName("MarketOrder"),
                    new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("LimitOrder"),
                    new TestCaseData(new StopMarketOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("StopMarketOrder"),
                };
            }
        }

    }
}
