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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of the AutomaticIndicatorWarmUp on option greeks
    /// </summary>
    public class AutomaticIndicatorWarmupOptionIndicatorsMirrorContractsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);

            Settings.AutomaticIndicatorWarmUp = true;

            var underlying = "GOOG";
            var resolution = Resolution.Minute;

            var expiration = new DateTime(2015, 12, 24);
            var strike = 650m;

            var equity = AddEquity(underlying, resolution).Symbol;
            var option = QuantConnect.Symbol.CreateOption(underlying, Market.USA, OptionStyle.American, OptionRight.Put, strike, expiration);
            AddOptionContract(option, resolution);
            // add the call counter side of the mirrored pair
            var mirrorOption = QuantConnect.Symbol.CreateOption(underlying, Market.USA, OptionStyle.American, OptionRight.Call, strike, expiration);
            AddOptionContract(mirrorOption, resolution);

            var impliedVolatility = IV(option, mirrorOption);
            var delta = D(option, mirrorOption, optionModel: OptionPricingModelType.BinomialCoxRossRubinstein, ivModel: OptionPricingModelType.BlackScholes);
            var gamma = G(option, mirrorOption, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);
            var vega = V(option, mirrorOption, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);
            var theta = T(option, mirrorOption, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);
            var rho = R(option, mirrorOption, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);

            if (impliedVolatility == 0m || delta == 0m || gamma == 0m || vega == 0m || theta == 0m || rho == 0m)
            {
                throw new RegressionTestException("Expected IV/greeks calculated");
            }
            if (!impliedVolatility.IsReady || !delta.IsReady || !gamma.IsReady || !vega.IsReady || !theta.IsReady || !rho.IsReady)
            {
                throw new RegressionTestException("Expected IV/greeks to be ready");
            }

            Quit($"Implied Volatility: {impliedVolatility}, Delta: {delta}, Gamma: {gamma}, Vega: {vega}, Theta: {theta}, Rho: {rho}");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 0;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 21;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
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
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
