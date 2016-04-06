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
using TradingApi.ModelObjects.Bitfinex.Json;
using QuantConnect.Orders;
using System.Reflection;
using Moq;
using TradingApi.Bitfinex;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture, Ignore("This test requires a configured and active account")]
    public class BitfinexBrokerageTests : BrokerageTests
    {

        #region Properties
        protected override Symbol Symbol
        {
            get { return Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitfinex); }
        }

        /// <summary>
        ///     Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType
        {
            get { return SecurityType.Forex; }
        }

        /// <summary>
        ///     Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        protected override decimal HighPrice
        {
            get { return 2000m; }
        }

        /// <summary>
        ///     Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice
        {
            get { return 100m; }
        }
        #endregion

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            string apiSecret = Config.Get("bitfinex-api-secret");
            string apiKey = Config.Get("bitfinex-api-key");
            string wallet = Config.Get("bitfinex-wallet");
            decimal scaleFactor = decimal.Parse(Config.Get("bitfinex-scale-factor", "1"));
            string url = Config.Get("bitfinex-wss", "wss://api2.bitfinex.com:3000/ws");
            var restClient = new BitfinexApi(apiSecret, apiKey);
            var webSocketClient = new WebSocketWrapper();
            return new BitfinexWebsocketsBrokerage(url, webSocketClient, apiKey, apiSecret, wallet, restClient, scaleFactor, securityProvider);
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            return 0;
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
