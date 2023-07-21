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
using Python.Runtime;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Statistics;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class ReturnsPerTradeReportElement : ChartReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Create a new distribution plot of returns per trade
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public ReturnsPerTradeReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Generate the returns per trade plot using the python libraries.
        /// </summary>
        public override string Render()
        {
            var backtestPercentagePerTrade = new List<double>();
            if (_backtest?.TotalPerformance?.ClosedTrades != null)
            {
                foreach (var trade in _backtest.TotalPerformance.ClosedTrades)
                {
                    if (trade.EntryPrice == 0m)
                    {
                        Log.Error($"ReturnsPerTradeReportElement.Render(): Encountered entry price of 0 in trade with entry time: {trade.EntryTime:yyyy-MM-dd HH:mm:ss} - Exit time: {trade.ExitTime:yyyy-MM-dd HH::mm:ss}");
                        continue;
                    }

                    var sideMultiplier = trade.Direction == TradeDirection.Long ? 1 : -1;
                    backtestPercentagePerTrade.Add(sideMultiplier * (Convert.ToDouble(trade.ExitPrice) - Convert.ToDouble(trade.EntryPrice)) / Convert.ToDouble(trade.EntryPrice));
                }
            }

            // TODO: LiveResult does not contain a TotalPerformance field, so skip live mode for now

            var base64 = "";
            using (Py.GIL())
            {
                // Charting library does not expect values to be in whole percentage values (i.e. not 1% == 1.0, but rather 1% == 0.01),
                base64 = Charting.GetReturnsPerTrade(backtestPercentagePerTrade.ToPython());
            }

            return base64;
        }
    }
}
