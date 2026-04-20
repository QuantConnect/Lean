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
 *
*/

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to override the option pricing model with the
    /// <see cref="QLOptionPriceModel"/> for a given option security.
    /// </summary>
    public class QLOptionPricingModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _checked;

        private Option _option;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var equity = AddEquity("GOOG");
            _option = AddOption(equity.Symbol);
            _option.SetFilter(u => u.Strikes(-2, +2).Expiration(0, 180));

            // Set the option price model to the default QL model
            _option.SetPriceModel(QLOptionPriceModelProvider.Instance.GetOptionPriceModel(_option.Symbol));

            if (_option.PriceModel is not QLOptionPriceModel)
            {
                throw new RegressionTestException("Option pricing model was not set to QLOptionPriceModel, which should be the default");
            }
        }

        public override void OnData(Slice slice)
        {
            if (!_checked  && slice.OptionChains.TryGetValue(_option.Symbol, out var chain))
            {
                if (_option.PriceModel is not QLOptionPriceModel)
                {
                    throw new RegressionTestException("Option pricing model was not set to QLOptionPriceModel");
                }

                foreach (var contract in chain)
                {
                    var theoreticalPrice = contract.TheoreticalPrice;
                    var iv = contract.ImpliedVolatility;
                    var greeks = contract.Greeks;

                    Log($"{contract.Symbol}:: Theoretical Price: {theoreticalPrice}, IV: {iv}, " +
                           $"Delta: {greeks.Delta}, Gamma: {greeks.Gamma}, Vega: {greeks.Vega}, " +
                           $"Theta: {greeks.Theta}, Rho: {greeks.Rho}, Lambda: {greeks.Lambda}");
                                 
                    _checked |= true;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_checked)
            {
                throw new RegressionTestException("Option chain was never received.");
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
        public virtual long DataPoints => 37131;

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
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
