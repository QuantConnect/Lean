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
    /// This regression algorithm is a test case for validation of conversion rates during warm up.
    /// </summary>
    public class WarmupConversionRatesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 5);
            SetEndDate(2018, 4, 5);
            SetCash(10000);

            SetWarmUp(TimeSpan.FromDays(1));
            AddCrypto("BTCEUR");
            AddCrypto("LTCUSD");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (Portfolio.CashBook["EUR"].ConversionRate == 0
                || Portfolio.CashBook["BTC"].ConversionRate == 0
                || Portfolio.CashBook["LTC"].ConversionRate == 0)
            {
                Log($"BTCEUR current price: {Securities["BTCEUR"].Price}");
                Log($"LTCUSD current price: {Securities["LTCUSD"].Price}");
                Log($"EUR conversion rate: {Portfolio.CashBook["EUR"].ConversionRate}");
                Log($"BTC conversion rate: {Portfolio.CashBook["BTC"].ConversionRate}");
                Log($"LTC conversion rate: {Portfolio.CashBook["LTC"].ConversionRate}");

                throw new Exception("Conversion rate is 0");
            }

            if (IsWarmingUp) return;
            if (!Portfolio.Invested)
            {
                SetHoldings("LTCUSD", 1);
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
            {"Compounding Annual Return", "-95.719%"},
            {"Drawdown", "4.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.859%"},
            {"Sharpe Ratio", "-10.507"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.074"},
            {"Beta", "-1.33"},
            {"Annual Standard Deviation", "0.103"},
            {"Annual Variance", "0.011"},
            {"Information Ratio", "-10.815"},
            {"Tracking Error", "0.18"},
            {"Treynor Ratio", "0.814"},
            {"Total Fees", "$0.00"}
        };
    }
}