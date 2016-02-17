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
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class LeanDataTests
    {
        [Test, TestCaseSource("GetLeanDataTestParameters")]
        public void GenerateZipFileName(LeanDataTestParameters parameters)
        {
            var zip = LeanData.GenerateZipFileName(parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedZipFileName, zip);
        }

        [Test, TestCaseSource("GetLeanDataTestParameters")]
        public void GenerateZipEntryName(LeanDataTestParameters parameters)
        {
            var entry = LeanData.GenerateZipEntryName(parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedZipEntryName, entry);
        }

        [Test, TestCaseSource("GetLeanDataTestParameters")]
        public void GenerateRelativeZipFilePath(LeanDataTestParameters parameters)
        {
            var relativePath = LeanData.GenerateRelativeZipFilePath(parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedRelativeZipFilePath, relativePath);
        }

        private static TestCaseData[] GetLeanDataTestParameters()
        {
            var date = new DateTime(2016, 02, 17);
            return new List<LeanDataTestParameters>
            {
                // equity
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Tick, TickType.Trade, "20160217_trade.zip", "20160217_spy_tick_trade.csv", "equity/usa/tick/spy"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Second, TickType.Trade, "20160217_trade.zip", "20160217_spy_second_trade.csv", "equity/usa/second/spy"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Minute, TickType.Trade, "20160217_trade.zip", "20160217_spy_minute_trade.csv", "equity/usa/minute/spy"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Hour, TickType.Trade, "spy.zip", "spy.csv", "equity/usa/hour"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Daily, TickType.Trade, "spy.zip", "spy.csv", "equity/usa/daily"),

                // equity option trades
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Tick, TickType.Trade, "20160217_trade.zip", "20160217_spy_american_put_1920000_20160219_tick_trade.csv", "option/usa/tick/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Tick, TickType.Quote, "20160217_quote.zip", "20160217_spy_american_put_1920000_20160219_tick_quote.csv", "option/usa/tick/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Second, TickType.Trade, "20160217_trade.zip", "20160217_spy_american_put_1920000_20160219_second_trade.csv", "option/usa/second/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Second, TickType.Quote, "20160217_quote.zip", "20160217_spy_american_put_1920000_20160219_second_quote.csv", "option/usa/second/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Minute, TickType.Trade, "20160217_trade.zip", "20160217_spy_american_put_1920000_20160219_minute_trade.csv", "option/usa/minute/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Minute, TickType.Quote, "20160217_quote.zip", "20160217_spy_american_put_1920000_20160219_minute_quote.csv", "option/usa/minute/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Hour, TickType.Trade, "spy_trade.zip", "spy_american_put_1920000_20160219_trade.csv", "option/usa/hour"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Hour, TickType.Quote, "spy_quote.zip", "spy_american_put_1920000_20160219_quote.csv", "option/usa/hour"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Daily, TickType.Trade, "spy_trade.zip", "spy_american_put_1920000_20160219_trade.csv", "option/usa/daily"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Daily, TickType.Quote, "spy_quote.zip", "spy_american_put_1920000_20160219_quote.csv", "option/usa/daily"),

                // forex
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Tick, TickType.Quote, "20160217_quote.zip", "20160217_eurusd_tick_quote.csv", "forex/fxcm/tick/eurusd"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Second, TickType.Quote, "20160217_quote.zip", "20160217_eurusd_second_quote.csv", "forex/fxcm/second/eurusd"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Minute, TickType.Quote, "20160217_quote.zip", "20160217_eurusd_minute_quote.csv", "forex/fxcm/minute/eurusd"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Hour, TickType.Quote, "eurusd.zip", "eurusd.csv", "forex/fxcm/hour"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Daily, TickType.Quote, "eurusd.zip", "eurusd.csv", "forex/fxcm/daily"),

                // cfd
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Tick, TickType.Quote, "20160217_quote.zip", "20160217_de10ybeur_tick_quote.csv", "cfd/fxcm/tick/de10ybeur"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Second, TickType.Quote, "20160217_quote.zip", "20160217_de10ybeur_second_quote.csv", "cfd/fxcm/second/de10ybeur"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Minute, TickType.Quote, "20160217_quote.zip", "20160217_de10ybeur_minute_quote.csv", "cfd/fxcm/minute/de10ybeur"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Hour, TickType.Quote, "de10ybeur.zip", "de10ybeur.csv", "cfd/fxcm/hour"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Daily, TickType.Quote, "de10ybeur.zip", "de10ybeur.csv", "cfd/fxcm/daily"),

            }.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
        }

        public class LeanDataTestParameters
        {
            public readonly string Name;
            public readonly Symbol Symbol;
            public readonly DateTime Date;
            public readonly Resolution Resolution;
            public readonly TickType TickType;
            public readonly string ExpectedZipFileName;
            public readonly string ExpectedZipEntryName;
            public readonly string ExpectedRelativeZipFilePath;
            public SecurityType SecurityType { get { return Symbol.ID.SecurityType; } }
            

            public LeanDataTestParameters(Symbol symbol, DateTime date, Resolution resolution, TickType tickType, string expectedZipFileName, string expectedZipEntryName, string expectedRelativeZipFileDirectory = "")
            {
                Symbol = symbol;
                Date = date;
                Resolution = resolution;
                TickType = tickType;
                ExpectedZipFileName = expectedZipFileName;
                ExpectedZipEntryName = expectedZipEntryName;
                ExpectedRelativeZipFilePath = Path.Combine(expectedRelativeZipFileDirectory, expectedZipFileName).Replace("/", Path.DirectorySeparatorChar.ToString());

                Name = SecurityType + "_" + resolution;
            }

            public LeanDataTestParameters(string name)
            {
                Name = name;
            }
        }
    }
}
