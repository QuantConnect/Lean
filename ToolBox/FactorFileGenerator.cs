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

using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Generates a factor file from a list of splits and dividends for a specified equity
    /// </summary>
    public class FactorFileGenerator
    {
        /// <summary>
        /// Data for this equity at daily resolution
        /// </summary>
        private readonly List<TradeBar> _dailyDataForEquity;

        /// <summary>
        /// The last date in the _dailyEquityData
        /// </summary>
        private readonly DateTime _lastDateFromEquityData;

        /// <summary>
        /// The symbol for which the factor file is being generated
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Constructor for the FactorFileGenerator
        /// </summary>
        /// <param name="symbol">The equity for which the factor file respresents</param>
        /// <param name="pathForDailyEquityData">The path to the daily data for the specified equity</param>
        public FactorFileGenerator(Symbol symbol, string pathForDailyEquityData)
        {
            Symbol = symbol;
            _dailyDataForEquity = ReadDailyEquityData(pathForDailyEquityData);
            _lastDateFromEquityData = _dailyDataForEquity.Last().Time;
        }

        /// <summary>
        /// Create FactorFile instance
        /// </summary>
        /// <param name="dividendSplitList">List of Dividends and Splits</param>
        /// <returns><see cref="FactorFile"/> instance</returns>
        public CorporateFactorProvider CreateFactorFile(List<BaseData> dividendSplitList)
        {
            var orderedDividendSplitQueue = new Queue<BaseData>(
                                        CombineIntraDayDividendSplits(dividendSplitList)
                                            .OrderByDescending(x => x.Time));

            var factorFileRows = new List<CorporateFactorRow>
            {
                // First Factor Row is set far into the future and by definition has 1 for both price and split factors
                new CorporateFactorRow(
                    Time.EndOfTime,
                    priceFactor: 1,
                    splitFactor: 1
                )
            };

            return RecursivlyGenerateFactorFile(orderedDividendSplitQueue, factorFileRows);
        }

        /// <summary>
        /// If dividend and split occur on the same day,
        ///   combine them into IntraDayDividendSplit object
        /// </summary>
        /// <param name="splitDividendList">List of split and dividends</param>
        /// <returns>A list of splits, dividends with intraday split and dividends combined into <see cref="IntraDayDividendSplit"/></returns>
        private static List<BaseData> CombineIntraDayDividendSplits(List<BaseData> splitDividendList)
        {
            var splitDividendCollection = new Collection<BaseData>(splitDividendList);

            var dateKeysLookup = splitDividendCollection.GroupBy(x => x.Time)
                                                .OrderByDescending(x => x.Key)
                                                .Select(group => group)
                                                .ToList();

            var baseDataList = new List<BaseData>();
            foreach (var kvpLookup in dateKeysLookup)
            {
                if (kvpLookup.Count() > 1)
                {
                    // Intraday dividend split found
                    var dividend = kvpLookup.First(x => x.GetType() == typeof(Dividend)) as Dividend;
                    var split = kvpLookup.First(x => x.GetType() == typeof(Split)) as Split;
                    baseDataList.Add(new IntraDayDividendSplit(split, dividend));
                }
                else
                {
                    baseDataList.Add(kvpLookup.First());
                }
            }

            return baseDataList;
        }

        /// <summary>
        /// Recursively generate a <see cref="FactorFile"/>
        /// </summary>
        /// <param name="orderedDividendSplits">Queue of dividends and splits ordered by date</param>
        /// <param name="factorFileRows">The list of factor file rows</param>
        /// <returns><see cref="FactorFile"/> instance</returns>
        private CorporateFactorProvider RecursivlyGenerateFactorFile(Queue<BaseData> orderedDividendSplits, List<CorporateFactorRow> factorFileRows)
        {
            // If there is no more dividends or splits, return
            if (!orderedDividendSplits.Any())
            {
                factorFileRows.Add(CreateLastFactorFileRow(factorFileRows, _dailyDataForEquity.Last().Close));
                return new CorporateFactorProvider(Symbol.ID.Symbol, factorFileRows);
            }

            var nextEvent = orderedDividendSplits.Dequeue();

            // If there is no more daily equity data to use, return
            if (_lastDateFromEquityData > nextEvent.Time)
            {
                decimal initialReferencePrice = 1;
                factorFileRows.Add(CreateLastFactorFileRow(factorFileRows, initialReferencePrice));
                return new CorporateFactorProvider(Symbol.ID.Symbol, factorFileRows);
            }

            var nextFactorFileRow = CalculateNextFactorFileRow(factorFileRows, nextEvent);

            if (nextFactorFileRow != null)
                factorFileRows.Add(nextFactorFileRow);

            return RecursivlyGenerateFactorFile(orderedDividendSplits, factorFileRows);
        }

        /// <summary>
        /// Create the last FileFactorRow.
        /// Represents the earliest date that the daily equity data contains.
        /// </summary>
        /// <param name="factorFileRows">The list of factor file rows</param>
        /// <returns><see cref="CorporateFactorRow"/></returns>
        private CorporateFactorRow CreateLastFactorFileRow(List<CorporateFactorRow> factorFileRows, decimal referencePrice)
        {
            return new CorporateFactorRow(
                _dailyDataForEquity.Last().Time.Date,
                factorFileRows.Last().PriceFactor,
                factorFileRows.Last().SplitFactor,
                referencePrice
            );
        }

        /// <summary>
        /// Calculates the next <see cref="CorporateFactorRow"/>
        /// </summary>
        /// <param name="factorFileRows">The current list of factorFileRows</param>
        /// <param name="nextEvent">The next dividend, split or intradayDividendSplit</param>
        /// <returns>A single factor file row</returns>
        private CorporateFactorRow CalculateNextFactorFileRow(List<CorporateFactorRow> factorFileRows, BaseData nextEvent)
        {
            CorporateFactorRow nextCorporateFactorRow;
            var t = nextEvent.GetType();

            switch (t.Name)
            {
                case "Dividend":
                    nextCorporateFactorRow = CalculateNextDividendFactor(nextEvent, factorFileRows.Last());
                    break;
                case "Split":
                    nextCorporateFactorRow = CalculateNextSplitFactor(nextEvent, factorFileRows.Last());
                    break;
                case "IntraDayDividendSplit":
                    nextCorporateFactorRow = CalculateIntradayDividendSplit((IntraDayDividendSplit)nextEvent, factorFileRows.Last());
                    break;
                default:
                    throw new ArgumentException("Unhandled BaseData type for FactorFileGenerator.");
            }

            return nextCorporateFactorRow;
        }

        /// <summary>
        /// Generates the <see cref="CorporateFactorRow"/> that represents a intraday dividend split.
        /// Applies the dividend first.
        /// </summary>
        /// <param name="intraDayDividendSplit"><see cref="IntraDayDividendSplit"/> instance that holds the intraday dividend and split information</param>
        /// <param name="last">The last <see cref="CorporateFactorRow"/> generated recursivly</param>
        /// <returns><see cref="CorporateFactorRow"/> that represents an intraday dividend and split</returns>
        private CorporateFactorRow CalculateIntradayDividendSplit(IntraDayDividendSplit intraDayDividendSplit, CorporateFactorRow last)
        {
            var row = CalculateNextDividendFactor(intraDayDividendSplit.Dividend, last);
            return CalculateNextSplitFactor(intraDayDividendSplit.Split, row);
        }

        /// <summary>
        /// Calculates the price factor of a <see cref="Dividend"/>
        /// </summary>
        /// <param name="dividend">The next dividend</param>
        /// <param name="previousCorporateFactorRow">The previous <see cref="CorporateFactorRow"/> generated</param>
        /// <returns><see cref="CorporateFactorRow"/> that represents the dividend event</returns>
        private CorporateFactorRow CalculateNextDividendFactor(BaseData dividend, CorporateFactorRow previousCorporateFactorRow)
        {
            var eventDayData = GetDailyDataForDate(dividend.Time);

            // If you don't have the equity data nothing can be calculated
            if (eventDayData == null)
            {
                return null;
            }

            TradeBar previousClosingPrice = FindPreviousTradableDayClosingPrice(eventDayData.Time);

            // adjust the dividend for both price and split factors (!)
            var priceFactor = previousCorporateFactorRow.PriceFactor *
                                (1 - dividend.Value * previousCorporateFactorRow.SplitFactor / previousClosingPrice.Close);

            return new CorporateFactorRow(
                previousClosingPrice.Time,
                priceFactor.RoundToSignificantDigits(7),
                previousCorporateFactorRow.SplitFactor,
                previousClosingPrice.Close
            );
        }

        /// <summary>
        /// Calculates the split factor of a <see cref="Split"/>
        /// </summary>
        /// <param name="split">The next <see cref="Split"/></param>
        /// <param name="previousCorporateFactorRow">The previous <see cref="CorporateFactorRow"/> generated</param>
        /// <returns><see cref="CorporateFactorRow"/>  that represents the split event</returns>
        private CorporateFactorRow CalculateNextSplitFactor(BaseData split, CorporateFactorRow previousCorporateFactorRow)
        {
            var eventDayData = GetDailyDataForDate(split.Time);

            // If you don't have the equity data nothing can be done
            if (eventDayData == null)
            {
                return null;
            }

            TradeBar previousClosingPrice = FindPreviousTradableDayClosingPrice(eventDayData.Time);

            return new CorporateFactorRow(
                    previousClosingPrice.Time,
                    previousCorporateFactorRow.PriceFactor,
                    (previousCorporateFactorRow.SplitFactor / split.Value).RoundToSignificantDigits(6),
                    previousClosingPrice.Close
                );
        }

        /// <summary>
        /// Gets the data for a specified date
        /// </summary>
        /// <param name="date">The current specified date</param>
        /// <returns><see cref="TradeBar"/>representing that date</returns>
        private TradeBar GetDailyDataForDate(DateTime date)
        {
            return _dailyDataForEquity.FirstOrDefault(x => x.Time.Date == date.Date);
        }

        /// <summary>
        /// Gets the data for the previous tradable day
        /// </summary>
        /// <param name="date">The current specified date</param>
        /// <returns>The last tradeble days data</returns>
        private TradeBar FindPreviousTradableDayClosingPrice(DateTime date)
        {
            TradeBar previousDayData = null;
            var lastDateforData = _dailyDataForEquity.Last();

            while (previousDayData == null && date > lastDateforData.EndTime)
            {
                previousDayData = _dailyDataForEquity.FirstOrDefault(x => x.Time == date.AddDays(-1));
                date = date.AddDays(-1);
            }

            return previousDayData;
        }

        /// <summary>
        /// Read the daily equity date from file
        /// </summary>
        /// <param name="pathForDailyEquityData">Path the the daily data</param>
        /// <returns>A list of <see cref="TradeBar"/> read from file</returns>
        private static List<TradeBar> ReadDailyEquityData(string pathForDailyEquityData)
        {
            var dataReader = new LeanDataReader(pathForDailyEquityData);
            var bars = dataReader.Parse();
            return bars.OrderByDescending(x => x.Time)
                         .Select(x => (TradeBar)x)
                         .ToList();
        }

        /// <summary>
        /// Pairs split and dividend data into one type
        /// </summary>
        private class IntraDayDividendSplit : BaseData
        {
            public Split Split { get; }
            public Dividend Dividend { get; }

            public IntraDayDividendSplit(Split split, Dividend dividend)
            {
                if (split == null)
                {
                    throw new ArgumentNullException(nameof(split));
                }

                if (dividend == null)
                {
                    throw new ArgumentNullException(nameof(dividend));
                }

                Split = split;
                Dividend = dividend;
                Time = Split.Time;
            }
        }
    }
}
