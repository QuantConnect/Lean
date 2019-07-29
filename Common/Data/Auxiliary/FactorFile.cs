﻿/*
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
 *
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Represents an entire factor file for a specified symbol
    /// </summary>
    public class FactorFile : IEnumerable<FactorFileRow>
    {
        /// <summary>
        /// Keeping a reversed version is more performant that reversing it each time we need it
        /// </summary>
        private readonly List<DateTime> _reversedFactorFileDates;

        /// <summary>
        /// The factor file data rows sorted by date
        /// </summary>
        public SortedList<DateTime, FactorFileRow> SortedFactorFileData { get; set; }

        /// <summary>
        /// The minimum tradeable date for the symbol
        /// </summary>
        /// <remarks>
        /// Some factor files have INF split values, indicating that the stock has so many splits
        /// that prices can't be calculated with correct numerical precision.
        /// To allow backtesting these symbols, we need to move the starting date
        /// forward when reading the data.
        /// Known symbols: GBSN, JUNI, NEWL
        /// </remarks>
        public DateTime? FactorFileMinimumDate { get; set; }

        /// <summary>
        /// Gets the most recent factor change in the factor file
        /// </summary>
        public DateTime MostRecentFactorChange => _reversedFactorFileDates
            .FirstOrDefault(time => time != Time.EndOfTime);

        /// <summary>
        /// Gets the symbol this factor file represents
        /// </summary>
        public string Permtick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactorFile"/> class.
        /// </summary>
        public FactorFile(string permtick, IEnumerable<FactorFileRow> data, DateTime? factorFileMinimumDate = null)
        {
            Permtick = permtick.LazyToUpper();

            var dictionary = new Dictionary<DateTime, FactorFileRow>();
            foreach (var row in data)
            {
                if (dictionary.ContainsKey(row.Date))
                {
                    Log.Trace(Invariant($"Skipping duplicate factor file row for symbol: {permtick}, date: {row.Date:yyyyMMdd}"));
                    continue;
                }

                dictionary.Add(row.Date, row);
            }
            SortedFactorFileData = new SortedList<DateTime, FactorFileRow>(dictionary);

            _reversedFactorFileDates = new List<DateTime>();
            foreach (var time in SortedFactorFileData.Keys.Reverse())
            {
                _reversedFactorFileDates.Add(time);
            }

            FactorFileMinimumDate = factorFileMinimumDate;
        }

        /// <summary>
        /// Reads a FactorFile in from the <see cref="Globals.DataFolder"/>.
        /// </summary>
        public static FactorFile Read(string permtick, string market)
        {
            DateTime? factorFileMinimumDate;
            return new FactorFile(permtick, FactorFileRow.Read(permtick, market, out factorFileMinimumDate), factorFileMinimumDate);
        }

        /// <summary>
        /// Parses the specified lines as a factor file
        /// </summary>
        public static FactorFile Parse(string permtick, IEnumerable<string> lines)
        {
            DateTime? factorFileMinimumDate;
            return new FactorFile(permtick, FactorFileRow.Parse(lines, out factorFileMinimumDate), factorFileMinimumDate);
        }

        /// <summary>
        /// Gets the price scale factor that includes dividend and split adjustments for the specified search date
        /// </summary>
        public decimal GetPriceScaleFactor(DateTime searchDate)
        {
            decimal factor = 1;
            //Iterate backwards to find the most recent factor:
            foreach (var splitDate in _reversedFactorFileDates)
            {
                if (splitDate.Date < searchDate.Date) break;
                factor = SortedFactorFileData[splitDate].PriceScaleFactor;
            }
            return factor;
        }

        /// <summary>
        /// Gets the split factor to be applied at the specified date
        /// </summary>
        public decimal GetSplitFactor(DateTime searchDate)
        {
            decimal factor = 1;
            //Iterate backwards to find the most recent factor:
            foreach (var splitDate in _reversedFactorFileDates)
            {
                if (splitDate.Date < searchDate.Date) break;
                factor = SortedFactorFileData[splitDate].SplitFactor;
            }
            return factor;
        }

        /// <summary>
        /// Gets price and split factors to be applied at the specified date
        /// </summary>
        public FactorFileRow GetScalingFactors(DateTime searchDate)
        {
            var factors = new FactorFileRow(searchDate, 1m, 1m, 0m);

            // Iterate backwards to find the most recent factors
            foreach (var splitDate in _reversedFactorFileDates)
            {
                if (splitDate.Date < searchDate.Date) break;
                factors = SortedFactorFileData[splitDate];
            }

            return factors;
        }

        /// <summary>
        /// Checks whether or not a symbol has scaling factors
        /// </summary>
        public static bool HasScalingFactors(string permtick, string market)
        {
            // check for factor files
            var path = Path.Combine(Globals.DataFolder, "equity", market, "factor_files", permtick.ToLowerInvariant() + ".csv");
            if (File.Exists(path))
            {
                return true;
            }
            Log.Trace($"FactorFile.HasScalingFactors(): Factor file not found: {permtick}");
            return false;
        }

        /// <summary>
        /// Returns true if the specified date is the last trading day before a dividend event
        /// is to be fired
        /// </summary>
        /// <remarks>
        /// NOTE: The dividend event in the algorithm should be fired at the end or AFTER
        /// this date. This is the date in the file that a factor is applied, so for example,
        /// MSFT has a 31 cent dividend on 2015.02.17, but in the factor file the factor is applied
        /// to 2015.02.13, which is the first trading day BEFORE the actual effective date.
        /// </remarks>
        /// <param name="date">The date to check the factor file for a dividend event</param>
        /// <param name="priceFactorRatio">When this function returns true, this value will be populated
        /// with the price factor ratio required to scale the closing value (pf_i/pf_i+1)</param>
        public bool HasDividendEventOnNextTradingDay(DateTime date, out decimal priceFactorRatio)
        {
            priceFactorRatio = 0;
            var index = SortedFactorFileData.IndexOfKey(date);
            if (index > -1 && index < SortedFactorFileData.Count - 1)
            {
                // grab the next key to ensure it's a dividend event
                var thisRow = SortedFactorFileData.Values[index];
                var nextRow = SortedFactorFileData.Values[index + 1];

                // if the price factors have changed then it's a dividend event
                if (thisRow.PriceFactor != nextRow.PriceFactor)
                {
                    priceFactorRatio = thisRow.PriceFactor/nextRow.PriceFactor;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the specified date is the last trading day before a split event
        /// is to be fired
        /// </summary>
        /// <remarks>
        /// NOTE: The split event in the algorithm should be fired at the end or AFTER this
        /// date. This is the date in the file that a factor is applied, so for example MSFT
        /// has a split on 1999.03.29, but in the factor file the split factor is applied on
        /// 1999.03.26, which is the first trading day BEFORE the actual split date.
        /// </remarks>
        public bool HasSplitEventOnNextTradingDay(DateTime date, out decimal splitFactor)
        {
            splitFactor = 1;
            var index = SortedFactorFileData.IndexOfKey(date);
            if (index > -1 && index < SortedFactorFileData.Count - 1)
            {
                // grab the next key to ensure it's a split event
                var thisRow = SortedFactorFileData.Values[index];
                var nextRow = SortedFactorFileData.Values[index + 1];

                // if the split factors have changed then it's a split event
                if (thisRow.SplitFactor != nextRow.SplitFactor)
                {
                    splitFactor = thisRow.SplitFactor/nextRow.SplitFactor;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Writes this factor file data to an enumerable of csv lines
        /// </summary>
        /// <returns>An enumerable of lines representing this factor file</returns>
        public IEnumerable<string> ToCsvLines()
        {
            foreach (var kvp in SortedFactorFileData)
            {
                yield return kvp.Value.ToCsv();
            }
        }

        /// <summary>
        /// Write the factor file to the correct place in the default Data folder
        /// </summary>
        /// <param name="symbol">The symbol this factor file represents</param>
        public void WriteToCsv(Symbol symbol)
        {
            var filePath = LeanData.GenerateRelativeFactorFilePath(symbol);
            File.WriteAllLines(filePath, ToCsvLines());
        }

        /// <summary>
        /// Gets all of the splits and dividends represented by this factor file
        /// </summary>
        /// <param name="symbol">The symbol to ues for the dividend and split objects</param>
        /// <param name="exchangeHours">Exchange hours used for resolving the previous trading day</param>
        /// <returns>All splits and diviends represented by this factor file in chronological order</returns>
        public List<BaseData> GetSplitsAndDividends(Symbol symbol, SecurityExchangeHours exchangeHours)
        {
            var dividendsAndSplits = new List<BaseData>();
            if (SortedFactorFileData.Count == 0)
            {
                Log.Trace($"{symbol} has no factors!");
                return dividendsAndSplits;
            }

            var futureFactorFileRow = SortedFactorFileData.Last().Value;
            for (var i = SortedFactorFileData.Count - 2; i >= 0 ; i--)
            {
                var row = SortedFactorFileData.Values[i];
                var dividend = row.GetDividend(futureFactorFileRow, symbol, exchangeHours);
                if (dividend.Distribution != 0m)
                {
                    dividendsAndSplits.Add(dividend);
                }

                var split = row.GetSplit(futureFactorFileRow, symbol, exchangeHours);
                if (split.SplitFactor != 1m)
                {
                    dividendsAndSplits.Add(split);
                }

                futureFactorFileRow = row;
            }

            return dividendsAndSplits.OrderBy(d => d.Time.Date).ToList();
        }

        /// <summary>
        /// Creates a new factor file with the specified data applied.
        /// Only <see cref="Dividend"/> and <see cref="Split"/> data types
        /// will be used.
        /// </summary>
        /// <param name="data">The data to apply</param>
        /// <param name="exchangeHours">Exchange hours used for resolving the previous trading day</param>
        /// <returns>A new factor file that incorporates the specified dividend</returns>
        public FactorFile Apply(List<BaseData> data, SecurityExchangeHours exchangeHours)
        {
            if (data.Count == 0)
            {
                return this;
            }

            var factorFileRows = new List<FactorFileRow>();
            var lastEntry = SortedFactorFileData.Last().Value;
            factorFileRows.Add(lastEntry);

            var combinedData = GetSplitsAndDividends(data[0].Symbol, exchangeHours).Concat(data)
                .OrderByDescending(d => d.Time.Date);

            foreach (var datum in combinedData)
            {
                FactorFileRow nextEntry = null;
                var split = datum as Split;
                var dividend = datum as Dividend;
                if (dividend != null)
                {
                    nextEntry = lastEntry.Apply(dividend, exchangeHours);
                    lastEntry = nextEntry;
                }
                else if (split != null)
                {
                    nextEntry = lastEntry.Apply(split, exchangeHours);
                    lastEntry = nextEntry;
                }

                if (nextEntry != null)
                {
                    // overwrite the latest entry -- this handles splits/dividends on the same date
                    if (nextEntry.Date == factorFileRows.Last().Date)
                    {
                        factorFileRows[factorFileRows.Count - 1] = nextEntry;
                    }
                    else
                    {
                        factorFileRows.Add(nextEntry);
                    }
                }
            }

            return new FactorFile(Permtick, factorFileRows, FactorFileMinimumDate);
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<FactorFileRow> GetEnumerator()
        {
            foreach (var kvp in SortedFactorFileData)
            {
                yield return kvp.Value;
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}