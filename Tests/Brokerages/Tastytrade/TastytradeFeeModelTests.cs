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
 *
*/

using System;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Orders.Fees;
using System.Collections.Generic;

namespace QuantConnect.Tests.Brokerages.Tastytrade
{
    /// <summary>
    /// Contains unit tests for the <see cref="TastytradeFeeModel"/> to ensure correct fee calculations
    /// for different security types such as equities, options, futures, and future options.
    /// </summary>
    [TestFixture]
    public class TastytradeFeeModelTests
    {
        private TastytradeFeeModel _feeModel;

        /// <summary>
        /// Initializes the <see cref="TastytradeFeeModel"/> instance once before any tests are run.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _feeModel = new TastytradeFeeModel();
        }

        /// <summary>
        /// Provides test cases for fee calculation across various symbols, order quantities,
        /// and holding quantities.
        /// </summary>
        private static IEnumerable<TestCaseData> FeeTestParameters
        {
            get
            {
                var aapl = Symbols.AAPL;
                yield return new(aapl, 1m, 0m, 0m);
                yield return new(aapl, 10m, 0m, 0m);
                var aaplOptionContract = Symbol.CreateOption(aapl, aapl.ID.Market, SecurityType.Option.DefaultOptionStyle(), OptionRight.Call, 200m, new DateTime(2025, 06, 20));
                yield return new(aaplOptionContract, 10m, 0m, 10m);
                yield return new(aaplOptionContract, 10m, 1m, 10m);
                yield return new(aaplOptionContract, 1m, -1m, 0m);
                yield return new(aaplOptionContract, 10m, -10m, 0m);
                yield return new(aaplOptionContract, 11m, -10m, 0m);
                var SP500EMini = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2025, 06, 20));
                yield return new(SP500EMini, 1m, 0m, 1.25m);
                yield return new(SP500EMini, 10m, 1m, 12.5m);
                yield return new(SP500EMini, -10m, 1m, 12.5m);
                var SP500EMini_OptionContract = Symbol.CreateOption(SP500EMini, SP500EMini.ID.Market, SecurityType.FutureOption.DefaultOptionStyle(), OptionRight.Put, 900m, new DateTime(2025, 09, 19));
                yield return new(SP500EMini_OptionContract, 1m, 0m, 2.5m);
                yield return new(SP500EMini_OptionContract, 2m, 0m, 5m);
                yield return new(SP500EMini_OptionContract, 4m, 0m, 10m);
                yield return new(SP500EMini_OptionContract, 4m, 0m, 10m);
                yield return new(SP500EMini_OptionContract, 1m, -1m, 0m);
                yield return new(SP500EMini_OptionContract, 10m, -10m, 0m);
                yield return new(SP500EMini_OptionContract, 11m, -11m, 0m);
                yield return new(SP500EMini_OptionContract, -10m, -10m, 25m);
            }
        }


        [Test, TestCaseSource(nameof(FeeTestParameters))]
        public void CalculateRightOrderFeeBasedOnSecurity(Symbol symbol, decimal orderQuantity, decimal symbolHoldingQuantity, decimal expectedFeeQuantity)
        {
            var parameters = GetOrderFeeParameters(symbol, orderQuantity, symbolHoldingQuantity);

            var fee = _feeModel.GetOrderFee(parameters);

            Assert.AreEqual(expectedFeeQuantity, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        /// <summary>
        /// Creates <see cref="OrderFeeParameters"/> using a <see cref="MarketOrder"/> and given
        /// symbol and holding quantity.
        /// </summary>
        /// <param name="symbol">The symbol of the security being ordered.</param>
        /// <param name="orderQuantity">The order quantity.</param>
        /// <param name="symbolHoldingQuantity">The current holding quantity.</param>
        /// <returns>A populated <see cref="OrderFeeParameters"/> object.</returns>
        private static OrderFeeParameters GetOrderFeeParameters(Symbol symbol, decimal orderQuantity, decimal symbolHoldingQuantity)
        {
            var security = new Security(symbol, null, new Cash("USD", 0m, 1m), SymbolProperties.GetDefault("USD"), null, null, new SecurityCache());
            security.Holdings.SetHoldings(100, symbolHoldingQuantity);

            var marketOrder = new MarketOrder(symbol, orderQuantity, DateTime.UtcNow);
            return new OrderFeeParameters(security, marketOrder);
        }
    }
}
