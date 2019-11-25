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

using System.IO;
using System.Linq;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class EstimatedCapacityReportElement : ReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Create a new capacity estimate
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public EstimatedCapacityReportElement(string name, string key, BacktestResult backtest, LiveResult live)
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
            var unit = "";
            var capacity = _backtest.Orders.Values.Sum(x => x.Value);

            if (capacity > 1e9m)
            {
                unit = "B";
                capacity /= 1e9m;
            }
            else if (capacity > 1e6m)
            {
                unit = "M";
                capacity /= 1e6m;
            }
            else if (capacity > 1e4m)
            {
                unit = "K";
                capacity /= 1e3m;
            }

            return $"{capacity:C0}{unit}";
        }
    }
}