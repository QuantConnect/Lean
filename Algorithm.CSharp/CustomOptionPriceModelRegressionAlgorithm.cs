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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing custom option price model implementation
    /// </summary>
    public class CustomOptionPriceModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var option = AddOption("GOOG");
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.StandardsOnly().Strikes(-2, +2).Expiration(0, 180));
            option.SetPriceModel(new CustomOptionPriceModel());
        }

        public override void OnData(Slice slice)
        {
            if (Portfolio.Invested) return;

            if (slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
            {
                foreach (var contract in chain.Contracts.Values)
                {
                    if (contract.TheoreticalPrice > 0 && contract.LastPrice > 0 && contract.TheoreticalPrice < contract.LastPrice * 0.9m)
                    {
                        MarketOrder(contract.Symbol, 1);
                        break;
                    }
                }
            }
        }

        private class CustomOptionPriceModel : OptionPriceModel
        {
            public override OptionPriceModelResult Evaluate(OptionPriceModelParameters parameters)
            {
                var contract = parameters.Contract;
                var underlying = contract.UnderlyingLastPrice;
                var strike = contract.Strike;

                decimal intrinsicValue;
                if (contract.Right == OptionRight.Call)
                {
                    intrinsicValue = Math.Max(0, underlying - strike);
                }
                else
                {
                    intrinsicValue = Math.Max(0, strike - underlying);
                }

                var theoreticalPrice = intrinsicValue + 1.0m;
                return new OptionPriceModelResult(theoreticalPrice, new SimpleGreeks());
            }
        }

        private class SimpleGreeks : Greeks
        {
            public override decimal Delta => 0.5m;
            public override decimal Gamma => 0.1m;
            public override decimal Theta => -0.05m;
            public override decimal Vega => 0.2m;
            public override decimal Rho => 0.1m;
            public override decimal Lambda => 2.0m;
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
            {"End Equity", "99864"},
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
            {"Estimated Strategy Capacity", "$16000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAT67A|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "1.66%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "7093bc566bb36a6db9bf9c940b30e2fd"}
        };
    }
}