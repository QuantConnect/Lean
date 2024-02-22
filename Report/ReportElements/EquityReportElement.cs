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

using QuantConnect.Packets;
using System.Linq;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class EquityReportElement : ReportElement
    {
        private readonly Result _result;
        private readonly bool _isStartingMode;

        /// <summary>
        /// Total portfolio value.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        /// <param name="isStartingMode">True, to get the starting total portfolio value.
        /// False, to get the ending total portfolio value</param>
        public EquityReportElement(string name, string key, BacktestResult backtest, LiveResult live, bool isStartingMode)
        {
            Name = name;
            Key= key;
            _result = live != null ? live : backtest;
            _isStartingMode = isStartingMode;
        }

        public override string Render()
        {
            if (_isStartingMode)
            {
                return ResultsUtil.EquityPoints(_result).FirstOrDefault().Value.ToString();
            }
            else
            {
                return ResultsUtil.EquityPoints(_result).LastOrDefault().Value.ToString();
            }
        }
    }
}
