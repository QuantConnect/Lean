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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm illustrating the usage of the <see cref="OptionIndicatorBase"/> indicators
    /// </summary>
    public class OptionIndicatorsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aapl;
        private Symbol _option;
        private ImpliedVolatility _impliedVolatility;
        private Delta _delta;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);
            SetEndDate(2014, 6, 7);
            SetCash(100000);

            _aapl = AddEquity("AAPL", Resolution.Daily).Symbol;
            _option = QuantConnect.Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Put, 505m, new DateTime(2014, 6, 27));
            AddOptionContract(_option);

            var interestRateProvider = new InterestRateProvider();
            var dividendYieldProvider = new DividendYieldProvider(_aapl);

            _impliedVolatility = new ImpliedVolatility(_option, interestRateProvider, dividendYieldProvider, OptionPricingModelType.BlackScholes, 2);
            _delta = new Delta(_option, interestRateProvider, dividendYieldProvider, OptionPricingModelType.BinomialCoxRossRubinstein, OptionPricingModelType.BlackScholes);
        }

        public override void OnData(Slice slice)
        {
            if (slice.Bars.ContainsKey(_aapl) && slice.QuoteBars.ContainsKey(_option))
            {
                var underlyingDataPoint = new IndicatorDataPoint(_aapl, slice.Time, slice.Bars[_aapl].Close);
                var optionDataPoint = new IndicatorDataPoint(_option, slice.Time, slice.QuoteBars[_option].Close);

                _impliedVolatility.Update(underlyingDataPoint);
                _impliedVolatility.Update(optionDataPoint);

                _delta.Update(underlyingDataPoint);
                _delta.Update(optionDataPoint);
            }    
        }

        public override void OnEndOfAlgorithm()
        {
            if (_impliedVolatility == 0m || _delta == 0m)
            {
                throw new Exception("Expected IV/greeks calculated");
            }
            Debug(@$"Implied Volatility: {_impliedVolatility.Current.Value},
Delta: {_delta.Current.Value}");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1197;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
