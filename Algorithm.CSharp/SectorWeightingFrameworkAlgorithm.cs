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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example algorithm defines its own custom coarse/fine fundamental selection model
    /// with sector weighted portfolio
    /// </summary>
    public class SectorWeightingFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Dictionary<Symbol, decimal> _targets = new Dictionary<Symbol, decimal>();

        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 04, 02);
            SetEndDate(2014, 04, 06);
            SetCash(100000);

            SetUniverseSelection(new FineFundamentalUniverseSelectionModel(SelectCoarse, SelectFine));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, QuantConnect.Time.OneDay));
            SetPortfolioConstruction(new SectorWeightingPortfolioConstructionModel());

            Func<string, Symbol> toSymbol = t => QuantConnect.Symbol.Create(t, SecurityType.Equity, Market.USA);
            _targets.Add(toSymbol("AAPL"), .25m);
            _targets.Add(toSymbol("AIG"), .5m);
            _targets.Add(toSymbol("IBM"), .25m);
            _targets.Add(toSymbol("GOOG"), .5m);
            _targets.Add(toSymbol("BAC"), .5m);
            _targets.Add(toSymbol("SPY"), 0);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status.IsFill())
            {
                var symbol = orderEvent.Symbol;
                var security = Securities[symbol];

                var absoluteBuyingPower = security.BuyingPowerModel
                    .GetReservedBuyingPowerForPosition(new ReservedBuyingPowerForPositionParameters(security))
                    .AbsoluteUsedBuyingPower   // See GH issue 4107
                    * security.BuyingPowerModel.GetLeverage(security);

                var portfolioShare = absoluteBuyingPower / Portfolio.TotalPortfolioValue;

                Debug($"Order event: {orderEvent}. Absolute buying power: {absoluteBuyingPower}");

                // Checks whether the portfolio share of a given symbol matches its target
                // Only considers the buy orders, because holding value is zero otherwise
                if (Math.Abs(_targets[symbol] - portfolioShare) > 0.01m && orderEvent.Direction == OrderDirection.Buy)
                {
                    throw new Exception($"Target for {symbol}: expected {_targets[symbol]}, actual: {portfolioShare}");
                }
            }
        }

        private IEnumerable<Symbol> SelectCoarse(IEnumerable<CoarseFundamental> coarse)
        {
            return Time.Date < new DateTime(2014, 4, 4)
                // IndustryTemplateCode of AAPL and IBM is N and AIG is I
                ? _targets.Keys.Take(3)
                // IndustryTemplateCode of GOOG is N and BAC is B. SPY have no fundamentals
                : _targets.Keys.Skip(3);
        }

        private IEnumerable<Symbol> SelectFine(IEnumerable<FineFundamental> fine) => fine.Select(f => f.Symbol);

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 58;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "11"},
            {"Average Win", "0.53%"},
            {"Average Loss", "-0.14%"},
            {"Compounding Annual Return", "-98.488%"},
            {"Drawdown", "3.700%"},
            {"Expectancy", "1.374"},
            {"Start Equity", "100000"},
            {"End Equity", "96613.29"},
            {"Net Profit", "-3.387%"},
            {"Sharpe Ratio", "-2.425"},
            {"Sortino Ratio", "-2.425"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "3.75"},
            {"Alpha", "1.887"},
            {"Beta", "3.528"},
            {"Annual Standard Deviation", "0.412"},
            {"Annual Variance", "0.17"},
            {"Information Ratio", "-0.613"},
            {"Tracking Error", "0.295"},
            {"Treynor Ratio", "-0.283"},
            {"Total Fees", "$47.58"},
            {"Estimated Strategy Capacity", "$44000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "102.74%"},
            {"OrderListHash", "fa900c290faf84a346d242c64cf01a69"}
        };
    }
}
