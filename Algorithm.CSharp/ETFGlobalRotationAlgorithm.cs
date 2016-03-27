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
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// ETF Global Rotation Strategy
    /// </summary>
    public class ETFGlobalRotationAlgorithm : QCAlgorithm
    {
        // we'll use this to tell us when the month has ended
        DateTime LastRotationTime = DateTime.MinValue;
        TimeSpan RotationInterval = TimeSpan.FromDays(30);
        private bool first = true;

        // these are the growth symbols we'll rotate through
        List<string> GrowthSymbols = new List<string>
        {
            "MDY", // US S&P mid cap 400
            "IEV", // iShares S&P europe 350
            "EEM", // iShared MSCI emerging markets
            "ILF", // iShares S&P latin america
            "EPP"  // iShared MSCI Pacific ex-Japan
        };

        // these are the safety symbols we go to when things are looking bad for growth
        List<string> SafetySymbols = new List<string>
        {
            "EDV", // Vangaurd TSY 25yr+
            "SHY"  // Barclays Low Duration TSY
        };

        // we'll hold some computed data in these guys
        List<SymbolData> SymbolData = new List<SymbolData>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetCash(25000);
            SetStartDate(2007, 1, 1);

            foreach (var symbol in GrowthSymbols.Union(SafetySymbols))
            {
                // ideally we would use daily data
                AddSecurity(SecurityType.Equity, symbol, Resolution.Minute);
                var oneMonthPerformance = MOM(symbol, 30, Resolution.Daily);
                var threeMonthPerformance = MOM(symbol, 90, Resolution.Daily);

                SymbolData.Add(new SymbolData
                {
                    Symbol = symbol,
                    OneMonthPerformance = oneMonthPerformance,
                    ThreeMonthPerformance = threeMonthPerformance
                });
            }
        }


        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            try
            {
                // the first time we come through here we'll need to do some things such as allocation
                // and initializing our symbol data
                if (first)
                {
                    first = false;
                    LastRotationTime = Time;
                    return;
                }

                var delta = Time.Subtract(LastRotationTime);
                if (delta > RotationInterval)
                {
                    LastRotationTime = Time;

                    // pick which one is best from growth and safety symbols
                    var orderedObjScores = SymbolData.OrderByDescending(x => x.ObjectiveScore).ToList();
                    foreach (var orderedObjScore in orderedObjScores)
                    {
                        Log(">>SCORE>>" + orderedObjScore.Symbol + ">>" + orderedObjScore.ObjectiveScore);
                    }
                    var bestGrowth = orderedObjScores.First();

                    if (bestGrowth.ObjectiveScore > 0)
                    {
                        if (Portfolio[bestGrowth.Symbol].Quantity == 0)
                        {
                            Log("PREBUY>>LIQUIDATE>>");
                            Liquidate();
                        }
                        Log(">>BUY>>" + bestGrowth.Symbol + "@" + (100 * bestGrowth.OneMonthPerformance).ToString("00.00"));
                        decimal qty = Portfolio.Cash / Securities[bestGrowth.Symbol].Close;
                        MarketOrder(bestGrowth.Symbol, (int) qty);
                    }
                    else
                    {
                        // if no one has a good objective score then let's hold cash this month to be safe
                        Log(">>LIQUIDATE>>CASH");
                        Liquidate();
                    }
                }
            }
            catch (Exception ex)
            {
                Error("OnTradeBar: " + ex.Message + "\r\n\r\n" + ex.StackTrace);
            }
        }
    }

    class SymbolData
    {
        public string Symbol;

        public Momentum OneMonthPerformance { get; set; }
        public Momentum ThreeMonthPerformance { get; set; }

        public decimal ObjectiveScore
        {
            get
            {
                // we weight the one month performance higher
                decimal weight1 = 100;
                decimal weight2 = 75;

                return (weight1 * OneMonthPerformance + weight2 * ThreeMonthPerformance) / (weight1 + weight2);
            }
        }
    }
}