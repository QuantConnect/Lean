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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Generates a factor file from a list of splits and dividends for a specified equity
    /// </summary>
    public class FactorFileGenerator
    {
        /// <summary>
        /// The symbol for which the factor file is being generated
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Data for this equity at daily resolution
        /// </summary>
        private readonly List<TradeBar> _dailyDataForEquity;

        /// <summary>
        /// The last date in the _dailyEquityData
        /// </summary>
        private readonly DateTime _lastDateFromEquityData;


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
        public FactorFile CreateFactorFile(List<BaseData> dividendSplitList)
        {
            var orderedDividendSplitQueue = new Queue<BaseData>(
                                        CombineIntraDayDividendSplits(dividendSplitList)
                                            .OrderByDescending(x => x.Time));

            var factorFileRows = new List<FactorFileRow>
            {
                // First Factor Row is set far into the future and by definition has 1 for both price and split factors
                new FactorFileRow(
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
        private List<BaseData> CombineIntraDayDividendSplits(List<BaseData> splitDividendList)
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
        private FactorFile RecursivlyGenerateFactorFile(Queue<BaseData> orderedDividendSplits, List<FactorFileRow> factorFileRows)
        {
            // If there is no more dividends or splits, return
            if (!orderedDividendSplits.Any())
            {
                factorFileRows.Add(CreateLastFactorFileRow(factorFileRows));
                return new FactorFile(Symbol.ID.Symbol, factorFileRows);
            }

            var nextEvent = orderedDividendSplits.Dequeue();

            // If there is no more daily equity data to use, return
            if (_lastDateFromEquityData > nextEvent.Time)
            {
                factorFileRows.Add(CreateLastFactorFileRow(factorFileRows));
                return new FactorFile(Symbol.ID.Symbol, factorFileRows);
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
        /// <returns><see cref="FactorFileRow"/></returns>
        private FactorFileRow CreateLastFactorFileRow(List<FactorFileRow> factorFileRows)
        {
            return new FactorFileRow(
                _dailyDataForEquity.Last().Time.Date,
                factorFileRows.Last().PriceFactor,
                factorFileRows.Last().SplitFactor
            );
        }

        /// <summary>
        /// Calculates the next <see cref="FactorFileRow"/>
        /// </summary>
        /// <param name="factorFileRows">The current list of factorFileRows</param>
        /// <param name="nextEvent">The next dividend, split or intradayDividendSplit</param>
        /// <returns>A single factor file row</returns>
        private FactorFileRow CalculateNextFactorFileRow(List<FactorFileRow> factorFileRows, BaseData nextEvent)
        {
            FactorFileRow nextFactorFileRow;
            var t = nextEvent.GetType();

            switch (t.Name)
            {
                case "Dividend":
                    nextFactorFileRow = CalculateNextDividendFactor(nextEvent, factorFileRows.Last());
                    break;
                case "Split":
                    nextFactorFileRow = CalculateNextSplitFactor(nextEvent, factorFileRows.Last());
                    break;
                case "IntraDayDividendSplit":
                    nextFactorFileRow = CalculateIntradayDividendSplit((IntraDayDividendSplit)nextEvent, factorFileRows.Last());
                    break;
                default:
                    throw new ArgumentException("Unhandled BaseData type for FactorFileGenerator.");
            }

            return nextFactorFileRow;
        }

        /// <summary>
        /// Generates the <see cref="FactorFileRow"/> that represents a intraday dividend split.
        /// Applies the dividend first.
        /// </summary>
        /// <param name="intraDayDividendSplit"><see cref="IntraDayDividendSplit"/> instance that holds the intraday dividend and split information</param>
        /// <param name="last">The last <see cref="FactorFileRow"/> generated recursivly</param>
        /// <returns><see cref="FactorFileRow"/> that represents an intraday dividend and split</returns>
        private FactorFileRow CalculateIntradayDividendSplit(IntraDayDividendSplit intraDayDividendSplit, FactorFileRow last)
        {
            var row = CalculateNextDividendFactor(intraDayDividendSplit.Dividend, last);
            return CalculateNextSplitFactor(intraDayDividendSplit.Split, row);
        }


        /// <summary>
        /// Calculates the price factor of a <see cref="Dividend"/>
        /// </summary>
        /// <param name="dividend">The next dividend</param>
        /// <param name="previousFactorFileRow">The previous <see cref="FactorFileRow"/> generated</param>
        /// <returns><see cref="FactorFileRow"/> that represents the dividend event</returns>
        private FactorFileRow CalculateNextDividendFactor(BaseData dividend, FactorFileRow previousFactorFileRow)
        {
            var eventDayData = GetDailyDataForDate(dividend.Time);

            // If you don't have the equity data nothing can be calculated
            if (eventDayData == null)
            {
                return null;
            }

            TradeBar previousClosingPrice = FindPreviousTradableDayClosingPrice(eventDayData.Time);

            // adjust the dividend for both price and split factors (!)
            var priceFactor = previousFactorFileRow.PriceFactor - dividend.Value *
                              previousFactorFileRow.PriceFactor /
                              previousClosingPrice.Close /
                              previousFactorFileRow.SplitFactor;

            return new FactorFileRow(
                previousClosingPrice.Time,
                priceFactor.RoundToSignificantDigits(7),
                previousFactorFileRow.SplitFactor,
                previousClosingPrice.Close
            );
        }

        /// <summary>
        /// Calculates the split factor of a <see cref="Split"/>
        /// </summary>
        /// <param name="split">The next <see cref="Split"/></param>
        /// <param name="previousFactorFileRow">The previous <see cref="FactorFileRow"/> generated</param>
        /// <returns><see cref="FactorFileRow"/>  that represents the split event</returns>
        private FactorFileRow CalculateNextSplitFactor(BaseData split, FactorFileRow previousFactorFileRow)
        {
            var eventDayData = GetDailyDataForDate(split.Time);

            // If you don't have the equity data nothing can be done
            if (eventDayData == null)
            {
                return null;
            }

            TradeBar previousClosingPrice = FindPreviousTradableDayClosingPrice(eventDayData.Time);

            return new FactorFileRow(
                    previousClosingPrice.Time,
                    previousFactorFileRow.PriceFactor,
                    (previousFactorFileRow.SplitFactor / split.Value).RoundToSignificantDigits(6),
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
        private List<TradeBar> ReadDailyEquityData(string pathForDailyEquityData)
        {
            using (var zipToOpen = new FileStream(pathForDailyEquityData, FileMode.Open))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var parser = new LeanParser();
                        var stream = entry.Open();
                        return parser.Parse(pathForDailyEquityData, stream)
                                     .OrderByDescending(x => x.Time)
                                     .Select(x => (TradeBar)x)
                                     .ToList();
                    }
                }
            }
            return new List<TradeBar>();
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
                    throw new ArgumentNullException("split");
                }

                if (dividend == null)
                {
                    throw new ArgumentNullException("dividend");
                }


                Split = split;
                Dividend = dividend;
                Time = Split.Time;
            }
        }
    }
}
