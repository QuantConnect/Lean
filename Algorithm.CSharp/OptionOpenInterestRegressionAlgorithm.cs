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
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Options Open Interest data regression test.
    /// </summary>
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="regression test" />
    public class OptionOpenInterestRegressionAlgorithm : QCAlgorithm
    {
        private const string UnderlyingTicker = "twx";
        public readonly Symbol Underlying = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Equity, Market.USA);
        public readonly Symbol OptionSymbol = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Option, Market.USA);

        public override void Initialize()
        {
            // this test opens position in the first day of trading, lives through stock split (7 for 1), and closes adjusted position on the second day
            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 06);
            SetCash(1000000);

            var equity = AddEquity(UnderlyingTicker);
            var option = AddOption(UnderlyingTicker);

            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);

            option.SetFilter(-10, +10, TimeSpan.Zero, TimeSpan.FromDays(365 * 2));

            // use the underlying equity as the benchmark
            SetBenchmark(equity.Symbol);
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                foreach (var chain in slice.OptionChains)
                {
                    foreach (var contract in chain.Value)
                    {
                        if (contract.Symbol.ID.StrikePrice == 72.5m &&
                            contract.Symbol.ID.OptionRight == OptionRight.Call &&
                            contract.Symbol.ID.Date == new DateTime(2016, 01, 15))
                        {
                            if (slice.Time.Date == new DateTime(2014, 06, 05) && contract.OpenInterest != 50)
                            {
                                throw new Exception("Regression test failed: current open interest was not correctly loaded and is not equal to 50");
                            }
                            if (slice.Time.Date == new DateTime(2014, 06, 06) && contract.OpenInterest != 70)
                            {
                                throw new Exception("Regression test failed: current open interest was not correctly loaded and is not equal to 70");
                            }
                            if (slice.Time.Date == new DateTime(2014, 06, 06))
                            {
                                MarketOrder(contract.Symbol, 1);
                                MarketOnCloseOrder(contract.Symbol, -1);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the evemts</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
        }
    }
}



