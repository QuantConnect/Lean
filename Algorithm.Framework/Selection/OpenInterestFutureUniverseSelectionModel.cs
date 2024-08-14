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
using System.Linq;
using NodaTime;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    ///     Selects contracts in a futures universe, sorted by open interest.  This allows the selection to identifiy current
    ///     active contract.
    /// </summary>
    public class OpenInterestFutureUniverseSelectionModel : FutureUniverseSelectionModel
    {
        private readonly int? _chainContractsLookupLimit;
        private readonly IAlgorithm _algorithm;
        private readonly int? _resultsLimit;
        private readonly MarketHoursDatabase _marketHoursDatabase;

        /// <summary>
        ///     Creates a new instance of <see cref="OpenInterestFutureUniverseSelectionModel" />
        /// </summary>
        /// <param name="algorithm">Algorithm</param>
        /// <param name="futureChainSymbolSelector">Selects symbols from the provided future chain</param>
        /// <param name="chainContractsLookupLimit">Limit on how many contracts to query for open interest</param>
        /// <param name="resultsLimit">Limit on how many contracts will be part of the universe</param>
        public OpenInterestFutureUniverseSelectionModel(IAlgorithm algorithm, Func<DateTime, IEnumerable<Symbol>> futureChainSymbolSelector, int? chainContractsLookupLimit = 6,
            int? resultsLimit = 1) : base(TimeSpan.FromDays(1), futureChainSymbolSelector)
        {
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            _algorithm = algorithm;
            _resultsLimit = resultsLimit;
            _chainContractsLookupLimit = chainContractsLookupLimit;
        }

        /// <summary>
        ///     Creates a new instance of <see cref="OpenInterestFutureUniverseSelectionModel" />
        /// </summary>
        /// <param name="algorithm">Algorithm</param>
        /// <param name="futureChainSymbolSelector">Selects symbols from the provided future chain</param>
        /// <param name="chainContractsLookupLimit">Limit on how many contracts to query for open interest</param>
        /// <param name="resultsLimit">Limit on how many contracts will be part of the universe</param>
        public OpenInterestFutureUniverseSelectionModel(IAlgorithm algorithm, PyObject futureChainSymbolSelector, int? chainContractsLookupLimit = 6,
            int? resultsLimit = 1) : this(algorithm, ConvertFutureChainSymbolSelectorToFunc(futureChainSymbolSelector), chainContractsLookupLimit, resultsLimit)
        {
        }

        /// <summary>
        ///     Defines the future chain universe filter
        /// </summary>
        protected override FutureFilterUniverse Filter(FutureFilterUniverse filter)
        {
            return filter.Contracts(FilterByOpenInterest(filter.ToDictionary(x => x, x => _marketHoursDatabase.GetEntry(x.ID.Market, x, x.ID.SecurityType))));
        }

        /// <summary>
        ///     Filters a set of contracts based on open interest.
        /// </summary>
        /// <param name="contracts">Contracts to filter</param>
        /// <returns>Filtered set</returns>
        public IEnumerable<Symbol> FilterByOpenInterest(IReadOnlyDictionary<Symbol, MarketHoursDatabase.Entry> contracts)
        {
            var symbols = new List<Symbol>(_chainContractsLookupLimit.HasValue ? contracts.Keys.OrderBy(x => x.ID.Date).Take(_chainContractsLookupLimit.Value) : contracts.Keys);
            var openInterest = symbols.GroupBy(x => contracts[x]).SelectMany(g => GetOpenInterest(g.Key, g.Select(i => i))).ToDictionary(x => x.Key, x => x.Value);

            if (openInterest.Count == 0)
            {
                _algorithm.Error(
                    $"{nameof(OpenInterestFutureUniverseSelectionModel)}.{nameof(FilterByOpenInterest)}: Failed to get historical open interest, no symbol will be selected."
                );
                return Enumerable.Empty<Symbol>();
            }

            var filtered = openInterest.OrderByDescending(x => x.Value).ThenBy(x => x.Key.ID.Date).Select(x => x.Key);
            if (_resultsLimit.HasValue)
            {
                filtered = filtered.Take(_resultsLimit.Value);
            }

            return filtered;
        }

        private Dictionary<Symbol, decimal> GetOpenInterest(MarketHoursDatabase.Entry marketHours, IEnumerable<Symbol> symbols)
        {
            var current = _algorithm.UtcTime;
            var exchangeHours = marketHours.ExchangeHours;
            var endTime = Instant.FromDateTimeUtc(_algorithm.UtcTime).InZone(exchangeHours.TimeZone).ToDateTimeUnspecified();
            var previousDay = Time.GetStartTimeForTradeBars(exchangeHours, endTime, Time.OneDay, 1, true, marketHours.DataTimeZone);
            var requests = symbols.Select(
                    symbol => new HistoryRequest(
                        previousDay,
                        current,
                        typeof(Tick),
                        symbol,
                        Resolution.Tick,
                        exchangeHours,
                        exchangeHours.TimeZone,
                        null,
                        true,
                        false,
                        DataNormalizationMode.Raw,
                        TickType.OpenInterest
                    )
                )
                .ToArray();
            return _algorithm.HistoryProvider.GetHistory(requests, exchangeHours.TimeZone)
                .Where(s => s.HasData && s.Ticks.Keys.Count > 0)
                .SelectMany(s => s.Ticks.Select(x => new Tuple<Symbol, Tick>(x.Key, x.Value.LastOrDefault())))
                .GroupBy(x => x.Item1)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(i => i.Item2.Time).LastOrDefault().Item2.Value);
        }

        /// <summary>
        /// Converts future chain symbol selector, provided as a Python lambda function, to a managed func
        /// </summary>
        /// <param name="futureChainSymbolSelector">Python lambda function that selects symbols from the provided future chain</param>
        /// <returns>Given Python future chain symbol selector as a func objet</returns>
        /// <exception cref="ArgumentException"></exception>
        private static Func<DateTime, IEnumerable<Symbol>> ConvertFutureChainSymbolSelectorToFunc(PyObject futureChainSymbolSelector)
        {
            if (futureChainSymbolSelector.TryConvertToDelegate(out Func<DateTime, IEnumerable<Symbol>> futureSelector))
            {
                return futureSelector;
            }
            else
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"FutureUniverseSelectionModel.ConvertFutureChainSymbolSelectorToFunc: {futureChainSymbolSelector.Repr()} is not a valid argument.");
                }
            }
        }
    }
}
