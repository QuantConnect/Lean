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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Strategy example using a portfolio of ETF Global Rotation
    /// </summary>
    /// <meta name="tag" content="strategy example" />
    /// <meta name="tag" content="momentum" />
    /// <meta name="tag" content="using data" />
    public class EtfGlobalRotationAlgorithm : QCAlgorithm
    {
        // we'll use this to tell us when the month has ended
        private DateTime _lastRotationTime = DateTime.MinValue;
        private TimeSpan _rotationInterval = TimeSpan.FromDays(30);
        private bool _first = true;

        // these are the growth symbols we'll rotate through
        List<string> _growthSymbols = new List<string>
        {
            "MDY", // US S&P mid cap 400
            "IEV", // iShares S&P europe 350
            "EEM", // iShared MSCI emerging markets
            "ILF", // iShares S&P latin america
            "EPP"  // iShared MSCI Pacific ex-Japan
        };

        // these are the safety symbols we go to when things are looking bad for growth
        List<string> _safetySymbols = new List<string>
        {
            "EDV", // Vangaurd TSY 25yr+
            "SHY"  // Barclays Low Duration TSY
        };

        // we'll hold some computed data in these guys
        List<SymbolData> _symbolData = new List<SymbolData>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetCash(25000);
            SetStartDate(2007, 1, 1);

            foreach (var symbol in _growthSymbols.Union(_safetySymbols))
            {
                // ideally we would use daily data
                AddSecurity(SecurityType.Equity, symbol, Resolution.Minute);
                var oneMonthPerformance = MOM(symbol, 30, Resolution.Daily);
                var threeMonthPerformance = MOM(symbol, 90, Resolution.Daily);

                _symbolData.Add(new SymbolData
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
                if (_first)
                {
                    _first = false;
                    _lastRotationTime = Time;
                    return;
                }

                var delta = Time.Subtract(_lastRotationTime);
                if (delta > _rotationInterval)
                {
                    _lastRotationTime = Time;

                    // pick which one is best from growth and safety symbols
                    var orderedObjScores = _symbolData.OrderByDescending(x => x.ObjectiveScore).ToList();
                    foreach (var orderedObjScore in orderedObjScores)
                    {
                        Log($">>SCORE>>{orderedObjScore.Symbol}>>{orderedObjScore.ObjectiveScore.ToStringInvariant()}");
                    }
                    var bestGrowth = orderedObjScores.First();

                    if (bestGrowth.ObjectiveScore > 0)
                    {
                        if (Portfolio[bestGrowth.Symbol].Quantity == 0)
                        {
                            Log("PREBUY>>LIQUIDATE>>");
                            Liquidate();
                        }
                        Log($">>BUY>>{bestGrowth.Symbol}@{(100 * bestGrowth.OneMonthPerformance).ToStringInvariant("00.00")}");
                        var qty = Portfolio.MarginRemaining / Securities[bestGrowth.Symbol].Close;
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