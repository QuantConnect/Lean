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

using System.Text.RegularExpressions;

namespace QuantConnect.Data
{
    public static class HistoryExtensions
    {
        private static readonly Regex _brokerageHistoryProvider = new("QuantConnect.Lean.Engine.HistoricalData.([a-zA-z]+)HistoryProvider", RegexOptions.Compiled);

        /// <summary>
        /// Helper method to get the brokerage name
        /// </summary>
        public static bool TryGetBrokerageName(string historyProviderName, out string brokerageName)
        {
            brokerageName = null;
            if (historyProviderName != "QuantConnect.Lean.Engine.HistoricalData.BrokerageHistoryProvider"
                && historyProviderName != "QuantConnect.Lean.Engine.HistoricalData.SubscriptionDataReaderHistoryProvider")
            {
                var matches = _brokerageHistoryProvider.Match(historyProviderName);
                if (matches.Success)
                {
                    brokerageName = matches.Groups[1].Value;
                    return true;
                }
            }

            return false;
        }
    }
}
