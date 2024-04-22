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
using QuantConnect.Data.Custom.IconicTypes;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm ensures that added data matches expectations
    /// </summary>
    public class CustomDataIconicTypesAddDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _googlEquity;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            var twxEquity = AddEquity("TWX", Resolution.Daily).Symbol;
            var customTwxSymbol = AddData<LinkedData>(twxEquity, Resolution.Daily).Symbol;

            _googlEquity = AddEquity("GOOGL", Resolution.Daily).Symbol;
            var customGooglSymbol = AddData<LinkedData>("GOOGL", Resolution.Daily).Symbol;

            var unlinkedDataSymbol = AddData<UnlinkedData>("GOOGL", Resolution.Daily).Symbol;
            var unlinkedDataSymbolUnderlyingEquity = QuantConnect.Symbol.Create("MSFT", SecurityType.Equity, Market.USA);
            var unlinkedDataSymbolUnderlying = AddData<UnlinkedData>(unlinkedDataSymbolUnderlyingEquity, Resolution.Daily).Symbol;

            var optionSymbol = AddOption("TWX", Resolution.Minute).Symbol;
            var customOptionSymbol = AddData<LinkedData>(optionSymbol, Resolution.Daily).Symbol;

            if (customTwxSymbol.Underlying != twxEquity)
            {
                throw new Exception($"Underlying symbol for {customTwxSymbol} is not equal to TWX equity. Expected {twxEquity} got {customTwxSymbol.Underlying}");
            }
            if (customGooglSymbol.Underlying != _googlEquity)
            {
                throw new Exception($"Underlying symbol for {customGooglSymbol} is not equal to GOOGL equity. Expected {_googlEquity} got {customGooglSymbol.Underlying}");
            }
            if (unlinkedDataSymbol.HasUnderlying)
            {
                throw new Exception($"Unlinked data type (no underlying) has underlying when it shouldn't. Found {unlinkedDataSymbol.Underlying}");
            }
            if (!unlinkedDataSymbolUnderlying.HasUnderlying)
            {
                throw new Exception("Unlinked data type (with underlying) has no underlying Symbol even though we added with Symbol");
            }
            if (unlinkedDataSymbolUnderlying.Underlying != unlinkedDataSymbolUnderlyingEquity)
            {
                throw new Exception($"Unlinked data type underlying does not equal equity Symbol added. Expected {unlinkedDataSymbolUnderlyingEquity} got {unlinkedDataSymbolUnderlying.Underlying}");
            }
            if (customOptionSymbol.Underlying != optionSymbol)
            {
                throw new Exception("Option symbol not equal to custom underlying symbol. Expected {optionSymbol} got {customOptionSymbol.Underlying}");
            }

            try
            {
                var customDataNoCache = AddData<LinkedData>("AAPL", Resolution.Daily);
                throw new Exception("AAPL was found in the SymbolCache, though it should be missing");
            }
            catch (InvalidOperationException)
            {
                // This is exactly what we wanted. AAPL shouldn't have been found in the SymbolCache, and because
                // LinkedData is mappable, we threw
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 49;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "32.142%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100382.52"},
            {"Net Profit", "0.383%"},
            {"Sharpe Ratio", "2.947"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "56.825%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.515"},
            {"Beta", "0.396"},
            {"Annual Standard Deviation", "0.091"},
            {"Annual Variance", "0.008"},
            {"Information Ratio", "-12.534"},
            {"Tracking Error", "0.136"},
            {"Treynor Ratio", "0.677"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$120000000.00"},
            {"Lowest Capacity Asset", "GOOG T1AZ164W5VTX"},
            {"Portfolio Turnover", "10.02%"},
            {"OrderListHash", "1811ed7e03d5561062ffa04295fe033a"}
        };
    }
}
