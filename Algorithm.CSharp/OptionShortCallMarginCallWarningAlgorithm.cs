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
    /// Algorithm asserting that the <see cref="QCAlgorithm.OnMarginCallWarning"/> event is fired when trading options
    /// </summary>
    public class OptionShortCallMarginCallWarningAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionContractSymbol;

        private bool _receivedMarginCallWarning;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2015, 12, 23);
            SetCash(115000);

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
            if (!Portfolio.Securities[_optionContractSymbol].Invested && slice.OptionChains.Count > 0)
            {
                MarketOrder(_optionContractSymbol, -5);
            }
        }

        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            throw new Exception("Expected OnMarginCall to not be invoked");
        }

        public override void OnMarginCallWarning()
        {
            // this code gets called when the margin remaining drops below 5% of our total portfolio value, it gives the algorithm
            // a chance to prevent a margin call from occurring

            // prevent margin calls by responding to the warning and increasing margin remaining
            var security = Securities[_optionContractSymbol];
            var holdings = security.Holdings.Quantity;
            var shares = (int)(-Math.Sign(holdings) * Math.Max(Math.Abs(holdings) * .005m, security.SymbolProperties.LotSize));
            Log($"{Time.ToStringInvariant()} - OnMarginCallWarning(): Liquidating {shares.ToStringInvariant()} shares of the option to avoid margin call.");
            MarketOrder(_optionContractSymbol, shares);

            if (!_receivedMarginCallWarning)
            {
                Debug($"OnMarginCallWarning at {Time}");
                _receivedMarginCallWarning = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_receivedMarginCallWarning)
            {
                throw new Exception("OnMarginCallWarning was not invoked");
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
            {"Total Fees", "$2.25"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "GOOCV W6NBKMCY0IH2|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "0.23%"},
            {"OrderListHash", "dfe06c5ccfcaf488ffee1e200553b891"}
        };
    }
}
