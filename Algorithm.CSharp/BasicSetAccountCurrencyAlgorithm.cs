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
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic algorithm using SetAccountCurrency
    /// </summary>
    public class BasicSetAccountCurrencyAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _btcEur;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 04, 04);  //Set Start Date
            SetEndDate(2018, 04, 04);    //Set End Date
            //Before setting any cash or adding a Security call SetAccountCurrency
            SetAccountCurrency("EUR");
            SetCash(100000);             //Set Strategy Cash

            _btcEur = AddCrypto("BTCEUR").Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_btcEur, 1);
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-100.000%"},
            {"Drawdown", "11.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "-7.328%"},
            {"Sharpe Ratio", "-12.15"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-17.81"},
            {"Beta", "5.703"},
            {"Annual Standard Deviation", "0.762"},
            {"Annual Variance", "0.581"},
            {"Information Ratio", "-17.121"},
            {"Tracking Error", "0.628"},
            {"Treynor Ratio", "-1.623"},
            {"Total Fees", "$0.00"}
        };
    }
}
