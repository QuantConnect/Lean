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

using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to implement a cross moving average for the futures front contract
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="indicator" />
    /// <meta name="tag" content="futures" />
    public class EmaCrossFuturesFrontMonthAlgorithm : QCAlgorithm
    {
        private const decimal _tolerance = 0.001m;
        private Symbol _symbol;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private IDataConsolidator _consolidator;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);
            SetCash(1000000);

            var future = AddFuture(Futures.Metals.Gold);

            // Only consider the front month contract
            // Update the universe once per day to improve performance
            future.SetFilter(x => x.FrontMonth().OnlyApplyFilterAtMarketOpen());

            // Create two exponential moving averages
            _fast = new ExponentialMovingAverage(100);
            _slow = new ExponentialMovingAverage(300);

            // Add a custom chart to track the EMA cross
            var chart = new Chart("EMA Cross");
            chart.AddSeries(new Series("Fast", SeriesType.Line, 0));
            chart.AddSeries(new Series("Slow", SeriesType.Line, 0));
            AddChart(chart);
        }

        public override void OnData(Slice slice)
        {
            SecurityHolding holding;
            if (Portfolio.TryGetValue(_symbol, out holding))
            {
                // Buy the futures' front contract when the fast EMA is above the slow one
                if (_fast > _slow * (1 + _tolerance))
                {
                    if (!holding.Invested)
                    {
                        SetHoldings(_symbol, .1);
                        PlotEma();
                    }
                }
                else if (holding.Invested)
                {
                    Liquidate(_symbol);
                    PlotEma();
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.RemovedSecurities.Count > 0)
            {
                // Remove the consolidator for the previous contract
                // and reset the indicators
                if (_symbol != null && _consolidator != null)
                {
                    SubscriptionManager.RemoveConsolidator(_symbol, _consolidator);
                    _fast.Reset();
                    _slow.Reset();
                }
                // We don't need to call Liquidate(_symbol),
                // since its positions are liquidated because the contract has expired.
            }

            // Only one security will be added: the new front contract
            _symbol = changes.AddedSecurities.SingleOrDefault().Symbol;

            // Create a new consolidator and register the indicators to it
            _consolidator = ResolveConsolidator(_symbol, Resolution.Minute);
            RegisterIndicator(_symbol, _fast, _consolidator);
            RegisterIndicator(_symbol, _slow, _consolidator);

            // Warm up the indicators
            WarmUpIndicator(_symbol, _fast, Resolution.Minute);
            WarmUpIndicator(_symbol, _slow, Resolution.Minute);

            PlotEma();
        }

        private void PlotEma()
        {
            Plot("EMA Cross", "Fast", _fast);
            Plot("EMA Cross", "Slow", _slow);
        }
    }
}
