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
using MoreLinq;
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Packets;
using System.Collections.Generic;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class MarketsReportElement : ReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Get the markets of the strategy.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public MarketsReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// The generated output string to be injected
        /// </summary>
        public override string Render()
        {
            var liveOrders = _live?.Orders?.Values.ToList();
            if (liveOrders == null)
            {
                liveOrders = new List<Order>();
            }

            var orders = new List<Order>();
            var backtestOrders = _backtest?.Orders?.Values;
            if (backtestOrders != null)
            {
                orders = backtestOrders.ToList();
            }

            orders = orders.Union(liveOrders).ToList();

            var securityTypes = orders.DistinctBy(o => o.SecurityType).Select(s => s.SecurityType.ToString()).ToList();
            Result = securityTypes;

            return string.Join(",", securityTypes);
        }
    }
}
