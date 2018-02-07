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

using NUnit.Framework;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    [Ignore("This test requires a configured and active account")]
    public class BitfinexBrokerageIntegrationTests : BitfinexBrokerageIntegrationTestsBase
    {
        #region Properties

        protected override Symbol Symbol => Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitfinex);

        /// <summary>
        ///     Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        protected override decimal HighPrice => 2000m;

        /// <summary>
        ///     Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice => 100m;

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