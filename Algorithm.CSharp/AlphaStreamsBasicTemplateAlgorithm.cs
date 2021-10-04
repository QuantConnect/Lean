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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm consuming an alpha streams portfolio state and trading based on it
    /// </summary>
    public class AlphaStreamsBasicTemplateAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Dictionary<Symbol, HashSet<Symbol>> _symbolsPerAlpha = new Dictionary<Symbol, HashSet<Symbol>>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 04, 04);
            SetEndDate(2018, 04, 06);

            SetExecution(new ImmediateExecutionModel());
            Settings.MinimumOrderMarginPortfolioPercentage = 0.01m;
            SetPortfolioConstruction(new EqualWeightingAlphaStreamsPortfolioConstructionModel());

            SetSecurityInitializer(new BrokerageModelSecurityInitializer(BrokerageModel,
                new FuncSecuritySeeder(GetLastKnownPrices)));

            foreach (var alphaId in new [] { "623b06b231eb1cc1aa3643a46", "9fc8ef73792331b11dbd5429a" })
            {
                AddData<AlphaStreamsPortfolioState>(alphaId);
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            foreach (var portfolioState in data.Get<AlphaStreamsPortfolioState>().Values)
            {
                ProcessPortfolioState(portfolioState);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($"OnOrderEvent: {orderEvent}");
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            changes.FilterCustomSecurities = false;
            foreach (var addedSecurity in changes.AddedSecurities)
            {
                if (addedSecurity.Symbol.IsCustomDataType<AlphaStreamsPortfolioState>())
                {
                    if (!_symbolsPerAlpha.ContainsKey(addedSecurity.Symbol))
                    {
                        _symbolsPerAlpha[addedSecurity.Symbol] = new HashSet<Symbol>();
                    }
                    // warmup alpha state, adding target securities
                    ProcessPortfolioState(addedSecurity.Cache.GetData<AlphaStreamsPortfolioState>());
                }
            }

            Log($"OnSecuritiesChanged: {changes}");
        }

        private bool UsedBySomeAlpha(Symbol asset)
        {
            return _symbolsPerAlpha.Any(pair => pair.Value.Contains(asset));
        }

        private void ProcessPortfolioState(AlphaStreamsPortfolioState portfolioState)
        {
            if (portfolioState == null)
            {
                return;
            }

            var alphaId = portfolioState.Symbol;
            if (!_symbolsPerAlpha.TryGetValue(alphaId, out var currentSymbols))
            {
                _symbolsPerAlpha[alphaId] = currentSymbols = new HashSet<Symbol>();
            }

            var newSymbols = new HashSet<Symbol>(currentSymbols.Count);
            foreach (var symbol in portfolioState.PositionGroups?.SelectMany(positionGroup => positionGroup.Positions).Select(state => state.Symbol) ?? Enumerable.Empty<Symbol>())
            {
                // only add it if it's not used by any alpha (already added check)
                if (newSymbols.Add(symbol) && !UsedBySomeAlpha(symbol))
                {
                    AddSecurity(symbol, resolution: UniverseSettings.Resolution, extendedMarketHours: UniverseSettings.ExtendedMarketHours);
                }
            }
            _symbolsPerAlpha[alphaId] = newSymbols;

            foreach (var symbol in currentSymbols.Where(symbol => !UsedBySomeAlpha(symbol)))
            {
                RemoveSecurity(symbol);
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
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.12%"},
            {"Compounding Annual Return", "-14.722%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.116%"},
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
            {"Fitness Score", "0.017"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-138.588"},
            {"Portfolio Turnover", "0.034"},
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
            {"OrderListHash", "2b94bc50a74caebe06c075cdab1bc6da"}
        };
    }
}
