/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Brokerages.Oanda;
using QuantConnect.Brokerages.Oanda.DataType;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Oanda
{
    [TestFixture, Ignore("This test requires a configured and testable Oanda practice account")]
    public partial class OandaBrokerageTests : BrokerageTests
    {
        /// <summary>
        ///     Creates the brokerage under test and connects it
        /// </summary>
        /// <returns>A connected brokerage instance</returns>
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, IHoldingsProvider holdingsProvider)
        {
            var environment = Config.Get("oanda-environment").ConvertTo<Environment>();
            var accessToken = Config.Get("oanda-access-token");
            var accountId = Config.Get("oanda-account-id").ConvertTo<int>();

            return new OandaBrokerage(orderProvider, holdingsProvider, environment, accessToken, accountId);
        }

        /// <summary>
        ///     Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol
        {
            get { return Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda); }
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
            get { return 5m; }
        }

        /// <summary>
        ///     Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice
        {
            get { return 0.32m; }
        }

        /// <summary>
        ///     Gets the current market price of the specified security
        /// </summary>
        protected override decimal GetAskPrice(Symbol symbol)
        {
            var oanda = (OandaBrokerage) Brokerage;
            var quotes = oanda.GetRates(new List<Instrument> { new Instrument { instrument = new OandaSymbolMapper().GetBrokerageSymbol(symbol) } });
            return (decimal)quotes[0].ask;
        }
    }
}
