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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that a currency added at runtime (here BTCEUR, from a scheduled event) has its
    /// conversion rate seeded right away, so using it immediately no longer throws because the rate is still 0.
    /// </summary>
    public class RuntimeCurrencyConversionSeedingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _ltcusd;
        private bool _addedAtRuntime;
        private bool _assertedSeeded;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 5);
            SetEndDate(2018, 4, 5);
            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);
            SetCash(100000);

            // Account currency asset that funds the loop
            _ltcusd = AddCrypto("LTCUSD", Resolution.Minute).Symbol;

            // Add a non-account-currency asset at runtime, mirroring users that add assets from a scheduled event
            Schedule.On(DateRules.EveryDay(), TimeRules.At(10, 0), () =>
            {
                if (_addedAtRuntime)
                {
                    return;
                }
                _addedAtRuntime = true;
                AddCrypto("BTCEUR", Resolution.Minute);
            });
        }

        /// <summary>
        /// Runs right after the runtime-added security is wired up, the earliest point it can be used
        /// </summary>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (!changes.AddedSecurities.Any(security => security.Symbol.Value == "BTCEUR"))
            {
                return;
            }
            _assertedSeeded = true;

            // With the fix these are already seeded here. Without it they would still be 0 and the conversion below would throw.
            var eur = Portfolio.CashBook["EUR"];
            var btc = Portfolio.CashBook["BTC"];
            if (eur.ConversionRate == 0 || btc.ConversionRate == 0)
            {
                throw new RegressionTestException(
                    $"Runtime-added currency conversion rates were not seeded (EUR={eur.ConversionRate}, BTC={btc.ConversionRate})");
            }

            if (Portfolio.CashBook.ConvertToAccountCurrency(100m, "EUR") <= 0)
            {
                throw new RegressionTestException("Expected a positive EUR -> account currency conversion");
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!_addedAtRuntime || Portfolio.Invested)
            {
                return;
            }

            if (Securities[_ltcusd].Price != 0)
            {
                SetHoldings(_ltcusd, 0.5);
            }
        }

        /// <summary>
        /// Makes sure the seeding path was actually exercised so the test can't silently pass
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (!_assertedSeeded)
            {
                throw new RegressionTestException("BTCEUR was never added at runtime, the seeding path was not exercised");
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
        public long DataPoints => 6005;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 591;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

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
            {"Start Equity", "100000.00"},
            {"End Equity", "99064.52"},
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
            {"Total Fees", "$149.18"},
            {"Estimated Strategy Capacity", "$160000.00"},
            {"Lowest Capacity Asset", "LTCUSD 2XR"},
            {"Portfolio Turnover", "50.20%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "69d27a394cffbd938ec23fbb451f37ae"}
        };
    }
}
