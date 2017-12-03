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
using QuantConnect.Brokerages.Bitfinex.Rest;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture, Ignore("This test requires a configured and active account")]
    public class BitfinexBrokerageIntegrationTests : BitfinexBrokerageIntegrationTestsBase
    {

        #region Properties
        protected override Symbol Symbol
        {
            get { return Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitfinex); }
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

        protected override decimal GetDefaultQuantity()
        {
            return 0.01m;
        }
        #endregion


        protected override decimal GetAskPrice(Symbol symbol)
        {
            return 0;
        }


    }
}
