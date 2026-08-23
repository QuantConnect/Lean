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
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.ToolBox.RandomDataGenerator;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator;

/// <summary>
/// Unit tests verifying that <see cref="OptionUniverseWriter"/> generates valid daily option universe CSV files.
/// </summary>
[TestFixture]
internal class OptionUniverseWriterTests
{
    private string _tempDataFolder = null!;
    private string _originalDataFolder = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDataFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDataFolder);

        _originalDataFolder = Globals.DataFolder;
        Config.Set("data-folder", _tempDataFolder);
        Globals.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        Config.Set("data-folder", _originalDataFolder);
        Globals.Reset();

        if (Directory.Exists(_tempDataFolder))
        {
            Directory.Delete(_tempDataFolder, true);
        }
    }

    /// <summary>
    /// Verifies that WriteUniverseFiles aggregates intraday contract ticks and writes the expected daily universe CSV.
    /// </summary>
    [Test]
    public void WriteUniverseFiles_GivenOptionTicks_GeneratesMatchingDailyUniverseCsv()
    {
        var (underlying, tickHistories) = CreateSampleOptionTickHistory();

        OptionUniverseWriter.WriteUniverseFiles(underlying, tickHistories);

        var lines = ReadGeneratedUniverseFile("tsla", "20260205");
        Assert.That(lines.Length, Is.EqualTo(3));
        Assert.That(lines[0], Is.EqualTo("expiry,strike,right,open,high,low,close,volume,open_interest,implied_volatility,delta,gamma,vega,theta,rho"));
        Assert.That(lines[1], Is.EqualTo(",,,100.0000,100.0000,100.0000,100.0000,0,,,,,,,"));
        Assert.That(lines[2], Is.EqualTo("20260213,200,C,1.50,1.75,1.50,1.75,30,20,0.20,,,,,"));
    }

    private static (Symbol Underlying, Dictionary<Symbol, List<Tick>> Histories) CreateSampleOptionTickHistory()
    {
        var underlying = Symbol.Create("TSLA", SecurityType.Equity, Market.USA);
        var option = Symbol.CreateOption(underlying, Market.USA, OptionStyle.American, OptionRight.Call, 200m, new DateTime(2026, 2, 13));

        var ticks = new List<Tick>
        {
            new(new DateTime(2026, 2, 5, 9, 30, 0), option, 1.50m, 1.50m) { Quantity = 10 },
            new(new DateTime(2026, 2, 5, 16, 0, 0), option, 1.75m, 1.75m) { Quantity = 20 }
        };

        return (underlying, new Dictionary<Symbol, List<Tick>> { { option, ticks } });
    }

    private string[] ReadGeneratedUniverseFile(string ticker, string date)
    {
        var path = Path.Combine(_tempDataFolder, "option", "usa", "universes", ticker, $"{date}.csv");
        Assert.That(File.Exists(path), Is.True, $"Universe file was not found at expected path: {path}");
        return File.ReadAllLines(path);
    }
}
