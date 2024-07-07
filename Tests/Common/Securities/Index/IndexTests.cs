﻿/*
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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities.Index
{
    [TestFixture]
    public class IndexTests
    {
        [Test]
        public void ConstructorExtractsQuoteCurrency()
        {
            var symbol = Symbol.Create("SPX", SecurityType.Index, Market.USA);
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                symbol,
                Resolution.Minute,
                TimeZones.Utc,
                TimeZones.NewYork,
                true,
                true,
                true
            );
            var symbolProperties = new SymbolProperties(
                "S&P 500 index",
                "USD",
                1,
                1,
                1,
                string.Empty
            );
            var index = new QuantConnect.Securities.Index.Index(
                SecurityExchangeHours.AlwaysOpen(config.DataTimeZone),
                new Cash("USD", 0, 0),
                config,
                symbolProperties,
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            Assert.AreEqual("USD", index.QuoteCurrency.Symbol);
        }
    }
}
