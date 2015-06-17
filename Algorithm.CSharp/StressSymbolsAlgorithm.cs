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
using QuantConnect.Securities.Forex;

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// Randomly selects the specified number of symbols from the lists below
    /// </summary>
    public class StressSymbolsAlgorithm : QCAlgorithm
    {
        public const int TickSymbolsToRun = 5;
        public const int SecondSymbolsToRun = 7;
        public const int MinuteSymbolsToRun = 10;

        /// <summary>
        /// Add Hundreds of Stock and Forex Symbol
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 01, 01);
            SetEndDate(2014, 02, 01);
            SetCash(250000);

            var allSymbols = StockSymbols.Concat(ForexSymbols).ToList();
            var symbolsUsed = new HashSet<string>();

            int totalSymbols = TickSymbolsToRun + SecondSymbolsToRun + MinuteSymbolsToRun;

            for (int i = 0; i < totalSymbols; i++)
            {
                Resolution resolution = Resolution.Tick;
                if (i >= TickSymbolsToRun && i < TickSymbolsToRun + SecondSymbolsToRun)
                {
                    resolution = Resolution.Second;
                }
                else if (i >= TickSymbolsToRun + SecondSymbolsToRun)
                {
                    resolution = Resolution.Minute;
                }

                string nextSymbol;
                do
                {
                    nextSymbol = GetRandomItem(allSymbols);
                }
                while (!symbolsUsed.Add(nextSymbol));

                SecurityType type = SecurityType.Equity;
                if (ForexSymbols.Contains(nextSymbol))
                {
                    type = SecurityType.Forex;
                }

                AddSecurity(type, nextSymbol, resolution);
                Debug("Added " + nextSymbol + " at " + resolution);
            }

            int ticks = SubscriptionManager.Subscriptions.Count(x => x.Resolution == Resolution.Tick);
            int seconds = SubscriptionManager.Subscriptions.Count(x => x.Resolution == Resolution.Second);
            int minutes = SubscriptionManager.Subscriptions.Count(x => x.Resolution == Resolution.Minute);
            Debug(string.Format("Ticks {0} Seconds {1} Minutes {2}", ticks, seconds, minutes));
        }

        private readonly Random _random = new Random();
        private T GetRandomItem<T>(List<T> list)
        {
            return list[_random.Next(list.Count)];
        }

        /// <summary>
        /// TradeBar data event handler
        /// </summary>
        public void OnData(TradeBars data)
        {

        }

        /// <summary>
        /// The stock symbols.
        /// </summary>
        public HashSet<string> StockSymbols = new HashSet<string>
        {
            "ABT",
            "ABBV",
            "ACE",
            "ACN",
            "ACT",
            "ADBE",
            "ADT",
            "AES",
            "AET",
            "AFL",
            "AMG",
            "A",
            "GAS",
            "APD",
            "ARG",
            "AKAM",
            "AA",
            "ALXN",
            "ATI",
            "ALLE",
            "AGN",
            "ADS",
            "ALL",
            "ALTR",
            "MO",
            "AMZN",
            "AEE",
            "AEP",
            "AXP",
            "AIG",
            "AMT",
            "AMP",
            "ABC",
            "AME",
            "AMGN",
            "APH",
            "APC",
            "ADI",
            "AON",
            "APA",
            "AIV",
            "AAPL",
            "AMAT",
            "ADM",
            "AIZ",
            "T",
            "ADSK",
            "ADP",
            "AN",
            "AZO",
            "AVGO",
            "AVB",
            "AVY",
            "AVP",
            "BHI",
            "BLL",
            "BAC",
            "BK",
            "BCR",
            "BAX",
            "BBT",
            "BDX",
            "BBBY",
            "BMS",
            "BRK.B",
            "BBY",
            "BIIB",
            "BLK",
            "HRB",
            "BA",
            "BWA",
            "BXP",
            "BSX",
            "BMY",
            "BRCM",
            "BF.B",
            "CHRW",
            "CA",
            "CVC",
            "COG",
            "CAM",
            "CPB",
            "COF",
            "CAH",
            "CFN",
            "KMX",
            "CCL",
            "CAT",
            "CBG",
            "CBS",
            "CELG",
            "CNP",
            "CTL",
            "CERN",
            "CF",
            "SCHW",
            "MSFT",
            "AAPL",
            "GOOG",
            "IBM",
            "JNJ",
            "TSLA",
            "TWTR",
            "LNKD",
            "FB"
        };

        /// <summary>
        /// The forex symbols.
        /// </summary>
        public HashSet<string> ForexSymbols = new HashSet<string>(Forex.CurrencyPairs);
    }
}