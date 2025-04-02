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
 *
*/

using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Future;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm using and testing HSI futures and index
    /// </summary>
    public class HSIFutureHourRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _symbolChangeEvent;
        private Symbol _contractSymbol;
        private Symbol _index;
        private Symbol _futureSymbol;

        /// <summary>
        /// The data resolution
        /// </summary>
        protected virtual Resolution Resolution => Resolution.Hour;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 20);
            SetEndDate(2013, 10, 30);

            SetAccountCurrency("HKD");
            SetTimeZone(TimeZones.HongKong);

            UniverseSettings.Resolution = Resolution;
            _index = AddIndex("HSI", Resolution).Symbol;
            var future = AddFuture(Futures.Indices.HangSeng, Resolution);
            future.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));
            _futureSymbol = future.Symbol;

            var seeder = new FuncSecuritySeeder(GetLastKnownPrices);
            SetSecurityInitializer(security => seeder.SeedSecurity(security));
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            foreach (var changedEvent in slice.SymbolChangedEvents.Values)
            {
                Debug($"{Time} - SymbolChanged event: {changedEvent}");
                if (Time.TimeOfDay != TimeSpan.Zero)
                {
                    throw new RegressionTestException($"{Time} unexpected symbol changed event {changedEvent}!");
                }
                _symbolChangeEvent++;
            }

            if (!Portfolio.Invested)
            {
                foreach (var chain in slice.FutureChains)
                {
                    // find the front contract expiring no earlier than in 90 days
                    var contract = (
                        from futuresContract in chain.Value.OrderBy(x => x.Expiry)
                        select futuresContract
                    ).FirstOrDefault();

                    // if found, trade it
                    if (contract != null)
                    {
                        _contractSymbol = contract.Symbol;
                        MarketOrder(_contractSymbol, 1);
                    }
                }
            }
            else
            {
                Liquidate();
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_symbolChangeEvent != 1)
            {
                throw new RegressionTestException($"Got no expected symbol changed event count {_symbolChangeEvent}!");
            }

            // Get the margin requirements
            var buyingPowerModel = Securities[_contractSymbol].BuyingPowerModel;
            var futureMarginModel = buyingPowerModel as FutureMarginModel;
            if (buyingPowerModel == null)
            {
                throw new RegressionTestException($"Invalid buying power model. Found: {buyingPowerModel.GetType().Name}. Expected: {nameof(FutureMarginModel)}");
            }
            var initialOvernight = futureMarginModel.InitialOvernightMarginRequirement;
            var maintenanceOvernight = futureMarginModel.MaintenanceOvernightMarginRequirement;
            var initialIntraday = futureMarginModel.InitialIntradayMarginRequirement;
            var maintenanceIntraday = futureMarginModel.MaintenanceIntradayMarginRequirement;

            var lastDataFuture = Securities[_futureSymbol].GetLastData();
            if (lastDataFuture == null || (lastDataFuture.EndTime - lastDataFuture.Time) != TimeSpan.FromHours(Resolution == Resolution.Hour ? 1 : 7.25)
                || lastDataFuture.EndTime.Date != lastDataFuture.Time.Date)
            {
                throw new RegressionTestException($"Unexpected data for symbol {_futureSymbol}!");
            }

            var lastDataIndex = Securities[_index].GetLastData();
            if (lastDataIndex == null || (lastDataIndex.EndTime - lastDataIndex.Time) != TimeSpan.FromHours(Resolution == Resolution.Hour ? 1 : 6.5)
                || lastDataFuture.EndTime.Date != lastDataFuture.Time.Date)
            {
                throw new RegressionTestException($"Unexpected data for symbol {_index}!");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var addedSecurity in changes.AddedSecurities)
            {
                if (addedSecurity.Symbol.SecurityType == SecurityType.Future
                    && !addedSecurity.Symbol.IsCanonical()
                    && !addedSecurity.HasData)
                {
                    throw new RegressionTestException($"Future contracts did not work up as expected: {addedSecurity.Symbol}");
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
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 652;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 25;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public virtual AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "57"},
            {"Average Win", "1.83%"},
            {"Average Loss", "-1.31%"},
            {"Compounding Annual Return", "-99.930%"},
            {"Drawdown", "23.000%"},
            {"Expectancy", "-0.572"},
            {"Start Equity", "100000"},
            {"End Equity", "80330"},
            {"Net Profit", "-19.670%"},
            {"Sharpe Ratio", "-1.298"},
            {"Sortino Ratio", "-1.254"},
            {"Probabilistic Sharpe Ratio", "1.073%"},
            {"Loss Rate", "82%"},
            {"Win Rate", "18%"},
            {"Profit-Loss Ratio", "1.40"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.772"},
            {"Annual Variance", "0.596"},
            {"Information Ratio", "-1.288"},
            {"Tracking Error", "0.772"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2280.00"},
            {"Estimated Strategy Capacity", "$120000000.00"},
            {"Lowest Capacity Asset", "HSI VL6DN7UV65S9"},
            {"Portfolio Turnover", "7099.25%"},
            {"OrderListHash", "f7382e07fdf6b8a39ee00ea5092fa831"}
        };
    }
}
