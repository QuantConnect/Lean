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
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
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
        public EstimatedCapacityReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        public override string Render()
        {
            var statistics = _backtest?.Statistics;
            string capacityUsd;
            if (statistics == null || !statistics.TryGetValue("Estimated Strategy Capacity", out capacityUsd))
            {
                return "-";
            }

            var capacity = decimal.Parse(capacityUsd.Replace("$", ""), NumberStyles.Any, CultureInfo.InvariantCulture);
            Result = capacity;

            if (capacity == 0m)
            {
                return "-";
            }

            return FormatNumber(capacity);
        }

        private static string FormatNumber(decimal number)
        {
            if (number < 1000)
            {
                return number.ToStringInvariant();
            }

            // Subtract by multiples of 5 to round down to nearest round number
            if (number < 10000)
            {
                return $"{number - 5m:#,.##}K";
            }

            if (number < 100000)
            {
                return $"{number - 50m:#,.#}K";
            }

            if (number < 1000000)
            {
                return $"{number - 500m:#,.}K";
            }

            if (number < 10000000)
            {
                return $"{number - 5000m:#,,.##}M";
            }

            if (number < 100000000)
            {
                return $"{number - 50000m:#,,.#}M";
            }

            if (number < 1000000000)
            {
                return $"{number - 500000m:#,,.}M";
            }

            return $"{number - 5000000m:#,,,.##}B";
        }
    }
}
