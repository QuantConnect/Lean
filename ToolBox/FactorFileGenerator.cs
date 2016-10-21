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
using QuantConnect.Orders;
using QuantConnect.Statistics;

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
        public List<TradeBar> DailyDataForEquity { get; private set; }

        private readonly DateTime _lastDateFromEquityData;

        /// <param name="symbol">The equity for which the factor file respresents</param>
        /// <param name="pathForDailyEquityData">The path to the daily data for the specified equity</param>
        public FactorFileGenerator(Symbol symbol, string pathForDailyEquityData)
        {
            Symbol = symbol;
            DailyDataForEquity = ReadDailyEquityData(pathForDailyEquityData);
            _lastDateFromEquityData = DailyDataForEquity.Last().Time;
        }

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
                                     .Select(x => (TradeBar) x)
                                     .ToList();
                    }
                }
            }
            return new List<TradeBar>();
        }

        /// <summary>
        /// Create FactorFile object
        /// </summary>
        /// <param name="baseDataQueue">Queue of Dividend and Splits</param>
        /// <returns>A factor file object</returns>
        public FactorFile CreateFactorFileFromData(Queue<BaseData> baseDataQueue)
        {
            baseDataQueue = ConsolidateIntraDayDividendSplits(baseDataQueue);

            var factorFileRows = new List<FactorFileRow>()
            {
                // First Factor Row is set far into the future
                new FactorFileRow(DateTime.ParseExact("20501231",
                                                      DateFormat.EightCharacter,
                                                      CultureInfo.InvariantCulture),
                                  1, // Price Factor
                                  1) // Split Factor
            };

            return RecursivlyGenerateFactorFile(baseDataQueue, factorFileRows);
        }
        /// <summary>
        /// If dividend and split occur on the same day, 
        ///   consolidate them into IntraDayDividendSplit object
        /// </summary>
        private Queue<BaseData> ConsolidateIntraDayDividendSplits(Queue<BaseData> marketEventQueue)
        {
            var marketEventList = new ObservableCollection<BaseData>(marketEventQueue.ToList());

            var dateKeysLookup = marketEventList.GroupBy(x => x.Time)
                                                .OrderByDescending(x => x.Key)
                                                .Select(group => group)
                                                .ToList();

            var baseDataList = new List<BaseData>();
            foreach (var kvpLookup in dateKeysLookup)
            {
                if (kvpLookup.Count() > 1)
                {
                    var dividend = kvpLookup.First(x => x.GetType() == typeof(Dividend)) as Dividend;
                    var split = kvpLookup.First(x => x.GetType() == typeof(Split)) as Split;
                    baseDataList.Add(new IntraDayDividendSplit(split, dividend));
                }
                else
                {
                    baseDataList.Add(kvpLookup.First());
                }
            }

            return new Queue<BaseData>(baseDataList);
        }

        /// <summary>
        /// Recursively generate the factor file
        /// </summary>
        /// <param name="baseDataDescendingQueue">Queue of dividends and splits as reported</param>
        /// <param name="factorFileRows">The list of factor file rows</param>
        /// <returns>FactorFile object</returns>
        private FactorFile RecursivlyGenerateFactorFile(Queue<BaseData> baseDataDescendingQueue, List<FactorFileRow> factorFileRows)
        {
            // If there is no more data return
            if (!baseDataDescendingQueue.Any())
            {
                factorFileRows.Add(CreateLastFactorFileRow(factorFileRows));
                return new FactorFile(Symbol.ID.Symbol, factorFileRows);
            }

            var nextEvent = baseDataDescendingQueue.Dequeue();

            // If there is no more daily equity data to use return
            if (_lastDateFromEquityData > nextEvent.Time)
            {
                factorFileRows.Add(CreateLastFactorFileRow(factorFileRows));
                return new FactorFile(Symbol.ID.Symbol, factorFileRows);
            }

            var nextFactorFileRow = CalculateNextFactorFileRow(factorFileRows, nextEvent);

            if (nextFactorFileRow != null)
                factorFileRows.Add(nextFactorFileRow);

            return RecursivlyGenerateFactorFile(baseDataDescendingQueue, factorFileRows);
        }

        /// <summary>
        /// Creates the first row in the factor file. The is the last row generated
        /// Represents the earliest date that the daily equity data contains.
        /// </summary>
        /// <param name="factorFileRows">The list of factor file rows</param>
        /// <returns>FactorFileRow</returns>
        private FactorFileRow CreateLastFactorFileRow(List<FactorFileRow> factorFileRows)
        {
            return new FactorFileRow(DailyDataForEquity.Last().Time,
                                     factorFileRows.Last().PriceFactor,
                                     factorFileRows.Last().SplitFactor);
        }

        /// <summary>
        /// Calculates the values for the next row in the factor file
        /// </summary>
        /// <param name="factorFileRows">The current list of factorFileRows</param>
        /// <param name="nextMarketEvent">The next dividend, split or intradayDividendSplit</param>
        /// <returns>A single factor file row</returns>
        private FactorFileRow CalculateNextFactorFileRow(List<FactorFileRow> factorFileRows, BaseData nextMarketEvent)
        {
            FactorFileRow nextFactorFileRow;
            var t = nextMarketEvent.GetType();

            switch (t.Name)
            {
                case "Dividend":
                    nextFactorFileRow = CalculateNextDividendFactor(nextMarketEvent, factorFileRows.Last());
                    break;
                case "Split":
                    nextFactorFileRow = CalculateNextSplitFactor(nextMarketEvent, factorFileRows.Last());
                    break;
                default:
                    nextFactorFileRow = CalculateIntradayDividendSplit((IntraDayDividendSplit)nextMarketEvent, factorFileRows.Last());
                    break;
            }

            return nextFactorFileRow;
        }

        /// <summary>
        /// Calculate a FactorFileRow that represents an intraday dividend split while applying the dividend first
        /// </summary>
        private FactorFileRow CalculateIntradayDividendSplit(IntraDayDividendSplit intraDayDividendSplit, FactorFileRow last)
        {
            var row = CalculateNextDividendFactor(intraDayDividendSplit.Dividend, last);
            return CalculateNextSplitFactor(intraDayDividendSplit.Split, row);
        }

        /// <summary>
        /// Calculates next price factor after dividend occurs
        /// </summary>
        /// <param name="nextEvent">The next dividend event</param>
        /// <param name="lastFactorFileRow">The current last item in the factor file</param>
        /// <returns></returns>
        private FactorFileRow CalculateNextDividendFactor(BaseData nextEvent, FactorFileRow lastFactorFileRow)
        {
            var eventDayData = DailyDataForEquity.FirstOrDefault(x => x.Time.Day == nextEvent.Time.Day
                                                                      && x.Time.Month == nextEvent.Time.Month
                                                                      && x.Time.Year == nextEvent.Time.Year);

            // If you don't have the equity data nothing can be done
            if (eventDayData == null)
                return null;

            TradeBar previousClosingPrice = FindPreviousClosingPrice(eventDayData.Time);

            var priceFactor = lastFactorFileRow.PriceFactor - (nextEvent.Value / ((previousClosingPrice.Close / 10000) * lastFactorFileRow.SplitFactor));

            return new FactorFileRow(previousClosingPrice.Time, priceFactor.RoundToSignificantDigits(7), lastFactorFileRow.SplitFactor);
        }

        /// <summary>
        /// Calculates the split factors
        /// </summary>
        /// <param name="nextMarketEvent">The market split or dividend currently being calculated</param>
        /// <param name="lastFactorFileRow">The last factor file row processed</param>
        /// <returns>The next Factor file row</returns>
        private FactorFileRow CalculateNextSplitFactor(BaseData nextMarketEvent, FactorFileRow lastFactorFileRow)
        {
            return new FactorFileRow(
                    nextMarketEvent.Time,
                    lastFactorFileRow.PriceFactor,
                    (lastFactorFileRow.SplitFactor * nextMarketEvent.Value).RoundToSignificantDigits(6)
                );
        }

        /// <summary>
        /// Gets the previous trading days closing price
        /// </summary>
        /// <param name="date">Date that is currently being processed</param>
        /// <returns>A tradebar representing that days data</returns>
        private TradeBar FindPreviousClosingPrice(DateTime date)
        {
            TradeBar previousDayData = null;
            var lastDateforData = DailyDataForEquity.Last();

            while (previousDayData == null && date > lastDateforData.EndTime)
            {
                previousDayData = DailyDataForEquity.FirstOrDefault(x => x.Time == date.AddDays(-1));
                date = date.AddDays(-1);
            }

            return previousDayData;
        }

    }
}
