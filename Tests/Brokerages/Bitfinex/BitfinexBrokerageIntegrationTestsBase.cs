using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Bitfinex;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using System.Reflection;
using Moq;
using QuantConnect.Brokerages;
using RestSharp;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture, Ignore("This test requires a configured and active account")]
    public class BitfinexBrokerageIntegrationTestsBase : BrokerageTests
    {

        #region Properties
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
            get { return 1m; }
        }

        /// <summary>
        /// Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice
        {
            get { return 0.001m; }
        }

        protected override decimal GetDefaultQuantity()
        {
            return 0.01m;
        }

        protected override Symbol Symbol
        {
            get
            {
                return Symbol.Create("ETHBTC", this.SecurityType, Market.Bitfinex);
            }
        }
        #endregion

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var restClient = new RestClient("https://api.gdax.com");
            var webSocketClient = new WebSocketWrapper();

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.BrokerageModel).Returns(new BitfinexBrokerageModel(AccountType.Cash));

            return new BitfinexBrokerage(Config.Get("gdax-url", "wss://ws-feed.gdax.com"), webSocketClient, restClient, Config.Get("bitfinex-api-key"), Config.Get("bitfinex-api-secret"),
                algorithm.Object);
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            var tick = ((BitfinexBrokerage)this.Brokerage).GetTick(symbol);
            return tick.AskPrice;
        }

        protected override void ModifyOrderUntilFilled(Order order, OrderTestParameters parameters, double secondsTimeout = 90)
        {
            Assert.Pass("Order update not supported");
        }

        //no stop limit support
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
