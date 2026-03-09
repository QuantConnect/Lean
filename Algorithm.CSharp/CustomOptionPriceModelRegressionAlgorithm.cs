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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test the creation and usage of a custom option price model
    /// </summary>
    public class CustomOptionPriceModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;
        private CustomOptionPriceModel _optionPriceModel;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);

            var option = AddOption("GOOG");
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.StandardsOnly().Strikes(-2, +2).Expiration(0, 180));
            _optionPriceModel = new CustomOptionPriceModel();
            option.SetPriceModel(_optionPriceModel);
        }

        public override void OnData(Slice slice)
        {
            if (Portfolio.Invested)
            {
                return;
            }

            if (slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
            {
                var underlyingPrice = chain.Underlying.Price;
                var atmContract = chain
                    .OrderByDescending(x => x.Expiry)
                    .ThenBy(x => Math.Abs(chain.Underlying.Price - x.Strike))
                    .ThenByDescending(x => x.Right)
                    .FirstOrDefault();

                if (atmContract != null && atmContract.TheoreticalPrice > 0)
                {
                    MarketOrder(atmContract.Symbol, 1);
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_optionPriceModel.EvaluationCount == 0)
            {
                throw new RegressionTestException("CustomOptionPriceModel.Evaluate() was never called");
            }
        }

        private class CustomOptionPriceModel : IOptionPriceModel
        {
            public int EvaluationCount { get; private set; }
            public OptionPriceModelResult Evaluate(OptionPriceModelParameters parameters)
            {
                EvaluationCount++;
                var contract = parameters.Contract;
                var underlying = contract.UnderlyingLastPrice;
                var strike = contract.Strike;
                var greeks = new Greeks(0.5m, 0.2m, 0.15m, 0.05m, 0.1m, 2.0m);

                decimal intrinsicValue;
                if (contract.Right == OptionRight.Call)
                {
                    intrinsicValue = Math.Max(0, underlying - strike);
                }
                else
                {
                    intrinsicValue = Math.Max(0, strike - underlying);
                    // Delta and Rho are negative for a put
                    greeks.Delta *= -1;
                    greeks.Rho *= -1;
                }
                var theoreticalPrice = intrinsicValue + 1.0m;
                var impliedVolatility = 0.2m;

                return new OptionPriceModelResult(theoreticalPrice, impliedVolatility, greeks);
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
        public long DataPoints => 15023;

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
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99799"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
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
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$2600000.00"},
            {"Lowest Capacity Asset", "GOOCV 30AKMEIPOX2DI|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "5.49%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "1925127010d4a935c1efe4bce0375c15"}
        };
    }
}
