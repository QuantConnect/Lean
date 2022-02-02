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
using QuantConnect.Brokerages.Binance;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;
using System.Threading;

namespace QuantConnect.Tests.Brokerages.Binance
{
    [TestFixture]
    public partial class BinanceBrokerageTests
    {
        private static readonly Symbol XRP_USDT = Symbol.Create("XRPUSDT", SecurityType.Crypto, Market.FTX);

        private static TestCaseData[] TestParameters
        {
            get
            {
                return new[]
                {
                    // valid parameters, for example
                    new TestCaseData(XRP_USDT, Resolution.Tick, false),
                    new TestCaseData(XRP_USDT, Resolution.Minute, false),
                    new TestCaseData(XRP_USDT, Resolution.Second, false),
                };
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void StreamsData(Symbol symbol, Resolution resolution, bool throwsException)
        {
            var cancelationToken = new CancellationTokenSource();
            var brokerage = (BinanceBrokerage)Brokerage;

            SubscriptionDataConfig[] configs;
            if (resolution == Resolution.Tick)
            {
                var tradeConfig = new SubscriptionDataConfig(GetSubscriptionDataConfig<Tick>(symbol, resolution),
                    tickType: TickType.Trade);
                var quoteConfig = new SubscriptionDataConfig(GetSubscriptionDataConfig<Tick>(symbol, resolution),
                    tickType: TickType.Quote);
                configs = new[] { tradeConfig, quoteConfig };
            }
            else
            {
                configs = new[]
                {
                    GetSubscriptionDataConfig<QuoteBar>(symbol, resolution),
                    GetSubscriptionDataConfig<TradeBar>(symbol, resolution)
                };
            }

            foreach (var config in configs)
            {
                ProcessFeed(brokerage.Subscribe(config, (s, e) =>
                    {
                    }),
                    cancelationToken,
                    (baseData) =>
                    {
                        if (baseData != null)
                        {
                            Log.Trace("{baseData}");
                        }
                    });
            }

            Thread.Sleep(20000);

            foreach (var config in configs)
            {
                brokerage.Unsubscribe(config);
            }

            Thread.Sleep(20000);

            cancelationToken.Cancel();
        }
    }
}
