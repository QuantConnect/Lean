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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm aims to test the TotalPortfolioValue,
    /// verifying its correctly updated (GH issue 3272)
    /// </summary>
    public class  TotalPortfolioValueRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<Symbol> _symbols = new List<Symbol>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2016, 1, 1);
            SetEndDate(2017, 1, 1);
            SetCash(100000);

            var securitiesToAdd = new List<string>
            {
                "SPY", "AAPL", "AAA", "GOOG", "GOOGL", "IBM", "QQQ", "FB", "WM", "WMI", "BAC", "USO", "IWM", "EEM", "BNO", "AIG"
            };
            foreach (var symbolStr in securitiesToAdd)
            {
                var security = AddEquity(symbolStr, Resolution.Daily);
                security.SetLeverage(100);
                _symbols.Add(security.Symbol);
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (Portfolio.Invested)
            {
                Liquidate();
            }
            else
            {
                foreach (var symbol in _symbols)
                {
                    SetHoldings(symbol, 10m / _symbols.Count);
                }

                // We will add some cash just for testing, users should not do this
                var totalPortfolioValueSnapshot = Portfolio.TotalPortfolioValue;
                var accountCurrencyCash = Portfolio.CashBook[AccountCurrency];
                var existingAmount = accountCurrencyCash.Amount;

                // increase cash amount
                Portfolio.CashBook.Add(AccountCurrency, existingAmount * 1.1m, 1);

                if (totalPortfolioValueSnapshot * 1.1m != Portfolio.TotalPortfolioValue)
                {
                    throw new RegressionTestException($"Unexpected TotalPortfolioValue {Portfolio.TotalPortfolioValue}." +
                        $" Expected: {totalPortfolioValueSnapshot * 1.1m}");
                }

                // lets remove part of what we added
                Portfolio.CashBook[AccountCurrency].AddAmount(-existingAmount * 0.05m);

                if (totalPortfolioValueSnapshot * 1.05m != Portfolio.TotalPortfolioValue)
                {
                    throw new RegressionTestException($"Unexpected TotalPortfolioValue {Portfolio.TotalPortfolioValue}." +
                        $" Expected: {totalPortfolioValueSnapshot * 1.05m}");
                }

                // lets set amount back to original value
                Portfolio.CashBook[AccountCurrency].SetAmount(existingAmount);
                if (totalPortfolioValueSnapshot != Portfolio.TotalPortfolioValue)
                {
                    throw new RegressionTestException($"Unexpected TotalPortfolioValue {Portfolio.TotalPortfolioValue}." +
                        $" Expected: {totalPortfolioValueSnapshot}");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5331;

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
            {"Total Orders", "3752"},
            {"Average Win", "0.63%"},
            {"Average Loss", "-0.73%"},
            {"Compounding Annual Return", "-5.190%"},
            {"Drawdown", "58.600%"},
            {"Expectancy", "0.007"},
            {"Start Equity", "100000"},
            {"End Equity", "94814.26"},
            {"Net Profit", "-5.186%"},
            {"Sharpe Ratio", "0.439"},
            {"Sortino Ratio", "0.44"},
            {"Probabilistic Sharpe Ratio", "23.379%"},
            {"Loss Rate", "46%"},
            {"Win Rate", "54%"},
            {"Profit-Loss Ratio", "0.86"},
            {"Alpha", "-0.014"},
            {"Beta", "4.75"},
            {"Annual Standard Deviation", "0.811"},
            {"Annual Variance", "0.658"},
            {"Information Ratio", "0.372"},
            {"Tracking Error", "0.746"},
            {"Treynor Ratio", "0.075"},
            {"Total Fees", "$18907.56"},
            {"Estimated Strategy Capacity", "$410000.00"},
            {"Lowest Capacity Asset", "BNO UN3IMQ2JU1YD"},
            {"Portfolio Turnover", "601.23%"},
            {"OrderListHash", "7e35def0ca91b89579b42cf23ef941e2"}
        };
    }
}
