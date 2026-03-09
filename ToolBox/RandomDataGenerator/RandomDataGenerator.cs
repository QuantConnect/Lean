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
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates random data according to the specified parameters
    /// </summary>
    public class RandomDataGenerator
    {
        private RandomDataGeneratorSettings _settings;
        private SecurityManager _securityManager;

        /// <summary>
        /// Initializes <see cref="RandomDataGenerator"/> instance fields
        /// </summary>
        /// <param name="settings">random data generation settings</param>
        /// <param name="securityManager">security management</param>
        public void Init(RandomDataGeneratorSettings settings, SecurityManager securityManager)
        {
            _settings = settings;
            _securityManager = securityManager;
        }

        /// <summary>
        /// Starts data generation
        /// </summary>
        public void Run()
        {
            var tickTypesPerSecurityType = SubscriptionManager.DefaultDataTypes();
            // can specify a seed value in this ctor if determinism is desired
            var random = new Random();
            var randomValueGenerator = new RandomValueGenerator();
            if (_settings.RandomSeedSet)
            {
                random = new Random(_settings.RandomSeed);
                randomValueGenerator = new RandomValueGenerator(_settings.RandomSeed);
            }

            var symbolGenerator = BaseSymbolGenerator.Create(_settings, randomValueGenerator);

            var maxSymbolCount = symbolGenerator.GetAvailableSymbolCount();
            if (_settings.SymbolCount > maxSymbolCount)
            {
                Log.Error($"RandomDataGenerator.Run(): Limiting Symbol count to {maxSymbolCount}, we don't have more {_settings.SecurityType} tickers for {_settings.Market}");
                _settings.SymbolCount = maxSymbolCount;
            }

            Log.Trace($"RandomDataGenerator.Run(): Begin data generation of {_settings.SymbolCount} randomly generated {_settings.SecurityType} assets...");

            // iterate over our randomly generated symbols
            var count = 0;
            var progress = 0d;
            var previousMonth = -1;

            foreach (var (symbolRef, currentSymbolGroup) in symbolGenerator.GenerateRandomSymbols()
                .GroupBy(s => s.HasUnderlying ? s.Underlying : s)
                .Select(g => (g.Key, g.OrderBy(s => s.HasUnderlying).ToList())))
            {
                Log.Trace($"RandomDataGenerator.Run(): Symbol[{++count}]: {symbolRef} Progress: {progress:0.0}% - Generating data...");

                var tickGenerators = new List<IEnumerator<Tick>>();
                var tickHistories = new Dictionary<Symbol, List<Tick>>();
                Security underlyingSecurity = null;
                foreach (var currentSymbol in currentSymbolGroup)
                {
                    if (!_securityManager.TryGetValue(currentSymbol, out var security))
                    {
                        security = _securityManager.CreateSecurity(
                            currentSymbol,
                            new List<SubscriptionDataConfig>(),
                            underlying: underlyingSecurity);
                        _securityManager.Add(security);
                    }

                    underlyingSecurity ??= security;

                    tickGenerators.Add(
                        new TickGenerator(_settings, tickTypesPerSecurityType[currentSymbol.SecurityType].ToArray(), security, randomValueGenerator)
                            .GenerateTicks()
                            .GetEnumerator());

                    tickHistories.Add(
                        currentSymbol,
                        new List<Tick>());
                }

                using var sync = new SynchronizingBaseDataEnumerator(tickGenerators);

                var lastLoggedProgress = 0.0;
                Log.Trace("[0%] Initializing tick data generation");
                while (sync.MoveNext())
                {
                    var dataPoint = sync.Current;
                    if (!_securityManager.TryGetValue(dataPoint.Symbol, out var security))
                    {
                        Log.Error($"RandomDataGenerator.Run(): Could not find security for symbol {sync.Current.Symbol}");
                        continue;
                    }

                    tickHistories[security.Symbol].Add(dataPoint as Tick);
                    security.Update(new List<BaseData> { dataPoint }, dataPoint.GetType(), false);

                    // Calculate and log progress percentage when it increases by more than 3%
                    var currentProgress = RandomDataGeneratorHelper.GetProgressAsPercentage(_settings.Start, _settings.End, dataPoint.EndTime);
                    if (currentProgress - lastLoggedProgress >= 3.0)
                    {
                        Log.Trace($"[{currentProgress:0.00}%] Generating tick data");
                        lastLoggedProgress = currentProgress;
                    }
                }
                Log.Trace("[100%] Tick data generation completed successfully.");
                foreach (var (currentSymbol, tickHistory) in tickHistories)
                {
                    var symbol = currentSymbol;

                    // This is done so that we can update the Symbol in the case of a rename event
                    var delistDate = GetDelistingDate(_settings.Start, _settings.End, randomValueGenerator);
                    var willBeDelisted = randomValueGenerator.NextBool(1.0);

                    // Companies rarely IPO then disappear within 6 months
                    if (willBeDelisted && tickHistory.Select(tick => tick.Time.Month).Distinct().Count() <= 6)
                    {
                        willBeDelisted = false;
                    }

                    var dividendsSplitsMaps = new DividendSplitMapGenerator(
                        symbol,
                        _settings,
                        randomValueGenerator,
                        symbolGenerator,
                        random,
                        delistDate,
                        willBeDelisted);

                    // Keep track of renamed symbols and the time they were renamed. 
                    var renamedSymbols = new Dictionary<Symbol, DateTime>();

                    if (_settings.SecurityType == SecurityType.Equity)
                    {
                        dividendsSplitsMaps.GenerateSplitsDividends(tickHistory);

                        if (!willBeDelisted)
                        {
                            dividendsSplitsMaps.DividendsSplits.Add(new CorporateFactorRow(new DateTime(2050, 12, 31), 1m, 1m));

                            if (dividendsSplitsMaps.MapRows.Count > 1)
                            {
                                // Remove the last element if we're going to have a 20501231 entry
                                dividendsSplitsMaps.MapRows.RemoveAt(dividendsSplitsMaps.MapRows.Count - 1);
                            }
                            dividendsSplitsMaps.MapRows.Add(new MapFileRow(new DateTime(2050, 12, 31), dividendsSplitsMaps.CurrentSymbol.Value));
                        }

                        // If the Symbol value has changed, update the current Symbol
                        if (symbol != dividendsSplitsMaps.CurrentSymbol)
                        {
                            // Add all Symbol rename events to dictionary
                            // We skip the first row as it contains the listing event instead of a rename event
                            foreach (var renameEvent in dividendsSplitsMaps.MapRows.Skip(1))
                            {
                                // Symbol.UpdateMappedSymbol does not update the underlying security ID Symbol, which 
                                // is used to create the hash code. Create a new equity Symbol from scratch instead.
                                symbol = Symbol.Create(renameEvent.MappedSymbol, SecurityType.Equity, _settings.Market);
                                renamedSymbols.Add(symbol, renameEvent.Date);

                                Log.Trace($"RandomDataGenerator.Run(): Symbol[{count}]: {symbol} will be renamed on {renameEvent.Date}");
                            }
                        }
                        else
                        {
                            // This ensures that ticks will be written for the current Symbol up until 9999-12-31
                            renamedSymbols.Add(symbol, new DateTime(9999, 12, 31));
                        }

                        symbol = dividendsSplitsMaps.CurrentSymbol;

                        // Write Splits and Dividend events to directory factor_files
                        var factorFile = new CorporateFactorProvider(symbol.Value, dividendsSplitsMaps.DividendsSplits, _settings.Start);
                        var mapFile = new MapFile(symbol.Value, dividendsSplitsMaps.MapRows);

                        factorFile.WriteToFile(symbol);
                        mapFile.WriteToCsv(_settings.Market, symbol.SecurityType);

                        Log.Trace($"RandomDataGenerator.Run(): Symbol[{count}]: {symbol} Dividends, splits, and map files have been written to disk.");
                    }
                    else
                    {
                        // This ensures that ticks will be written for the current Symbol up until 9999-12-31
                        renamedSymbols.Add(symbol, new DateTime(9999, 12, 31));
                    }

                    // define aggregators via settings
                    var aggregators = CreateAggregators(_settings, tickTypesPerSecurityType[currentSymbol.SecurityType].ToArray()).ToList();
                    Symbol previousSymbol = null;
                    var currentCount = 0;
                    var monthsTrading = 0;

                    foreach (var renamed in renamedSymbols)
                    {
                        var previousRenameDate = previousSymbol == null ? new DateTime(1, 1, 1) : renamedSymbols[previousSymbol];
                        var previousRenameDateDay = new DateTime(previousRenameDate.Year, previousRenameDate.Month, previousRenameDate.Day);
                        var renameDate = renamed.Value;
                        var renameDateDay = new DateTime(renameDate.Year, renameDate.Month, renameDate.Day);

                        foreach (var tick in tickHistory.Where(tick => tick.Time >= previousRenameDate && previousRenameDateDay != TickDay(tick)))
                        {
                            // Prevents the aggregator from being updated with ticks after the rename event
                            if (TickDay(tick) > renameDateDay)
                            {
                                break;
                            }

                            if (tick.Time.Month != previousMonth)
                            {
                                Log.Trace($"RandomDataGenerator.Run(): Symbol[{count}]: Month: {tick.Time:MMMM}");
                                previousMonth = tick.Time.Month;
                                monthsTrading++;
                            }

                            foreach (var item in aggregators)
                            {
                                tick.Value = tick.Value / dividendsSplitsMaps.FinalSplitFactor;
                                item.Consolidator.Update(tick);
                            }

                            if (monthsTrading >= 6 && willBeDelisted && tick.Time > delistDate)
                            {
                                Log.Trace($"RandomDataGenerator.Run(): Symbol[{count}]: {renamed.Key} delisted at {tick.Time:MMMM yyyy}");
                                break;
                            }
                        }

                        // count each stage as a point, so total points is 2*Symbol-count
                        // and the current progress is twice the current, but less one because we haven't finished writing data yet
                        progress = 100 * (2 * count - 1) / (2.0 * _settings.SymbolCount);

                        Log.Trace($"RandomDataGenerator.Run(): Symbol[{count}]: {renamed.Key} Progress: {progress:0.0}% - Saving data in LEAN format");

                        // persist consolidated data to disk
                        foreach (var item in aggregators)
                        {
                            var writer = new LeanDataWriter(item.Resolution, renamed.Key, Globals.DataFolder, item.TickType);

                            // send the flushed data into the writer. pulling the flushed list is very important,
                            // lest we likely wouldn't get the last piece of data stuck in the consolidator
                            // Filter out the data we're going to write here because filtering them in the consolidator update phase
                            // makes it write all dates for some unknown reason
                            writer.Write(item.Flush().Where(data => data.Time > previousRenameDate && previousRenameDateDay != DataDay(data)));
                        }

                        // update progress
                        progress = 100 * (2 * count) / (2.0 * _settings.SymbolCount);
                        Log.Trace($"RandomDataGenerator.Run(): Symbol[{count}]: {symbol} Progress: {progress:0.0}% - Symbol data generation and output completed");

                        previousSymbol = renamed.Key;
                        currentCount++;
                    }
                }
            }

            Log.Trace("RandomDataGenerator.Run(): Random data generation has completed.");

            DateTime TickDay(Tick tick) => new(tick.Time.Year, tick.Time.Month, tick.Time.Day);
            DateTime DataDay(BaseData data) => new(data.Time.Year, data.Time.Month, data.Time.Day);
        }

        public static DateTime GetDateMidpoint(DateTime start, DateTime end)
        {
            TimeSpan span = end.Subtract(start);
            int span_time = (int)span.TotalMinutes;
            double diff_span = -(span_time / 2.0);
            DateTime start_time = end.AddMinutes(Math.Round(diff_span, 2, MidpointRounding.ToEven));

            //Returns a DateTime object that is halfway between start and end
            return start_time;
        }

        public static DateTime GetDelistingDate(DateTime start, DateTime end, RandomValueGenerator randomValueGenerator)
        {
            var mid_point = GetDateMidpoint(start, end);
            var delist_Date = randomValueGenerator.NextDate(mid_point, end, null);

            //Returns a DateTime object that is a random value between the mid_point and end
            return delist_Date;
        }

        public static IEnumerable<TickAggregator> CreateAggregators(RandomDataGeneratorSettings settings, TickType[] tickTypes)
        {
            // create default aggregators for tick type/resolution
            foreach (var tickAggregator in TickAggregator.ForTickTypes(settings.SecurityType, settings.Resolution, tickTypes))
            {
                yield return tickAggregator;
            }


            // ensure we have a daily consolidator when coarse is enabled
            if (settings.IncludeCoarse && settings.Resolution != Resolution.Daily)
            {
                // prefer trades for coarse - in practice equity only does trades, but leaving this as configurable
                if (tickTypes.Contains(TickType.Trade))
                {
                    yield return TickAggregator.ForTickTypes(settings.SecurityType, Resolution.Daily, TickType.Trade).Single();
                }
                else
                {
                    yield return TickAggregator.ForTickTypes(settings.SecurityType, Resolution.Daily, TickType.Quote).Single();
                }
            }
        }
    }
}
