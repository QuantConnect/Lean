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
using QuantConnect.Orders;
using System.Collections.Generic;
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm with existing holdings consuming an alpha streams portfolio state and trading based on it
    /// </summary>
    public class AlphaStreamsWithHoldingsBasicTemplateAlgorithm : AlphaStreamsBasicTemplateAlgorithm
    {
        private decimal _initialCash = 100000;
        private decimal _expectedSpyQuantity;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 04, 04);
            SetEndDate(2018, 04, 06);
            SetCash(_initialCash);

            SetExecution(new ImmediateExecutionModel());
            UniverseSettings.Resolution = Resolution.Hour;
            Settings.MinimumOrderMarginPortfolioPercentage = 0.001m;
            SetPortfolioConstruction(new EqualWeightingAlphaStreamsPortfolioConstructionModel());

            // AAPL should be liquidated since it's not hold by the alpha
            // This is handled by the PCM
            var aapl = AddEquity("AAPL", Resolution.Hour);
            aapl.Holdings.SetHoldings(100, 10);

            // SPY will be bought following the alpha streams portfolio
            // This is handled by the PCM + Execution Model
            var spy = AddEquity("SPY", Resolution.Hour);
            spy.Holdings.SetHoldings(100, -10);

            AddData<AlphaStreamsPortfolioState>("94d820a93fff127fa46c15231d");
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (_expectedSpyQuantity == 0 && orderEvent.Symbol == "SPY")
            {
                var security = Securities["SPY"];
                var priceInAccountCurrency = security.AskPrice * security.QuoteCurrency.ConversionRate;
                _expectedSpyQuantity = (_initialCash * (1 - Settings.FreePortfolioValuePercentage) - priceInAccountCurrency) / priceInAccountCurrency;
                _expectedSpyQuantity = _expectedSpyQuantity.DiscretelyRoundBy(1, MidpointRounding.ToZero);
            }

            base.OnOrderEvent(orderEvent);
        }

        public override void OnEndOfAlgorithm()
        {
            if (Securities["AAPL"].HoldStock)
            {
                throw new Exception("We should no longer hold AAPL since the alpha does not");
            }

            if (Securities["SPY"].Holdings.Quantity != _expectedSpyQuantity)
            {
                throw new Exception($"Unexpected SPY holdings. Expected {_expectedSpyQuantity} was {Securities["SPY"].Holdings.Quantity}");
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
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-1.03%"},
            {"Compounding Annual Return", "-87.617%"},
            {"Drawdown", "3.100%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-1.515%"},
            {"Sharpe Ratio", "-2.45"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.008"},
            {"Beta", "1.015"},
            {"Annual Standard Deviation", "0.344"},
            {"Annual Variance", "0.118"},
            {"Information Ratio", "-0.856"},
            {"Tracking Error", "0.005"},
            {"Treynor Ratio", "-0.83"},
            {"Total Fees", "$3.09"},
            {"Estimated Strategy Capacity", "$8900000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Fitness Score", "0.511"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "6113.173"},
            {"Portfolio Turnover", "0.511"},
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
            {"OrderListHash", "788eb2c74715a78476ba0db3b2654eb6"}
        };
    }
}
