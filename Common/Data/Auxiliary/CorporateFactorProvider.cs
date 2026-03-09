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
*
*/

using System;
using System.Linq;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Corporate related factor provider. Factors based on splits and dividends
    /// </summary>
    public class CorporateFactorProvider : FactorFile<CorporateFactorRow>
    {
        /// <summary>
        ///Creates a new instance
        /// </summary>
        public CorporateFactorProvider(string permtick, IEnumerable<CorporateFactorRow> data, DateTime? factorFileMinimumDate = null) : base(permtick, data, factorFileMinimumDate)
        {
        }

        /// <summary>
        /// Gets the price scale factor that includes dividend and split adjustments for the specified search date
        /// </summary>
        public override decimal GetPriceFactor(DateTime searchDate, DataNormalizationMode dataNormalizationMode, DataMappingMode? dataMappingMode = null, uint contractOffset = 0)
        {
            if (dataNormalizationMode == DataNormalizationMode.Raw)
            {
                return 0;
            }

            var factor = 1m;

            for (var i = 0; i < ReversedFactorFileDates.Count; i++)
            {
                var factorDate = ReversedFactorFileDates[i];
                if (factorDate.Date < searchDate.Date)
                {
                    break;
                }

                var factorFileRow = SortedFactorFileData[factorDate];
                switch (dataNormalizationMode)
                {
                    case DataNormalizationMode.TotalReturn:
                    case DataNormalizationMode.SplitAdjusted:
                        factor = factorFileRow.First().SplitFactor;
                        break;
                    case DataNormalizationMode.Adjusted:
                    case DataNormalizationMode.ScaledRaw:
                        factor = factorFileRow.First().PriceScaleFactor;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return factor;
        }

        /// <summary>
        /// Gets price and split factors to be applied at the specified date
        /// </summary>
        public CorporateFactorRow GetScalingFactors(DateTime searchDate)
        {
            var factors = new CorporateFactorRow(searchDate, 1m, 1m, 0m);

            // Iterate backwards to find the most recent factors
            foreach (var splitDate in ReversedFactorFileDates)
            {
                if (splitDate.Date < searchDate.Date) break;
                factors = SortedFactorFileData[splitDate][0];
            }

            return factors;
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
        /// <param name="referencePrice">When this function returns true, this value will be populated
        /// with the reference raw price, which is the close of the provided date</param>
        public bool HasDividendEventOnNextTradingDay(DateTime date, out decimal priceFactorRatio, out decimal referencePrice)
        {
            priceFactorRatio = 0;
            referencePrice = 0;
            var index = SortedFactorFileData.IndexOfKey(date);
            if (index > -1 && index < SortedFactorFileData.Count - 1)
            {
                // grab the next key to ensure it's a dividend event
                var thisRow = SortedFactorFileData.Values[index].First();
                var nextRow = SortedFactorFileData.Values[index + 1].First();

                // if the price factors have changed then it's a dividend event
                if (thisRow.PriceFactor != nextRow.PriceFactor)
                {
                    priceFactorRatio = thisRow.PriceFactor / nextRow.PriceFactor;
                    referencePrice = thisRow.ReferencePrice;
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
        /// <param name="date">The date to check the factor file for a split event</param>
        /// <param name="splitFactor">When this function returns true, this value will be populated
        /// with the split factor ratio required to scale the closing value</param>
        /// <param name="referencePrice">When this function returns true, this value will be populated
        /// with the reference raw price, which is the close of the provided date</param>
        public bool HasSplitEventOnNextTradingDay(DateTime date, out decimal splitFactor, out decimal referencePrice)
        {
            splitFactor = 1;
            referencePrice = 0;
            var index = SortedFactorFileData.IndexOfKey(date);
            if (index > -1 && index < SortedFactorFileData.Count - 1)
            {
                // grab the next key to ensure it's a split event
                var thisRow = SortedFactorFileData.Values[index].First();
                var nextRow = SortedFactorFileData.Values[index + 1].First();

                // if the split factors have changed then it's a split event
                if (thisRow.SplitFactor != nextRow.SplitFactor)
                {
                    splitFactor = thisRow.SplitFactor / nextRow.SplitFactor;
                    referencePrice = thisRow.ReferencePrice;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets all of the splits and dividends represented by this factor file
        /// </summary>
        /// <param name="symbol">The symbol to ues for the dividend and split objects</param>
        /// <param name="exchangeHours">Exchange hours used for resolving the previous trading day</param>
        /// <param name="decimalPlaces">The number of decimal places to round the dividend's distribution to, defaulting to 2</param>
        /// <returns>All splits and dividends represented by this factor file in chronological order</returns>
        public List<BaseData> GetSplitsAndDividends(Symbol symbol, SecurityExchangeHours exchangeHours, int decimalPlaces = 2)
        {
            var dividendsAndSplits = new List<BaseData>();
            if (SortedFactorFileData.Count == 0)
            {
                Log.Trace($"{symbol} has no factors!");
                return dividendsAndSplits;
            }

            var futureFactorFileRow = SortedFactorFileData.Last().Value.First();
            for (var i = SortedFactorFileData.Count - 2; i >= 0; i--)
            {
                var row = SortedFactorFileData.Values[i].First();
                var dividend = row.GetDividend(futureFactorFileRow, symbol, exchangeHours, decimalPlaces);
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
        public CorporateFactorProvider Apply(List<BaseData> data, SecurityExchangeHours exchangeHours)
        {
            if (data.Count == 0)
            {
                return this;
            }

            var factorFileRows = new List<CorporateFactorRow>();
            var firstEntry = SortedFactorFileData.First().Value.First();
            var lastEntry = SortedFactorFileData.Last().Value.First();
            factorFileRows.Add(lastEntry);

            var splitsAndDividends = GetSplitsAndDividends(data[0].Symbol, exchangeHours);

            var combinedData = splitsAndDividends.Concat(data)
                .DistinctBy(e => $"{e.GetType().Name}{e.Time.ToStringInvariant(DateFormat.EightCharacter)}")
                .OrderByDescending(d => d.Time.Date);

            foreach (var datum in combinedData)
            {
                CorporateFactorRow nextEntry = null;
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

            var firstFactorFileRow = new CorporateFactorRow(firstEntry.Date, factorFileRows.Last().PriceFactor, factorFileRows.Last().SplitFactor, firstEntry.ReferencePrice == 0 ? 0 : firstEntry.ReferencePrice);
            var existing = factorFileRows.FindIndex(row => row.Date == firstFactorFileRow.Date);
            if (existing == -1)
            {
                // only add it if not present
                factorFileRows.Add(firstFactorFileRow);
            }

            return new CorporateFactorProvider(Permtick, factorFileRows, FactorFileMinimumDate);
        }
    }
}
