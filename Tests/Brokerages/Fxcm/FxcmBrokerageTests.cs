using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Brokerages.Fxcm;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Fxcm
{
    [TestFixture, Ignore("These tests require a configured and active FXCM practice account")]
    public class FxcmBrokerageTests : BrokerageTests
    {
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, IHoldingsProvider holdingsProvider)
        {
            var server = Config.Get("fxcm-server");
            var terminal = Config.Get("fxcm-terminal");
            var userName = Config.Get("fxcm-user-name");
            var password = Config.Get("fxcm-password");

            return new FxcmBrokerage(orderProvider, server, terminal, userName, password);
        }

        protected override string Symbol
        {
            get { return "EURUSD"; }
        }

        protected override SecurityType SecurityType
        {
            get { return SecurityType.Forex; }
        }

        protected override decimal HighPrice
        {
            // FXCM requires order prices to be not more than 5600 pips from the market price (at least for EURUSD)
            get { return 1.5m; }
        }

        protected override decimal LowPrice
        {
            // FXCM requires order prices to be not more than 5600 pips from the market price (at least for EURUSD)
            get { return 0.7m; }
        }

        protected override decimal GetAskPrice(string symbol, SecurityType securityType)
        {
            var brokerage = (FxcmBrokerage)Brokerage;
            var quotes = brokerage.GetQuotes(new List<string> { brokerage.ConvertSymbolToFxcmSymbol(symbol) });
            return (decimal)quotes.Single().getAskClose();
        }

        protected override int GetDefaultQuantity()
        {
            // FXCM requires this minimum for Forex instruments
            return 1000;
        }
    }
}
