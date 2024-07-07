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

using System.Globalization;
using System.Linq;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    /// <summary>
    /// Capacity Estimation Report Element
    /// </summary>
    public sealed class EstimatedCapacityReportElement : ReportElement
    {
        private readonly BacktestResult _backtest;
        private readonly LiveResult _live;

        /// <summary>
        /// Create a new capacity estimate
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public EstimatedCapacityReportElement(
            string name,
            string key,
            BacktestResult backtest,
            LiveResult live
        )
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Render element
        /// </summary>
        public override string Render()
        {
            var statistics = _backtest?.Statistics;
            string capacityWithCurrency;
            if (
                statistics == null
                || !statistics.TryGetValue("Estimated Strategy Capacity", out capacityWithCurrency)
            )
            {
                return "-";
            }

            var capacity = Currencies.Parse(capacityWithCurrency).RoundToSignificantDigits(2);

            Result = capacity;

            if (capacity == 0m)
            {
                return "-";
            }

            return capacity.ToFinancialFigures();
        }
    }
}
