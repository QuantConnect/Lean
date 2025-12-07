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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Common;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    public class SecuritySessionWithFutureContractRegressionAlgorithm : SecuritySessionRegressionAlgorithm
    {
        private Future _futureContract;
        private MarketHourAwareConsolidator _continuousContractConsolidator;
        private MarketHourAwareConsolidator _futureContractConsolidator;
        private decimal _previousContinuousContractOpenInterest;
        private decimal _previousFutureContractOpenInterest;
        public override void InitializeSecurity()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 08);

            Security = AddFuture(Futures.Metals.Gold, Resolution.Minute, extendedMarketHours: ExtendedMarketHours);
            _futureContract = AddFutureContract(FuturesChain(Security.Symbol).OrderBy(x => x.Symbol.ID.Date).First());

            // Manually add consolidators to simulate Session behavior
            _continuousContractConsolidator = new MarketHourAwareConsolidator(false, Resolution.Daily, typeof(QuoteBar), TickType.Quote, false);
            _futureContractConsolidator = new MarketHourAwareConsolidator(false, Resolution.Daily, typeof(QuoteBar), TickType.Quote, false);
            SubscriptionManager.AddConsolidator(Security.Symbol, _continuousContractConsolidator, TickType.Quote);
            SubscriptionManager.AddConsolidator(_futureContract.Symbol, _futureContractConsolidator, TickType.Quote);
        }

        protected override void AccumulateSessionData(Slice slice)
        {
            if (CurrentDate.Date != slice.Time.Date)
            {
                // Check the previous session bar for the continuous contract and future contract

                var consolidated = (QuoteBar)_continuousContractConsolidator.Consolidated;
                var futureSession = Security.Session;
                if (futureSession[1].Open != consolidated.Open
                    || futureSession[1].High != consolidated.High
                    || futureSession[1].Low != consolidated.Low
                    || futureSession[1].Close != consolidated.Close
                    || futureSession[1].OpenInterest != _previousContinuousContractOpenInterest)
                {
                    throw new RegressionTestException("Mismatch in previous session bar (OHLCV)");
                }

                consolidated = (QuoteBar)_futureContractConsolidator.Consolidated;
                var futureContractSession = _futureContract.Session;
                if (futureContractSession[1].Open != consolidated.Open
                    || futureContractSession[1].High != consolidated.High
                    || futureContractSession[1].Low != consolidated.Low
                    || futureContractSession[1].Close != consolidated.Close
                    || futureContractSession[1].OpenInterest != _previousFutureContractOpenInterest)
                {
                    throw new RegressionTestException("Mismatch in previous session bar (OHLCV)");
                }
                CurrentDate = slice.Time;
            }
        }

        protected override void ValidateSessionBars()
        {
            // Check the current session bar for the continuous contract and future contract

            var futureSession = Security.Session;
            _previousContinuousContractOpenInterest = Security.OpenInterest;
            var workingData = (QuoteBar)_continuousContractConsolidator.WorkingData;
            if (futureSession.Open != workingData.Open
                || futureSession.High != workingData.High
                || futureSession.Low != workingData.Low
                || futureSession.Close != workingData.Close
                || futureSession.OpenInterest != Security.OpenInterest)
            {
                throw new RegressionTestException("Mismatch in current session bar (OHLCV)");
            }

            var futureContractSession = _futureContract.Session;
            _previousFutureContractOpenInterest = _futureContract.OpenInterest;
            workingData = (QuoteBar)_futureContractConsolidator.WorkingData;
            if (futureContractSession.Open != workingData.Open
                || futureContractSession.High != workingData.High
                || futureContractSession.Low != workingData.Low
                || futureContractSession.Close != workingData.Close
                || futureSession.OpenInterest != _futureContract.OpenInterest)
            {
                throw new RegressionTestException("Mismatch in current session bar (OHLCV)");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 7296;

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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
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
