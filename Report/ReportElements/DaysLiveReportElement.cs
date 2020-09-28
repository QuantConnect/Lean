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
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal class DaysLiveReportElement : ReportElement
    {
        private LiveResult _live;

        /// <summary>
        /// Create a new metric describing the number of days an algorithm has been live.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public DaysLiveReportElement(string name, string key, LiveResult live)
        {
            _live = live;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// The generated output string to be injected
        /// </summary>
        public override string Render()
        {
            if (_live == null)
            {
                return "-";
            }

            var equityPoints = ResultsUtil.EquityPoints(_live);
            return (DateTime.UtcNow - equityPoints.First().Key).Days.ToStringInvariant();
        }
    }
}
