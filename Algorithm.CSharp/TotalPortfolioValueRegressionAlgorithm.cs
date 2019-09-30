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
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
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
                    throw new Exception($"Unexpected TotalPortfolioValue {Portfolio.TotalPortfolioValue}." +
                        $" Expected: {totalPortfolioValueSnapshot * 1.1m}");
                }

                // lets remove part of what we added
                Portfolio.CashBook[AccountCurrency].AddAmount(-existingAmount * 0.05m);

                if (totalPortfolioValueSnapshot * 1.05m != Portfolio.TotalPortfolioValue)
                {
                    throw new Exception($"Unexpected TotalPortfolioValue {Portfolio.TotalPortfolioValue}." +
                        $" Expected: {totalPortfolioValueSnapshot * 1.05m}");
                }

                // lets set amount back to original value
                Portfolio.CashBook[AccountCurrency].SetAmount(existingAmount);
                if (totalPortfolioValueSnapshot != Portfolio.TotalPortfolioValue)
                {
                    throw new Exception($"Unexpected TotalPortfolioValue {Portfolio.TotalPortfolioValue}." +
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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3655"},
            {"Average Win", "0.66%"},
            {"Average Loss", "-0.75%"},
            {"Compounding Annual Return", "-18.179%"},
            {"Drawdown", "64.900%"},
            {"Expectancy", "-0.004"},
            {"Net Profit", "-18.179%"},
            {"Sharpe Ratio", "0.319"},
            {"Loss Rate", "47%"},
            {"Win Rate", "53%"},
            {"Profit-Loss Ratio", "0.89"},
            {"Alpha", "-0.274"},
            {"Beta", "4.918"},
            {"Annual Standard Deviation", "0.981"},
            {"Annual Variance", "0.962"},
            {"Information Ratio", "0.214"},
            {"Tracking Error", "0.903"},
            {"Treynor Ratio", "0.064"},
            {"Total Fees", "$19980.73"}
        };
    }
}
