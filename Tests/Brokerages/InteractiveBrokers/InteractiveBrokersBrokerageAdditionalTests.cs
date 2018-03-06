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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IBApi;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    [Ignore("These tests require the IBController and IB TraderWorkstation to be installed.")]
    public class InteractiveBrokersBrokerageAdditionalTests
    {
        private readonly List<Order> _orders = new List<Order>();

        [Test]
        public void StressTestGetUsdConversion()
        {
            var brokerage = GetBrokerage();
            Assert.IsTrue(brokerage.IsConnected);

            // private method testing hack :)
            var method = brokerage.GetType().GetMethod("GetUsdConversion", BindingFlags.NonPublic | BindingFlags.Instance);

            const string currency = "SEK";
            const int count = 20;

            for (var i = 1; i <= count; i++)
            {
                var value = (decimal)method.Invoke(brokerage, new object[] { currency });

                Console.WriteLine(i + " - GetUsdConversion({0}) = {1}", currency, value);

                Assert.IsTrue(value > 0);
            }

            brokerage.Disconnect();
            brokerage.Dispose();
            InteractiveBrokersGatewayRunner.Stop();
        }

        [Test]
        public void TestRateLimiting()
        {
            using (var brokerage = GetBrokerage())
            {
                Assert.IsTrue(brokerage.IsConnected);

                var method = brokerage.GetType().GetMethod("GetContractDetails", BindingFlags.NonPublic | BindingFlags.Instance);

                var contract = new Contract
                {
                    Symbol = "EUR",
                    Exchange = "IDEALPRO",
                    SecType = "CASH",
                    Currency = "USD"
                };
                var parameters = new object[] { contract };

                var result = Parallel.For(1, 100, x =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    var value = (ContractDetails)method.Invoke(brokerage, parameters);
                    stopwatch.Stop();
                    Console.WriteLine($"{DateTime.UtcNow:O} Response time: {stopwatch.Elapsed}");
                });
                while (!result.IsCompleted) Thread.Sleep(1000);
            }

            InteractiveBrokersGatewayRunner.Stop();
        }

        private InteractiveBrokersBrokerage GetBrokerage()
        {
            InteractiveBrokersGatewayRunner.Start(Config.Get("ib-controller-dir"),
                Config.Get("ib-tws-dir"),
                Config.Get("ib-user-name"),
                Config.Get("ib-password"),
                Config.Get("ib-trading-mode"),
                Config.GetBool("ib-use-tws")
                );

            // grabs account info from configuration
            var securityProvider = new SecurityProvider();
            securityProvider[Symbols.USDJPY] = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.USDJPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, false, false),
                new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));

            var brokerage = new InteractiveBrokersBrokerage(new QCAlgorithm(), new OrderProvider(_orders), securityProvider);
            brokerage.Connect();

            return brokerage;
        }
    }
}
