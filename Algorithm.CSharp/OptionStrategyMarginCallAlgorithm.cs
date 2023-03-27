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
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that the <see cref="QCAlgorithm.OnMarginCall"/> event is fired when trading options strateties
    /// </summary>
    public class OptionStrategyMarginCallAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        private OptionStrategy _optionStrategy;

        private bool _receivedMarginCallWarning;

        private bool _onMarginCallWasCalled;

        private bool _orderPlaced;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(150000);

            var equity = AddEquity("GOOG", leverage: 4);
            var option = AddOption(equity.Symbol);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-2, +2)
                .Expiration(0, 180));
        }

        public override void OnData(Slice slice)
        {
            if (!_orderPlaced && !Portfolio.Invested)
            {
                if (IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
                {
                    var callContracts = chain.Where(contract => contract.Right == OptionRight.Call)
                        .GroupBy(x => x.Expiry)
                        .OrderBy(grouping => grouping.Key)
                        .First()
                        .OrderBy(x => x.Strike)
                        .ToList();

                    var expiry = callContracts[0].Expiry;
                    var lowerStrike = callContracts[0].Strike;
                    var middleStrike = callContracts[1].Strike;
                    var higherStrike = callContracts[2].Strike;

                    _optionStrategy = OptionStrategies.Straddle(_optionSymbol, higherStrike, expiry);

                    Order(_optionStrategy, -5);
                    _orderPlaced = true;
                }
            }
        }

        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            Debug($"OnMarginCall at {Time}");
            _onMarginCallWasCalled = true;

            var investedOptions = Portfolio.Securities.Values
                .Where(x => x.Invested && x.Type == SecurityType.Option)
                .ToList();

            if (investedOptions.Count != _optionStrategy.OptionLegs.Count)
            {
                throw new Exception("OnMarginCall was called with a different number of options than the strategy has legs" +
                    $"Expected: {investedOptions.Count}. Actual: {_optionStrategy.OptionLegs.Count}");
            }

            if (requests.Count != investedOptions.Count)
            {
                throw new Exception("OnMarginCall should be called with the same number of requests as the number of legs in the strategy. " +
                    $"Expected: {investedOptions.Count}, Actual: {requests.Count}");
            }

            if (requests.Skip(1).Any(request => request.Symbol == requests[0].Symbol) ||
                requests.Any(request => !investedOptions.Any(option => option.Symbol == request.Symbol)))
            {
                throw new Exception("OnMarginCall should be called with requests for each of regs of the strategy." +
                    $@"Expected: {string.Join(", ", investedOptions.Select(option => option.Symbol))}.Actual: {string.Join(", ", requests.Select(request => request.Symbol))}");
            }
        }

        public override void OnMarginCallWarning()
        {
            Debug($"OnMarginCallWarning at {Time}");
            _receivedMarginCallWarning = true;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_onMarginCallWasCalled)
            {
                throw new Exception("Expected OnMarginCall to be invoked");
            }

            if (_receivedMarginCallWarning)
            {
                throw new Exception("Expected OnMarginCall to not be invoked");
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
        public long DataPoints => 475788;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
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
            {"Total Fees", "$25.00"},
            {"Estimated Strategy Capacity", "$180000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "18.59%"},
            {"OrderListHash", "d301d24a1268d3c9312b3c0138416e65"}
        };
    }
}
