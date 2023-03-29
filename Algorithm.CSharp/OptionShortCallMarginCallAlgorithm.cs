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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that the <see cref="QCAlgorithm.OnMarginCall"/> event is fired when trading options
    /// </summary>
    public class OptionShortCallMarginCallAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionContractSymbol;

        private bool _onMarginCallWasCalled;

        private bool _orderPlaced;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2015, 12, 23);
            SetCash(150000);

            var equitySymbol = AddEquity("GOOG").Symbol;
            var optionSymbol = QuantConnect.Symbol.CreateOption(
                equitySymbol,
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                760,
                new DateTime(2015, 12, 24));
            var optionContract = AddOptionContract(optionSymbol);
            _optionContractSymbol = optionContract.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!_orderPlaced &&
                !Portfolio.Securities[_optionContractSymbol].Invested &&
                slice.OptionChains.Count > 0)
            {
                MarketOrder(_optionContractSymbol, -10);
                _orderPlaced = true;
            }
        }

        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            Debug($"OnMarginCall at {Time}");
            _onMarginCallWasCalled = true;

            if (requests.Count != 1)
            {
                throw new Exception($"OnMarginCall was called with {requests.Count} requests, expected 1");
            }

            if (requests[0].Symbol != _optionContractSymbol)
            {
                throw new Exception($"OnMarginCall was called for {requests[0].Symbol}, expected {_optionContractSymbol}");
            }

            if (requests[0].Quantity != -Portfolio.Securities[_optionContractSymbol].Holdings.Quantity)
            {
                throw new Exception($@"OnMarginCall was called with quantity {requests[0].Quantity}, expected {
                    -Portfolio.Securities[_optionContractSymbol].Holdings.Quantity}");
            }
        }

        public override void OnMarginCallWarning()
        {
            throw new Exception("Expected OnMarginCallWarning to not be invoked");
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_onMarginCallWasCalled)
            {
                throw new Exception("Expected OnMarginCall to be invoked");
            }

            if (!_orderPlaced)
            {
                throw new Exception("Expected an initial order to be placed");
            }

            if (Portfolio.Invested)
            {
                throw new Exception("Expected to be fully liquidated");
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
        public long DataPoints => 1574;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$5.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "GOOCV W6NBKMCY0IH2|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "0.73%"},
            {"OrderListHash", "6ef62152288a4e29a3de54fca607e022"}
        };
    }
}
