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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm consuming an alpha streams portfolio state and trading based on it
    /// </summary>
    public class AlphaStreamsBasicTemplateAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<Symbol> _currentSymbols;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 04, 04);
            SetEndDate(2018, 04, 06);

            _currentSymbols = new List<Symbol>();
            SetExecution(new ImmediateExecutionModel());
            Settings.MinimumOrderMarginPortfolioPercentage = 0.01m;
            SetPortfolioConstruction(new SecurityTargetPortfolioConstructionModel());
            var alpha = AddData<AlphaStreamsPortfolioState>("623b06b231eb1cc1aa3643a46");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (data.ContainsKey("623b06b231eb1cc1aa3643a46"))
            {
                var portfolioState = (AlphaStreamsPortfolioState)data["623b06b231eb1cc1aa3643a46"];
                var newSymbols = new List<Symbol>();
                if (!portfolioState.PositionGroups.IsNullOrEmpty())
                {
                    var portfolioValueFactor = Portfolio.TotalPortfolioValue / portfolioState.TotalPortfolioValue * 1;
                    foreach (var positionGroup in portfolioState.PositionGroups)
                    {
                        foreach (var position in positionGroup.Positions)
                        {
                            var security = AddSecurity(position.Symbol, Resolution.Minute);
                            security.Holdings.Target = new PortfolioTarget(position.Symbol, position.Quantity * portfolioValueFactor);
                            newSymbols.Add(position.Symbol);
                            _currentSymbols.Remove(position.Symbol);
                        }
                    }
                }

                foreach (var symbol in _currentSymbols)
                {
                    Securities[symbol].Holdings.Target = null;
                    Liquidate(symbol);
                    RemoveSecurity(symbol);
                }

                _currentSymbols = newSymbols;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"OnOrderEvent: {orderEvent}");
        }

        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.Invested)
            {
                throw new Exception("Should not be invested at end of algorithm");
            }
        }

        private class SecurityTargetPortfolioConstructionModel : IPortfolioConstructionModel
        {
            public IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
            {
                foreach (var symbol in algorithm.Securities.Keys.Where(symbol => symbol.SecurityType == SecurityType.Base))
                {
                    if (algorithm.CurrentSlice.ContainsKey(symbol))
                    {
                        var portfolioState = (AlphaStreamsPortfolioState)algorithm.CurrentSlice["623b06b231eb1cc1aa3643a46"];
                    }
                }

                foreach (var security in algorithm.Securities.Values)
                {
                    if (security.Holdings.Target != null && security.Holdings.Target.Quantity != security.Holdings.Quantity)
                    {
                        yield return security.Holdings.Target;
                    }
                }
            }
            public void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.23%"},
            {"Compounding Annual Return", "-27.348%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.233%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "2.474"},
            {"Tracking Error", "0.339"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$83000.00"},
            {"Lowest Capacity Asset", "BTCUSD XJ"},
            {"Fitness Score", "0.034"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-127.431"},
            {"Portfolio Turnover", "0.069"},
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
            {"OrderListHash", "d10390e3426c62b1dc637b7b893e34b6"}
        };
    }
}
