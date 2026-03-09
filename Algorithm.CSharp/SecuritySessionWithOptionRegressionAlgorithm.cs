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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Common;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class SecuritySessionWithOptionRegressionAlgorithm : SecuritySessionRegressionAlgorithm
    {
        private MarketHourAwareConsolidator _optionContractConsolidator;
        private decimal _previousOpenInterest;
        public override void InitializeSecurity()
        {
            SetStartDate(2014, 06, 06);
            SetEndDate(2014, 06, 09);

            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
            UniverseSettings.MinimumTimeInUniverse = TimeSpan.Zero;

            var aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

            var _contract = OptionChain(aapl)
                .OrderBy(symbol => symbol.ID.OptionRight)
                .ThenBy(symbol => symbol.ID.StrikePrice)
                .ThenBy(symbol => symbol.ID.Date)
                .ThenBy(symbol => symbol.ID)
                .FirstOrDefault(optionContract => optionContract.ID.OptionRight == OptionRight.Call);
            Security = AddOptionContract(_contract);

            // Manually add consolidators to simulate Session behavior
            _optionContractConsolidator = new MarketHourAwareConsolidator(false, Resolution.Daily, typeof(QuoteBar), TickType.Quote, false);
            SubscriptionManager.AddConsolidator(_contract.Symbol, _optionContractConsolidator, TickType.Quote);
        }

        protected override void AccumulateSessionData(Slice slice)
        {
            if (CurrentDate.Date != slice.Time.Date)
            {
                // Check the previous session bar
                var consolidated = (QuoteBar)_optionContractConsolidator.Consolidated;
                var futureSession = Security.Session;
                if (futureSession[1].Open != consolidated.Open
                    || futureSession[1].High != consolidated.High
                    || futureSession[1].Low != consolidated.Low
                    || futureSession[1].Close != consolidated.Close
                    || futureSession[1].OpenInterest != _previousOpenInterest)
                {
                    throw new RegressionTestException("Mismatch in previous session bar (OHLCV)");
                }
                CurrentDate = slice.Time;
            }
        }

        protected override void ValidateSessionBars()
        {
            var futureSession = Security.Session;

            _previousOpenInterest = Security.OpenInterest;
            var workingData = (QuoteBar)_optionContractConsolidator.WorkingData;
            if (futureSession.Open != workingData.Open
                || futureSession.High != workingData.High
                || futureSession.Low != workingData.Low
                || futureSession.Close != workingData.Close
                || futureSession.OpenInterest != Security.OpenInterest)
            {
                throw new RegressionTestException("Mismatch in current session bar (OHLCV)");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 3147;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
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
            {"Information Ratio", "-9.486"},
            {"Tracking Error", "0.008"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
