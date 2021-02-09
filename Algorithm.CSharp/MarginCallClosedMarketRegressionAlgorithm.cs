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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Margin model regression algorithm testing <see cref="PatternDayTradingMarginModel"/> and
    /// margin calls being triggered when the market is about to close, GH issue 4064.
    /// Brother too <see cref="NoMarginCallExpectedRegressionAlgorithm"/>
    /// </summary>
    public class MarginCallClosedMarketRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _marginCall;
        private Symbol _spy;
        private decimal _closedMarketLeverage;
        private decimal _openMarketLeverage;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            var security = AddEquity("SPY", Resolution.Minute);
            _spy = security.Symbol;

            _closedMarketLeverage = 2;
            _openMarketLeverage = 5;
            security.BuyingPowerModel = new PatternDayTradingMarginModel(_closedMarketLeverage, _openMarketLeverage);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, _openMarketLeverage);
            }
        }

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            _marginCall++;
            foreach (var order in requests.ToList())
            {
                var quantityHold = Securities[_spy].Holdings.Quantity;
                // we should reduce our position by the same relation between the open and closed market leverage
                var expectedFinalQuantity = quantityHold * _closedMarketLeverage / _openMarketLeverage;

                var actualFinalQuantity = quantityHold + order.Quantity;

                // leave a 1% margin for are expected calculations
                if (Math.Abs(expectedFinalQuantity - actualFinalQuantity) > (quantityHold * 0.01m))
                {
                    throw new Exception($"Expected {expectedFinalQuantity} final quantity but was {actualFinalQuantity}");
                }

                if (!Securities[_spy].Exchange.ExchangeOpen
                    || !Securities[_spy].Exchange.ClosingSoon)
                {
                    throw new Exception($"Expected exchange to be open: {Securities[_spy].Exchange.ExchangeOpen} and to be closing soon: {Securities[_spy].Exchange.ClosingSoon}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_marginCall != 1)
            {
                throw new Exception($"We expected a single margin call to happen, {_marginCall} occurred");
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
            {"Average Win", "0.39%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "1692.800%"},
            {"Drawdown", "5.500%"},
            {"Expectancy", "0"},
            {"Net Profit", "3.759%"},
            {"Sharpe Ratio", "17.525"},
            {"Probabilistic Sharpe Ratio", "67.464%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "3.885"},
            {"Beta", "2.019"},
            {"Annual Standard Deviation", "0.444"},
            {"Annual Variance", "0.197"},
            {"Information Ratio", "26.114"},
            {"Tracking Error", "0.224"},
            {"Treynor Ratio", "3.851"},
            {"Total Fees", "$26.01"},
            {"Fitness Score", "0.999"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "210.297"},
            {"Portfolio Turnover", "1.981"},
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
            {"OrderListHash", "1e61db721e349935374853dd883790e6"}
        };
    }
}
