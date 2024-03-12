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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp.Alphas
{
    ///<summary>
    /// Alpha Benchmark Strategy capitalizing on ETF rebalancing causing momentum during trending markets.
    /// Strategy by Prof. Shum, reposted by Ernie Chan.
    /// Source: http://epchan.blogspot.com/2012/10/a-leveraged-etfs-strategy.html
    ///</summary>
    /// <meta name="tag" content="alphastream" />
    /// <meta name="tag" content="algorithm framework" />
    /// <meta name="tag" content="etf" />
    public class RebalancingLeveragedETFAlpha : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly List<ETFGroup> Groups = new List<ETFGroup>();

        public override void Initialize()
        {
            SetStartDate(2017, 6, 1);
            SetEndDate(2018, 8, 1);
            SetCash(100000);

            var underlying = new List<string> { "SPY", "QLD", "DIA", "IJR", "MDY", "IWM", "QQQ", "IYE", "EEM", "IYW", "EFA", "GAZB", "SLV", "IEF", "IYM", "IYF", "IYH", "IYR", "IYC", "IBB", "FEZ", "USO", "TLT" };
            var ultraLong = new List<string> { "SSO", "UGL", "DDM", "SAA", "MZZ", "UWM", "QLD", "DIG", "EET", "ROM", "EFO", "BOIL", "AGQ", "UST", "UYM", "UYG", "RXL", "URE", "UCC", "BIB", "ULE", "UCO", "UBT" };
            var ultraShort = new List<string> { "SDS", "GLL", "DXD", "SDD", "MVV", "TWM", "QID", "DUG", "EEV", "REW", "EFU", "KOLD", "ZSL", "PST", "SMN", "SKF", "RXD", "SRS", "SCC", "BIS", "EPV", "SCO", "TBT" };

            for (var i = 0; i < underlying.Count; i++)
            {
                Groups.Add(new ETFGroup(AddEquity(underlying[i]).Symbol, AddEquity(ultraLong[i]).Symbol, AddEquity(ultraShort[i]).Symbol));
            }

            // Manually curated universe
            SetUniverseSelection(new ManualUniverseSelectionModel());

            // Select the demonstration alpha model
            SetAlpha(new RebalancingLeveragedETFAlphaModel(Groups));

            // Select our default model types
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Equally weigh securities in portfolio, based on insights
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Set Immediate Execution Model
            SetExecution(new ImmediateExecutionModel());

            // Set Null Risk Management Model
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

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
            {"Total Orders", "2465"},
            {"Average Win", "0.26%"},
            {"Average Loss", "-0.24%"},
            {"Compounding Annual Return", "7.848%"},
            {"Drawdown", "17.500%"},
            {"Expectancy", "0.035"},
            {"Net Profit", "9.233%"},
            {"Sharpe Ratio", "0.492"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.06"},
            {"Alpha", "0.585"},
            {"Beta", "-24.639"},
            {"Annual Standard Deviation", "0.19"},
            {"Annual Variance", "0.036"},
            {"Information Ratio", "0.387"},
            {"Tracking Error", "0.19"},
            {"Treynor Ratio", "-0.004"},
            {"Total Fees", "$9029.33"}
        };
    }

    /// <summary>
    /// If the underlying ETF has experienced a return >= 1% since the previous day's close up to the current time at 14:15,
    /// then buy it's ultra ETF right away, and exit at the close. If the return is &lt;= -1%, sell it's ultra-short ETF.
    /// </summary>
    class RebalancingLeveragedETFAlphaModel : AlphaModel
    {
        private DateTime _date;
        private readonly List<ETFGroup> _etfGroups;

        /// <summary>
        /// Create a new leveraged ETF rebalancing alpha
        /// </summary>
        public RebalancingLeveragedETFAlphaModel(List<ETFGroup> etfGroups)
        {
            _etfGroups = etfGroups;
            _date = DateTime.MinValue;
            Name = "RebalancingLeveragedETFAlphaModel";
        }

        /// <summary>
        /// Scan to see if the returns are greater than 1% at 2.15pm to emit an insight.
        /// </summary>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            // Initialize:
            var insights = new List<Insight>();
            var magnitude = 0.0005;

            // Paper suggests leveraged ETF's rebalance from 2.15pm - to close
            // giving an insight period of 105 minutes.
            var period = TimeSpan.FromMinutes(105);

            if (algorithm.Time.Date != _date)
            {
                _date = algorithm.Time.Date;

                // Save yesterday's price and reset the signal.
                foreach (var group in _etfGroups)
                {
                    var history = algorithm.History(group.Underlying, 1, Resolution.Daily);
                    group.YesterdayClose = history.Select(x => x.Close).FirstOrDefault();
                }
            }

            // Check if the returns are > 1% at 14.15
            if (algorithm.Time.Hour == 14 && algorithm.Time.Minute == 15)
            {
                foreach (var group in _etfGroups)
                {
                    if (group.YesterdayClose == 0) continue;
                    var returns = (algorithm.Portfolio[group.Underlying].Price - group.YesterdayClose) / group.YesterdayClose;

                    if (returns > 0.01m)
                    {
                        insights.Add(Insight.Price(group.UltraLong, period, InsightDirection.Up, magnitude));
                    }
                    else if (returns < -0.01m)
                    {
                        insights.Add(Insight.Price(group.UltraShort, period, InsightDirection.Down, magnitude));
                    }
                }
            }
            return insights;
        }
    }

    class ETFGroup
    {
        public Symbol Underlying;
        public Symbol UltraLong;
        public Symbol UltraShort;
        public decimal YesterdayClose;

        /// <summary>
        /// Group the underlying ETF and it's ultra ETFs
        /// </summary>
        /// <param name="underlying">The underlying indexETF</param>
        /// <param name="ultraLong">The long-leveraged version of underlying ETF</param>
        /// <param name="ultraShort">The short-leveraged version of the underlying ETF</param>
        public ETFGroup(Symbol underlying, Symbol ultraLong, Symbol ultraShort)
        {
            Underlying = underlying;
            UltraLong = ultraLong;
            UltraShort = ultraShort;
        }
    }
}
