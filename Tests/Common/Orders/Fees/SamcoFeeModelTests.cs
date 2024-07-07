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

using System;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    internal class SamcoFeeModelTests
    {
        private Equity _sbininr;
        private readonly IFeeModel _feeModel = new SamcoFeeModel();

        [SetUp]
        public void Initialize()
        {
            var quoteCurrency = new Cash(Currencies.INR, 0, 1);
            var exchangeHours = MarketHoursDatabase
                .FromDataFolder()
                .GetExchangeHours(Market.India, Symbols.SBIN, SecurityType.Equity);
            _sbininr = new Equity(
                Symbols.SBIN,
                exchangeHours,
                quoteCurrency,
                SymbolProperties.GetDefault(Currencies.INR),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            _sbininr.SetMarketPrice(
                new TradeBar(DateTime.Now, Symbols.SBIN, 100m, 100m, 100m, 100m, 1)
            );
        }

        [Test]
        public void ReturnsFeeInQuoteCurrencyInAccountCurrency()
        {
            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(_sbininr, new MarketOrder(_sbininr.Symbol, 1, DateTime.Now))
            );

            Assert.AreEqual(Currencies.INR, fee.Value.Currency);
            Assert.AreEqual(0.02m, fee.Value.Amount);
        }
    }
}
