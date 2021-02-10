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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class TradesPerDayReportElement : ReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Estimate the trades per day of the strategy.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public TradesPerDayReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Generate trades per day
        /// </summary>
        public override string Render()
        {
            var liveOrders = _live?.Orders?.Values.ToList();
            if (liveOrders == null)
            {
                liveOrders = new List<Order>();
            }

            var orders = _backtest?.Orders?.Values.Concat(liveOrders).OrderBy(x => x.Time);
            if (orders == null)
            {
                return "-";
            }

            if (!orders.Any())
            {
                return "-";
            }

            var days = orders.Last().Time
                .Subtract(orders.First().Time)
                .TotalDays;

            if (days == 0)
            {
                days = 1;
            }

            var tradesPerDay = orders.Count() / days;
            Result = tradesPerDay;

            if (tradesPerDay > 9)
            {
                return $"{tradesPerDay:F0}";
            }

            return $"{tradesPerDay:F1}";
        }
    }
}
