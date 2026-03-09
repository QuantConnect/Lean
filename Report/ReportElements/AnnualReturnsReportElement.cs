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
using System.Linq;
using Deedle;
using Python.Runtime;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class AnnualReturnsReportElement : ChartReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Create a new plot of annual returns
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public AnnualReturnsReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Generate the annual returns plot using the python libraries.
        /// </summary>
        public override string Render()
        {
            var backtestReturns = ResultsUtil.EquityPoints(_backtest);
            var liveReturns = ResultsUtil.EquityPoints(_live);

            var backtestTime = backtestReturns.Keys.ToList();
            var backtestStrategy = backtestReturns.Values.ToList();

            var liveTime = liveReturns.Keys.ToList();
            var liveStrategy = liveReturns.Values.ToList();

            var base64 = "";
            using (Py.GIL())
            {
                var backtestList = new PyList();
                var liveList = new PyList();

                // We need to set the datetime index first before we resample
                //var backtestSeries = Pandas.Series(backtestStrategy.ToPython());
                var backtestSeries = new Series<DateTime, double>(backtestTime, backtestStrategy);

                // Get the annual returns for the strategy
                // ResampleEquivalence works similarly to Pandas' DataFrame.resample(...) method
                // Here we transform the series to resample to the year's start, then we get the aggregate return from the year.
                // Pandas equivalent:
                //
                // df.pct_change().resample('AS').sum().mul(100)
                var backtestAnnualReturns = backtestSeries.ResampleEquivalence(date => new DateTime(date.Year, 1, 1), agg => agg.TotalReturns() * 100).DropMissing();

                // We need to set the datetime index first before we resample
                var liveSeries = new Series<DateTime, double>(liveTime, liveStrategy);

                // Get the annual returns for the live strategy.
                // Same as above, this is equivalent to:
                //
                // df.pct_change().resample('AS').sum().mul(100)
                var liveAnnualReturns = liveSeries.ResampleEquivalence(date => new DateTime(date.Year, 1, 1), agg => agg.TotalReturns() * 100).DropMissing();

                // Select only the year number and pass it to the plotting library
                backtestList.Append(backtestAnnualReturns.Keys.Select(x => x.Year).ToList().ToPython());
                backtestList.Append(backtestAnnualReturns.Values.ToList().ToPython());
                liveList.Append(liveAnnualReturns.Keys.Select(x => x.Year).ToList().ToPython());
                liveList.Append(liveAnnualReturns.Values.ToList().ToPython());

                base64 = Charting.GetAnnualReturns(backtestList, liveList);
            }

            return base64;
        }
    }
}