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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This is an option split regression algorithm
    /// </summary>
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="regression test" />
    public class OptionRenameRegressionAlgorithm : QCAlgorithm
    {
        private const string UnderlyingTicker = "FOXA";
        public readonly Symbol Underlying = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Equity, Market.USA);
        public readonly Symbol OptionSymbol = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Option, Market.USA);

        public override void Initialize()
        {
            // this test opens position in the first day of trading, lives through stock rename (NWSA->FOXA), dividends, and closes adjusted position on the third day
            SetStartDate(2013, 06, 28);
            SetEndDate(2013, 07, 02);
            SetCash(1000000);

            var equity = AddEquity(UnderlyingTicker);
            var option = AddOption(UnderlyingTicker);

            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);

            // set our strike/expiry filter for this option chain
            option.SetFilter(-1, +1, TimeSpan.Zero, TimeSpan.MaxValue);

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
                if (Time.Day == 28 && Time.Hour > 9 && Time.Minute > 0)
                {
                    OptionChain chain;
                    if (slice.OptionChains.TryGetValue(OptionSymbol, out chain))
                    {
                        var contract =
                            chain.OrderBy(x => x.Expiry)
                            .Where(x => x.Right == OptionRight.Call && x.Strike == 33 && x.Expiry.Date == new DateTime(2013, 08, 17))
                            .FirstOrDefault();

                        if (contract != null)
                        {
                            // Buying option
                            Buy(contract.Symbol, 1);

                            // Buying the underlying stock
                            var underlyingSymbol = contract.Symbol.Underlying;
                            Buy(underlyingSymbol, 100);

                            // checks
                            if (contract.AskPrice != 1.1m)
                            {
                                throw new Exception("Regression test failed: current ask price was not loaded from NWSA backtest file and is not $1.1");
                            }
                        }
                    }
                }
            }
            else
            {
                if (Time.Day == 2 && Time.Hour > 14 && Time.Minute > 0)
                {
                    // selling positions
                    Liquidate();

                    // checks
                    OptionChain chain;
                    if (slice.OptionChains.TryGetValue(OptionSymbol, out chain))
                    {
                        var contract =
                            chain.OrderBy(x => x.Expiry)
                            .Where(x => x.Right == OptionRight.Call && x.Strike == 33 && x.Expiry.Date == new DateTime(2013, 08, 17))
                            .FirstOrDefault();

                        if (contract.BidPrice != 0.05m)
                        {
                            throw new Exception("Regression test failed: current bid price was not loaded from FOXA file and is not $0.05");
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
            Log(orderEvent.ToString());
        }
    }
}



