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

using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm reproduces GH issue 1859: 'Non-USD cash added during
    /// Initialize not counted as starting capital in backtesting'
    /// </summary>
    public class StartingCapitalRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 4); //Set Start Date
            SetEndDate(2018, 4, 4); //Set End Date

            SetCash(10000);
            SetCash("EUR", 10000);
            SetCash("BTC", 10000);
            SetCash("ETH", 10000);

            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            AddCrypto("BTCUSD");
            _symbol = AddCrypto("ETHUSD").Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                Buy(_symbol, 1);
                Debug("Purchased Stock");
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
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-100.000%"},
            {"Drawdown", "10.700%"},
            {"Expectancy", "0"},
            {"Net Profit", "-7.119%"},
            {"Sharpe Ratio", "-12.379"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-17.159"},
            {"Beta", "5.519"},
            {"Annual Standard Deviation", "0.727"},
            {"Annual Variance", "0.528"},
            {"Information Ratio", "-17.603"},
            {"Tracking Error", "0.595"},
            {"Treynor Ratio", "-1.631"},
            {"Total Fees", "$1.21"}
        };
    }
}
