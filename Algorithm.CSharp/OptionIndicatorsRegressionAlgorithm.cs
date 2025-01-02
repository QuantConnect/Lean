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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm illustrating the usage of the <see cref="OptionIndicatorBase"/> indicators
    /// </summary>
    public class OptionIndicatorsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private ImpliedVolatility _impliedVolatility;
        private Delta _delta;
        private Gamma _gamma;
        private Vega _vega;
        private Theta _theta;
        private Rho _rho;

        protected virtual string ExpectedGreeks { get; set; } = "Implied Volatility: 0.44529,Delta: -0.00921,Gamma: 0.00036,Vega: 0.03636,Theta: -0.03747,Rho: 0.00047";

        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);
            SetEndDate(2014, 6, 7);
            SetCash(100000);

            AddEquity("AAPL", Resolution.Minute);
            var option = QuantConnect.Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Put, 505m, new DateTime(2014, 6, 27));
            AddOptionContract(option, Resolution.Minute);

            InitializeIndicators(option);
        }

        protected void InitializeIndicators(Symbol option)
        {
            _impliedVolatility = IV(option);
            _delta = D(option, optionModel: OptionPricingModelType.BinomialCoxRossRubinstein, ivModel: OptionPricingModelType.BlackScholes);
            _gamma = G(option, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);
            _vega = V(option, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);
            _theta = T(option, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);
            _rho = R(option, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);
        }

        public override void OnEndOfAlgorithm()
        {
            if (_impliedVolatility == 0m || _delta == 0m || _gamma == 0m || _vega == 0m || _theta == 0m || _rho == 0m)
            {
                throw new RegressionTestException("Expected IV/greeks calculated");
            }
            var result = @$"Implied Volatility: {_impliedVolatility},Delta: {_delta},Gamma: {_gamma},Vega: {_vega},Theta: {_theta},Rho: {_rho}";

            Debug(result);
            if (result != ExpectedGreeks)
            {
                throw new RegressionTestException($"Unexpected greek values {result}. Expected {ExpectedGreeks}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 1974;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
