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
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;
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

        /// <summary>
        /// Split <see cref="HistoryRequest"/> on several request with update mapped symbol.
        /// </summary>
        /// <param name="request">Represents historical data requests</param>
        /// <param name="mapFileProvider">Provides instances of <see cref="MapFileResolver"/> at run time</param>
        /// <returns>
        /// Return HistoryRequests with different <see cref="BaseDataRequest.StartTimeUtc"/> - <seealso cref="BaseDataRequest.EndTimeUtc"/> range
        /// and <seealso cref="Symbol.Value"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mapFileProvider"/> is null.</exception>
        /// <example>
        /// For instances:
        /// request = { StartTimeUtc = 2013/01/01, EndTimeUtc = 2017/02/02, Symbol = "GOOGL" }  split request on:
        /// 1: request = { StartTimeUtc = 2013/01/01, EndTimeUtc = 2014/04/02, Symbol.Value = "GOOG" }
        /// 2: request = { StartTimeUtc = 2014/04/**03**, EndTimeUtc = 2017/02/02, Symbol.Value = "GOOGL" }
        /// > GOOGLE: IPO: August 19, 2004 Name = GOOG then it was restructured: from "GOOG" to "GOOGL" on April 2, 2014
        /// </example>
        public static IEnumerable<HistoryRequest> SplitHistoryRequestWithUpdatedMappedSymbol(this HistoryRequest request, IMapFileProvider mapFileProvider)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Symbol.SecurityType != SecurityType.Future && request.Symbol.RequiresMapping())
            {
                var isReturnHistoryRequest = default(bool);
                foreach (var tickerDateRange in mapFileProvider.RetrieveSymbolHistoricalDefinitionsInDateRange(request.Symbol, request.StartTimeLocal, request.EndTimeLocal))
                {
                    isReturnHistoryRequest = true;
                    var symbol = request.Symbol.UpdateMappedSymbol(tickerDateRange.Ticker);
                    yield return new HistoryRequest(
                        request,
                        symbol,
                        tickerDateRange.StartDateTimeLocal.ConvertToUtc(request.ExchangeHours.TimeZone),
                        tickerDateRange.EndDateTimeLocal.ConvertToUtc(request.ExchangeHours.TimeZone)
                        );
                }

                if (!isReturnHistoryRequest)
                {
                    yield return request;
                }
            }
            else
            {
                yield return request;
            }
        }
    }
}
