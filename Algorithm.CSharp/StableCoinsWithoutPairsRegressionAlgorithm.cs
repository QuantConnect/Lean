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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
namespace QuantConnect.Algorithm.CSharp
{
    public class StableCoinsWithoutPairsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2018, 5, 1);
            SetEndDate(2018, 5, 2);
            SetAccountCurrency("USD");
            SetCash("BNFCR", 20000);
            SetBrokerageModel(Brokerages.BrokerageName.Binance, AccountType.Cash);
            AddCrypto("BTCUSDT", Resolution.Hour, Market.Binance);
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("BTCUSDT", 1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // Get the amount of the custom currency BNFCR
            var amount = Portfolio.CashBook["BNFCR"].Amount;
            // Check that conversion doesn't change the value,
            // since BNFCR is a 1-to-1 stablecoin without pair
            var amountInAccountCurrency = Portfolio.CashBook.ConvertToAccountCurrency(amount, "BNFCR");
            if (amount != amountInAccountCurrency)
            {
                throw new RegressionTestException($"The amounts should be equal, but they are not: {amount} and {amountInAccountCurrency}");
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
        public long DataPoints => 50;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 4;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "49"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "120000.00"},
            {"End Equity", "120000"},
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
            {"OrderListHash", "92454ed90f4024c6654227bd0fd0228b"}
        };
    }
}
