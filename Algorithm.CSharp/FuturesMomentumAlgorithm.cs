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
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// EMA cross with SP500 E-mini futures
    /// In this example, we demostrate how to trade futures contracts using
    /// a equity to generate the trading signals
    /// It also shows how you can prefilter contracts easily based on expirations.
    /// It also shows how you can inspect the futures chain to pick a specific contract to trade.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="futures" />
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="strategy example" />
    public class FuturesMomentumAlgorithm : QCAlgorithm
    {
        private const decimal _tolerance = 0.001m;
        private const int _fastPeriod = 20;
        private const int _slowPeriod = 60;

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        public bool IsReady { get { return _fast.IsReady && _slow.IsReady; } }
        public bool IsUpTrend { get { return IsReady && _fast > _slow * (1 + _tolerance); } }
        public bool IsDownTrend { get { return IsReady && _fast < _slow * (1 + _tolerance); } }

        public override void Initialize()
        {
            SetStartDate(2016, 1, 1);
            SetEndDate(2016, 8, 18);
            SetCash(100000);
            SetWarmUp(Math.Max(_fastPeriod, _slowPeriod));

            // Adds SPY to be used in our EMA indicators
            var equity = AddEquity("SPY", Resolution.Daily);
            _fast = EMA(equity.Symbol, _fastPeriod, Resolution.Daily);
            _slow = EMA(equity.Symbol, _slowPeriod, Resolution.Daily);

            // Adds the future that will be traded and
            // set our expiry filter for this futures chain
            var future = AddFuture(Futures.Indices.SP500EMini);
            future.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && IsUpTrend)
            {
                foreach (var chain in slice.FutureChains)
                {
                    // find the front contract expiring no earlier than in 90 days
                    var contract = (
                        from futuresContract in chain.Value.OrderBy(x => x.Expiry)
                        where futuresContract.Expiry > Time.Date.AddDays(90)
                        select futuresContract
                        ).FirstOrDefault();

                    // if found, trade it
                    if (contract != null)
                    {
                        MarketOrder(contract.Symbol, 1);
                    }
                }
            }

            if (Portfolio.Invested && IsDownTrend)
            {
                Liquidate();
            }
        }

        public override void OnEndOfDay()
        {
            Plot("Indicator Signal", "EOD", IsDownTrend ? -1 : IsUpTrend ? 1 : 0);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
        }
    }
}