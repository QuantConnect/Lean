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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Data.Custom.USTreasury;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm ensures that added data matches expectations
    /// </summary>
    public class CustomDataAddDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _googlEquity;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            var twxEquity = AddEquity("TWX", Resolution.Daily).Symbol;
            var customTwxSymbol = AddData<SECReport8K>(twxEquity, Resolution.Daily).Symbol;

            _googlEquity = AddEquity("GOOGL", Resolution.Daily).Symbol;
            var customGooglSymbol = AddData<SECReport10K>("GOOGL", Resolution.Daily).Symbol;

            var usTreasury = AddData<USTreasuryYieldCurveRate>("GOOGL", Resolution.Daily).Symbol;
            var usTreasuryUnderlyingEquity = QuantConnect.Symbol.Create("MSFT", SecurityType.Equity, Market.USA);
            var usTreasuryUnderlying = AddData<USTreasuryYieldCurveRate>(usTreasuryUnderlyingEquity, Resolution.Daily).Symbol;

            var optionSymbol = AddOption("TWX", Resolution.Minute).Symbol;
            var customOptionSymbol = AddData<SECReport10K>(optionSymbol, Resolution.Daily).Symbol;

            if (customTwxSymbol.Underlying != twxEquity)
            {
                throw new Exception($"Underlying symbol for {customTwxSymbol} is not equal to TWX equity. Expected {twxEquity} got {customTwxSymbol.Underlying}");
            }
            if (customGooglSymbol.Underlying != _googlEquity)
            {
                throw new Exception($"Underlying symbol for {customGooglSymbol} is not equal to GOOGL equity. Expected {_googlEquity} got {customGooglSymbol.Underlying}");
            }
            if (usTreasury.HasUnderlying)
            {
                throw new Exception($"US Treasury yield curve (no underlying) has underlying when it shouldn't. Found {usTreasury.Underlying}");
            }
            if (!usTreasuryUnderlying.HasUnderlying)
            {
                throw new Exception("US Treasury yield curve (with underlying) has no underlying Symbol even though we added with Symbol");
            }
            if (usTreasuryUnderlying.Underlying != usTreasuryUnderlyingEquity)
            {
                throw new Exception($"US Treasury yield curve underlying does not equal equity Symbol added. Expected {usTreasuryUnderlyingEquity} got {usTreasuryUnderlying.Underlying}");
            }
            if (customOptionSymbol.Underlying != optionSymbol)
            {
                throw new Exception("Option symbol not equal to custom underlying symbol. Expected {optionSymbol} got {customOptionSymbol.Underlying}");
            }

            try
            {
                var customDataNoCache = AddData<SECReport10Q>("AAPL", Resolution.Daily);
                throw new Exception("AAPL was found in the SymbolCache, though it should be missing");
            }
            catch (InvalidOperationException)
            {
                // This is exactly what we wanted. AAPL shouldn't have been found in the SymbolCache, and because
                // SECReport10Q is a mappable type, we threw
                return;
            }
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested && !Transactions.GetOpenOrders().Any())
            {
                SetHoldings(_googlEquity, 0.5);
            }
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "31.756%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.378%"},
            {"Sharpe Ratio", "2.708"},
            {"Probabilistic Sharpe Ratio", "56.960%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.079"},
            {"Beta", "0.099"},
            {"Annual Standard Deviation", "0.079"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "-6.058"},
            {"Tracking Error", "0.19"},
            {"Treynor Ratio", "2.159"},
            {"Total Fees", "$1.00"},
            {"Fitness Score", "0.1"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "47.335"},
            {"Portfolio Turnover", "0.1"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "214f38f9084bc350c93010aa2fb69822"}
        };
    }
}
