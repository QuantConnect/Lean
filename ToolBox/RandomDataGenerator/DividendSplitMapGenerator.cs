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
using QuantConnect.Data.Market;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates random splits, random dividends, and map file
    /// </summary>
    public class DividendSplitMapGenerator
    {
        /// <summary>
        /// The final factor to adjust all prices with in order to maintain price continuity.
        /// </summary>
        /// <remarks>
        /// Set default equal to 1 so that we can use it even in the event of no splits
        /// </remarks>
        public decimal FinalSplitFactor = 1m;

        /// <summary>
        /// Stores <see cref="MapFileRow"/> instances
        /// </summary>
        public List<MapFileRow> MapRows = new();

        /// <summary>
        /// Stores <see cref="CorporateFactorRow"/> instances
        /// </summary>
        public List<CorporateFactorRow> DividendsSplits = new List<CorporateFactorRow>();

        /// <summary>
        /// Current Symbol value. Can be renamed
        /// </summary>
        public Symbol CurrentSymbol { get; private set; }

        private readonly RandomValueGenerator _randomValueGenerator;
        private readonly Random _random;
        private readonly RandomDataGeneratorSettings _settings;
        private readonly DateTime _delistDate;
        private readonly bool _willBeDelisted;
        private readonly BaseSymbolGenerator _symbolGenerator;

        public DividendSplitMapGenerator(
            Symbol symbol,
            RandomDataGeneratorSettings settings,
            RandomValueGenerator randomValueGenerator,
            BaseSymbolGenerator symbolGenerator,
            Random random,
            DateTime delistDate,
            bool willBeDelisted)
        {
            CurrentSymbol = symbol;
            _settings = settings;
            _randomValueGenerator = randomValueGenerator;
            _random = random;
            _delistDate = delistDate;
            _willBeDelisted = willBeDelisted;
            _symbolGenerator = symbolGenerator;
        }

        /// <summary>
        /// Generates the splits, dividends, and maps.
        /// Writes necessary output to public variables
        /// </summary>
        /// <param name="tickHistory"></param>
        public void GenerateSplitsDividends(IEnumerable<Tick> tickHistory)
        {
            var previousMonth = -1;
            var monthsTrading = 0;

            var hasRename = _randomValueGenerator.NextBool(_settings.HasRenamePercentage);
            var hasSplits = _randomValueGenerator.NextBool(_settings.HasSplitsPercentage);
            var hasDividends = _randomValueGenerator.NextBool(_settings.HasDividendsPercentage);
            var dividendEveryQuarter = _randomValueGenerator.NextBool(_settings.DividendEveryQuarterPercentage);

            var previousX = _random.NextDouble();

            // Since the largest equity value we can obtain is 10 000 000, if we want this price divided by the FinalSplitFactor
            // to be upper bounded by 100 000 000 we need to make sure the FinalSplitFactor is lower bounded by 0.1. Therefore,
            // since in the worst of the cases FinalSplitFactor = (previousSplitFactor)^(2m), where m is the number of months
            // in the time span, we need to lower bound previousSplitFactor by (0.1)^(1/(2m))
            //
            // In the sameway, since the smallest equity value is 0.01, if we want this price divided by the FinalSplitFactor
            // to be lower bounded by 0.01 we need to make sure the FinalSplitFactor is upper bounded by 1. Therefore, since
            // in the worst of the cases FinalSplitFactor = (previousSplitFactor)^(2m), where m is the number of months in
            // the time span, we need to upper bound previousSplitFactor by 1

            var months = (int)Math.Round(_settings.End.Subtract(_settings.Start).Days / (365.25 / 12));
            var minPreviousSplitFactor = (decimal)(Math.Pow((double)0.1, 1 / (double)(2 * months)));
            var maxPreviousSplitFactor = 1;
            var previousSplitFactor = hasSplits ? ((decimal)_random.NextDouble())*(maxPreviousSplitFactor - minPreviousSplitFactor) + minPreviousSplitFactor : 1;
            var previousPriceFactor = hasDividends ? (decimal)Math.Tanh(previousX) : 1;

            var splitDates = new List<DateTime>();
            var dividendDates = new List<DateTime>();

            var firstTick = true;

            // Iterate through all ticks and generate splits and dividend data
            if (_settings.SecurityType == SecurityType.Equity)
            {
                foreach (var tick in tickHistory)
                {
                    // On the first trading day write relevant starting data to factor and map files
                    if (firstTick)
                    {
                        DividendsSplits.Add(new CorporateFactorRow(tick.Time,
                            previousPriceFactor,
                            previousSplitFactor,
                            tick.Value));

                        MapRows.Add(new MapFileRow(tick.Time, CurrentSymbol.Value));
                    }

                    // Add the split to the DividendsSplits list if we have a pending
                    // split. That way, we can use the correct referencePrice in the split event.
                    if (splitDates.Count != 0)
                    {
                        var deleteDates = new List<DateTime>();

                        foreach (var splitDate in splitDates)
                        {
                            if (tick.Time > splitDate)
                            {
                                DividendsSplits.Add(new CorporateFactorRow(
                                    splitDate,
                                    previousPriceFactor,
                                    previousSplitFactor,
                                    tick.Value / FinalSplitFactor));

                                FinalSplitFactor *= previousSplitFactor;
                                deleteDates.Add(splitDate);
                            }
                        }

                        // Deletes dates we've already looped over
                        splitDates.RemoveAll(x => deleteDates.Contains(x));
                    }

                    if (dividendDates.Count != 0)
                    {
                        var deleteDates = new List<DateTime>();

                        foreach (var dividendDate in dividendDates)
                        {
                            if (tick.Time > dividendDate)
                            {
                                DividendsSplits.Add(new CorporateFactorRow(
                                    dividendDate,
                                    previousPriceFactor,
                                    previousSplitFactor,
                                    tick.Value / FinalSplitFactor));

                                deleteDates.Add(dividendDate);
                            }
                        }

                        dividendDates.RemoveAll(x => deleteDates.Contains(x));
                    }

                    if (tick.Time.Month != previousMonth)
                    {
                        // Every quarter, try to generate dividend events
                        if (hasDividends && (tick.Time.Month - 1) % 3 == 0)
                        {
                            // Make it so there's a 10% chance that dividends occur if there is no dividend every quarter
                            if (dividendEveryQuarter || _randomValueGenerator.NextBool(10.0))
                            {
                                do
                                {
                                    previousX += _random.NextDouble() / 10;
                                    previousPriceFactor = (decimal)Math.Tanh(previousX);
                                } while (previousPriceFactor >= 1.0m || previousPriceFactor <= 0m);

                                dividendDates.Add(_randomValueGenerator.NextDate(tick.Time, tick.Time.AddMonths(1), (DayOfWeek)_random.Next(1, 5)));
                            }
                        }
                        // Have a 5% chance of a split every month
                        if (hasSplits && _randomValueGenerator.NextBool(_settings.MonthSplitPercentage))
                        {
                            // Produce another split factor that is also bounded by the min and max split factors allowed
                            if (_randomValueGenerator.NextBool(5.0)) // Add the possibility of a reverse split
                            {
                                previousSplitFactor = ((decimal)_random.NextDouble()) * (previousSplitFactor - minPreviousSplitFactor) + minPreviousSplitFactor;
                            }
                            else
                            {
                                previousSplitFactor = ((decimal)_random.NextDouble()) * (maxPreviousSplitFactor - previousSplitFactor) + previousSplitFactor;
                            }

                            splitDates.Add(_randomValueGenerator.NextDate(tick.Time, tick.Time.AddMonths(1), (DayOfWeek)_random.Next(1, 5)));
                        }
                        // 10% chance of being renamed every month
                        if (hasRename && _randomValueGenerator.NextBool(10.0))
                        {
                            var randomDate = _randomValueGenerator.NextDate(tick.Time, tick.Time.AddMonths(1), (DayOfWeek)_random.Next(1, 5));
                            MapRows.Add(new MapFileRow(randomDate, CurrentSymbol.Value));

                            CurrentSymbol = _symbolGenerator.NextSymbol(_settings.SecurityType, _settings.Market);
                        }

                        previousMonth = tick.Time.Month;
                        monthsTrading++;
                    }

                    if (monthsTrading >= 6 && _willBeDelisted && tick.Time > _delistDate)
                    {
                        MapRows.Add(new MapFileRow(tick.Time, CurrentSymbol.Value));
                        break;
                    }

                    firstTick = false;
                }
            }
        }
    }
}
