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

using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Orders;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of using custom margin interest rate model in backtesting.
    /// </summary>
    /// <meta name="tag" content="custom margin interest rate models" />
    public class CustomMarginInterestRateModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        private decimal _cashAfterOrder;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 01);
            SetEndDate(2013, 10, 31);

            var security = AddEquity("SPY", Resolution.Hour);
            _spy = security.Symbol;

            // set the margin interest rate model
            security.SetMarginInterestRateModel(new CustomMarginInterestRateModel());
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 1);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                _cashAfterOrder = Portfolio.Cash;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var security = Securities[_spy];
            var marginInterestRateModel = security.MarginInterestRateModel as CustomMarginInterestRateModel;

            if (marginInterestRateModel == null)
            {
                throw new RegressionTestException("CustomMarginInterestRateModel was not set");
            }

            if (marginInterestRateModel.CallCount == 0)
            {
                throw new RegressionTestException("CustomMarginInterestRateModel was not called");
            }

            var expectedCash = _cashAfterOrder * (decimal)Math.Pow(1 + (double)marginInterestRateModel.InterestRate, marginInterestRateModel.CallCount);

            // add a tolerance since using Math.Pow(double, double) given the lack of a decimal overload
            if (Math.Abs(Portfolio.Cash - expectedCash) > 1e-10m)
            {
                throw new RegressionTestException($"Expected cash {expectedCash} but got {Portfolio.Cash}");
            }
        }

        public class CustomMarginInterestRateModel : IMarginInterestRateModel
        {
            public decimal InterestRate { get; } = 0.01m;

            public int CallCount { get; private set; }

            public void ApplyMarginInterestRate(MarginInterestRateParameters marginInterestRateParameters)
            {
                var security = marginInterestRateParameters.Security;
                var positionValue = security.Holdings.GetQuantityValue(security.Holdings.Quantity);

                if (positionValue.Amount > 0)
                {
                    positionValue.Cash.AddAmount(InterestRate * positionValue.Cash.Amount);
                    CallCount++;
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 330;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "93.409%"},
            {"Drawdown", "2.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "105698.63"},
            {"Net Profit", "5.699%"},
            {"Sharpe Ratio", "4.701"},
            {"Sortino Ratio", "9.153"},
            {"Probabilistic Sharpe Ratio", "85.653%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.145"},
            {"Beta", "0.998"},
            {"Annual Standard Deviation", "0.108"},
            {"Annual Variance", "0.012"},
            {"Information Ratio", "28.436"},
            {"Tracking Error", "0.005"},
            {"Treynor Ratio", "0.506"},
            {"Total Fees", "$3.43"},
            {"Estimated Strategy Capacity", "$150000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "3.19%"},
            {"OrderListHash", "c0205e9d3d1bfdee958fecccb36413ec"}
        };
    }
}
