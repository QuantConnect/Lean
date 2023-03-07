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
        private decimal _expectedSpyQuantity;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 04, 04);
            SetEndDate(2018, 04, 06);
            SetCash(100000);

            SetExecution(new ImmediateExecutionModel());
            UniverseSettings.Resolution = Resolution.Hour;
            Settings.MinimumOrderMarginPortfolioPercentage = 0.001m;
            SetPortfolioConstruction(new EqualWeightingAlphaStreamsPortfolioConstructionModel());

            // AAPL should be liquidated since it's not hold by the alpha
            // This is handled by the PCM
            var aapl = AddEquity("AAPL", Resolution.Hour);
            aapl.Holdings.SetHoldings(40, 10);

            // SPY will be bought following the alpha streams portfolio
            // This is handled by the PCM + Execution Model
            var spy = AddEquity("SPY", Resolution.Hour);
            spy.Holdings.SetHoldings(246, -10);

            AddData<AlphaStreamsPortfolioState>("94d820a93fff127fa46c15231d");
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (_expectedSpyQuantity == 0 && orderEvent.Symbol == "SPY" && orderEvent.Status == OrderStatus.Filled)
            {
                var security = Securities["SPY"];
                var priceInAccountCurrency = Portfolio.CashBook.ConvertToAccountCurrency(security.AskPrice, security.QuoteCurrency.Symbol);
                _expectedSpyQuantity = (Portfolio.TotalPortfolioValue - Settings.FreePortfolioValue) / priceInAccountCurrency;
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

            // we allow some padding for small price differences
            if (Math.Abs(Securities["SPY"].Holdings.Quantity - _expectedSpyQuantity) > _expectedSpyQuantity * 0.03m)
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 2313;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0.01%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-87.617%"},
            {"Drawdown", "3.100%"},
            {"Expectancy", "8.518"},
            {"Net Profit", "-1.515%"},
            {"Sharpe Ratio", "-2.45"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "18.04"},
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
            {"Portfolio Turnover", "34.12%"},
            {"OrderListHash", "788eb2c74715a78476ba0db3b2654eb6"}
        };
    }
}
