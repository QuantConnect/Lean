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
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the algorithm-level <see cref="QCAlgorithm.SetSlippageModel(ISlippageModel)"/>
    /// applies a custom <see cref="SlippageModel"/> subclass to all securities,
    /// with per-security models set afterwards taking precedence
    /// </summary>
    public class AlgorithmSlippageModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private CustomSlippageModel _slippageModel;
        private Symbol _spy;
        private Symbol _ibm;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            SetSecurityInitializer(new BrokerageModelSecurityInitializer(BrokerageModel, new FuncSecuritySeeder(GetLastKnownPrices)));

            _slippageModel = new CustomSlippageModel();
            SetSlippageModel(_slippageModel);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;
            var ibm = AddEquity("IBM", Resolution.Minute);
            // per-security models set after the algorithm-level model take precedence for that security
            ibm.SetSlippageModel(NullSlippageModel.Instance);
            _ibm = ibm.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 0.5m);
                SetHoldings(_ibm, 0.5m);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Securities[_spy].SlippageModel != _slippageModel)
            {
                throw new RegressionTestException("Expected SPY to use the algorithm-level slippage model");
            }
            if (Securities[_ibm].SlippageModel != NullSlippageModel.Instance)
            {
                throw new RegressionTestException("Expected the per-security slippage model to take precedence for IBM");
            }
            if (_slippageModel.CallCount == 0)
            {
                throw new RegressionTestException("Expected the algorithm-level slippage model to have been used");
            }
        }

        private class CustomSlippageModel : SlippageModel
        {
            public int CallCount { get; private set; }

            public override decimal GetSlippageApproximation(Security asset, Order order)
            {
                CallCount++;
                return 0.05m;
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
        public long DataPoints => 7843;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 20;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "343.438%"},
            {"Drawdown", "2.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101922.49"},
            {"Net Profit", "1.922%"},
            {"Sharpe Ratio", "10.891"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "66.279%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.565"},
            {"Beta", "0.993"},
            {"Annual Standard Deviation", "0.232"},
            {"Annual Variance", "0.054"},
            {"Information Ratio", "7.794"},
            {"Tracking Error", "0.071"},
            {"Treynor Ratio", "2.545"},
            {"Total Fees", "$3.55"},
            {"Estimated Strategy Capacity", "$16000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "19.93%"},
            {"Drawdown Recovery", "3"},
            {"OrderListHash", "c4766cde15ad208b5f6c12c6a0af59b9"}
        };
    }
}
