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
using System.IO;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.ToolBox.RandomDataGenerator;

/// <summary>
/// Writes daily Option Universe CSV files for generated options data.
/// </summary>
internal static class OptionUniverseWriter
{
    private const decimal DefaultSyntheticImpliedVolatility = 0.20m;
    private const decimal DefaultUnderlyingPrice = 100.00m;

    private readonly record struct OptionRow(
        Symbol Symbol,
        decimal Open,
        decimal High,
        decimal Low,
        decimal Close,
        decimal Volume,
        decimal OpenInterest
    );

    /// <summary>
    /// Processes generated tick histories and writes daily universe selection CSVs for the option.
    /// </summary>
    /// <param name="underlyingSymbol">The underlying asset symbol.</param>
    /// <param name="tickHistories">The historical ticks generated for each option contract.</param>
    public static void WriteUniverseFiles(Symbol underlyingSymbol, Dictionary<Symbol, List<Tick>> tickHistories)
    {
        var dailyData = AggregateDailyOptionRows(tickHistories);
        WriteAllDailyUniverseFiles(underlyingSymbol, dailyData);
    }

    private static Dictionary<DateTime, List<OptionRow>> AggregateDailyOptionRows(
        Dictionary<Symbol, List<Tick>> tickHistories)
    {
        var dailyContractData = new Dictionary<DateTime, List<OptionRow>>();

        foreach (var (contract, ticks) in tickHistories)
        {
            if (ticks.Count == 0)
            {
                continue;
            }

            AggregateContractTicks(contract, ticks, dailyContractData);
        }

        return dailyContractData;
    }

    private static void AggregateContractTicks(
        Symbol contract,
        List<Tick> ticks,
        Dictionary<DateTime, List<OptionRow>> dailyContractData)
    {
        foreach (var dayGroup in ticks.GroupBy(t => t.Time.Date))
        {
            var dayTicks = dayGroup.ToList();
            var row = CreateOptionRow(contract, dayTicks);

            if (!dailyContractData.TryGetValue(dayGroup.Key, out var list))
            {
                list = new List<OptionRow>();
                dailyContractData[dayGroup.Key] = list;
            }

            list.Add(row);
        }
    }

    private static OptionRow CreateOptionRow(Symbol contract, List<Tick> dayTicks)
    {
        var open = dayTicks.First().Value;
        var high = dayTicks.Max(t => t.Value);
        var low = dayTicks.Min(t => t.Value);
        var close = dayTicks.Last().Value;
        var volume = dayTicks.Sum(t => t.Quantity);
        var openInterest = dayTicks.Last().Quantity;

        return new OptionRow(contract, open, high, low, close, volume, openInterest);
    }

    private static void WriteAllDailyUniverseFiles(Symbol underlyingSymbol, Dictionary<DateTime, List<OptionRow>> dailyData)
    {
        var canonicalSymbol = Symbol.CreateCanonicalOption(underlyingSymbol);

        foreach (var (date, rows) in dailyData)
        {
            WriteSingleDailyUniverseFile(canonicalSymbol, date, rows);
        }
    }

    private static void WriteSingleDailyUniverseFile(Symbol canonicalSymbol, DateTime date, List<OptionRow> rows)
    {
        var universePath = BaseChainUniverseData.GetUniverseFullFilePath(canonicalSymbol, date);
        Directory.CreateDirectory(Path.GetDirectoryName(universePath)!);

        var lines = GenerateCsvLines(rows);
        File.WriteAllLines(universePath, lines);
    }

    private static IEnumerable<string> GenerateCsvLines(List<OptionRow> rows)
    {
        yield return OptionUniverse.CsvHeader(SecurityType.Option);
        yield return FormatUnderlyingRow(DefaultUnderlyingPrice);

        var sortedRows = rows.OrderBy(r => r.Symbol.ID.Date).ThenBy(r => r.Symbol.ID.StrikePrice);
        foreach (var row in sortedRows)
        {
            yield return OptionUniverse.ToCsv(
                row.Symbol,
                row.Open,
                row.High,
                row.Low,
                row.Close,
                row.Volume,
                row.OpenInterest,
                DefaultSyntheticImpliedVolatility,
                null
            );
        }
    }

    private static string FormatUnderlyingRow(decimal price)
    {
        return FormattableString.Invariant($",,,{price:F4},{price:F4},{price:F4},{price:F4},0,,,,,,,");
    }
}
