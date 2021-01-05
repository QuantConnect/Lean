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
            {"Total Trades", "3528"},
            {"Average Win", "0.67%"},
            {"Average Loss", "-0.71%"},
            {"Compounding Annual Return", "17.227%"},
            {"Drawdown", "63.700%"},
            {"Expectancy", "0.020"},
            {"Net Profit", "17.227%"},
            {"Sharpe Ratio", "0.834"},
            {"Probabilistic Sharpe Ratio", "33.688%"},
            {"Loss Rate", "48%"},
            {"Win Rate", "52%"},
            {"Profit-Loss Ratio", "0.95"},
            {"Alpha", "0.825"},
            {"Beta", "-0.34"},
            {"Annual Standard Deviation", "0.945"},
            {"Annual Variance", "0.893"},
            {"Information Ratio", "0.713"},
            {"Tracking Error", "0.957"},
            {"Treynor Ratio", "-2.323"},
            {"Total Fees", "$24760.85"},
            {"Fitness Score", "0.54"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "0.238"},
            {"Return Over Maximum Drawdown", "0.27"},
            {"Portfolio Turnover", "7.204"},
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
            {"OrderListHash", "843493486"}
        };
    }
}
