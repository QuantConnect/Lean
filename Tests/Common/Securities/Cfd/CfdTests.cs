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

namespace QuantConnect.Tests.Common.Securities.Cfd
{
    [TestFixture]
    public class CfdTests
    {
        [Test]
        public void ConstructorExtractsQuoteCurrency()
        {
            var symbol = Symbol.Create("DE30EUR", SecurityType.Cfd, Market.Oanda);
            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.Utc, TimeZones.NewYork, true, true, true);
            var symbolProperties = new SymbolProperties("Dax German index", "EUR", 1, 1, 1);
            var cfd = new QuantConnect.Securities.Cfd.Cfd(SecurityExchangeHours.AlwaysOpen(config.DataTimeZone), new Cash("EUR", 0, 0), config, symbolProperties, ErrorCurrencyConverter.Instance, RegisteredSecurityDataTypesProvider.Null);
            Assert.AreEqual("EUR", cfd.QuoteCurrency.Symbol);
        }

    }
}
