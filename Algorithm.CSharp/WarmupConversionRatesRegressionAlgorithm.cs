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
using QuantConnect.Brokerages;
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
            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 17277;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 180;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "10000.00"},
            {"End Equity", "9884.48"},
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
            {"Total Fees", "$29.84"},
            {"Estimated Strategy Capacity", "$410000.00"},
            {"Lowest Capacity Asset", "LTCUSD 2XR"},
            {"Portfolio Turnover", "100.61%"},
            {"OrderListHash", "716b5757844f607d1402a5571f015aea"}
        };
    }
}
