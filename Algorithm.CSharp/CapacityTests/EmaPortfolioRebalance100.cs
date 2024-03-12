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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests a wide variety of liquid and illiquid stocks together, with bins
    /// of 20 ranging from micro-cap to mega-cap stocks.
    /// </summary>
    public class EmaPortfolioRebalance100 : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public List<SymbolData> Data;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 2, 5);
            SetWarmup(1000);
            SetCash(100000);

            Data = new List<SymbolData> {
                new SymbolData(this, AddEquity("AADR", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AAMC", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AAU", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ABDC", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ABIO", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ABUS", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AC", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACER", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACES", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACGLO", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACH", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACHV", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACIO", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACIU", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACNB", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACRS", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACSI", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACT", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACT", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACTG", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZYNE", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZYME", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZUO", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZUMZ", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZTR", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZSL", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZSAN", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZROZ", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZLAB", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZIXI", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZIV", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZIOP", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZGNX", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZG", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZEUS", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZAGG", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("YYY", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("YRD", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("YRCW", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("YPF", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AA", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AAN", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AAP", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AAXN", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ABB", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ABC", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACAD", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACC", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACGL", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACIW", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACM", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACWV", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ACWX", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ADM", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ADPT", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ADS", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ADUS", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AEM", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AEO", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AEP", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ZTS", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("YUM", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("XLY", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("XLV", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("XLRE", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("XLP", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("XLNX", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("XLF", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("XLC", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("XLB", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("XEL", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("XBI", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("X", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("WYNN", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("WW", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("WORK", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("WMB", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("WM", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("WELL", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("WEC", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AAPL", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("ADBE", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AGG", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AMD", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("AMZN", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("BA", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("BABA", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("BAC", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("BMY", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("C", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("CMCSA", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("CRM", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("CSCO", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("DIS", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("EEM", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("EFA", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("FB", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("GDX", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("GE", Resolution.Minute).Symbol),
                new SymbolData(this, AddEquity("SPY", Resolution.Minute).Symbol)
            };
        }

        public override void OnData(Slice data)
        {
            var fastFactor = 0.005m;

            foreach (var sd in Data)
            {
                if (!Portfolio.Invested && sd.Fast * (1 + fastFactor) > sd.Slow)
                {
                    SetHoldings(sd.Symbol, 0.01);
                }
                else if (Portfolio.Invested && sd.Fast * (1 - fastFactor) < sd.Slow)
                {
                    Liquidate(sd.Symbol);
                }
            }
        }

        public class SymbolData
        {
            public Symbol Symbol;
            public ExponentialMovingAverage Fast;
            public ExponentialMovingAverage Slow;
            public bool IsCrossed => Fast > Slow;

            public SymbolData(QCAlgorithm algorithm, Symbol symbol) {
                Symbol = symbol;
                Fast = algorithm.EMA(symbol, 20);
                Slow = algorithm.EMA(symbol, 300);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 0;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1015"},
            {"Average Win", "0.01%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-12.674%"},
            {"Drawdown", "1.400%"},
            {"Expectancy", "-0.761"},
            {"Net Profit", "-1.328%"},
            {"Sharpe Ratio", "-12.258"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "95%"},
            {"Win Rate", "5%"},
            {"Profit-Loss Ratio", "3.67"},
            {"Alpha", "-0.142"},
            {"Beta", "0.038"},
            {"Annual Standard Deviation", "0.01"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-4.389"},
            {"Tracking Error", "0.123"},
            {"Treynor Ratio", "-3.359"},
            {"Total Fees", "$1125.52"},
            {"Estimated Strategy Capacity", "$300.00"},
            {"Fitness Score", "0.007"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-14.315"},
            {"Return Over Maximum Drawdown", "-9.589"},
            {"Portfolio Turnover", "0.406"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "4c165e8d648d54a85bb7b564050a6f85"}
        };
    }
}
