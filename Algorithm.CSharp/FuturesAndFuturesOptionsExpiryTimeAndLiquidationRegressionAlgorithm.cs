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
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Option;
using Futures = QuantConnect.Securities.Futures;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests delistings for Futures and Futures Options to ensure that they are delisted at the expected times.
    /// </summary>
    public class FuturesAndFuturesOptionsExpiryTimeAndLiquidationRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _invested;
        private int _liquidated;
        private int _delistingsReceived;

        private Symbol _esFuture;
        private Symbol _esFutureOption;

        private readonly DateTime _expectedExpiryWarningTime = new DateTime(2020, 6, 19);
        private readonly DateTime _expectedExpiryDelistingTime = new DateTime(2020, 6, 20);
        private readonly DateTime _expectedLiquidationTime = new DateTime(2020, 6, 20);

        public override void Initialize()
        {
            SetStartDate(2020, 1, 5);
            SetEndDate(2020, 12, 1);
            SetCash(100000);

            var es = QuantConnect.Symbol.CreateFuture(
                Futures.Indices.SP500EMini,
                Market.CME,
                new DateTime(2020, 6, 19));

            var esOption = QuantConnect.Symbol.CreateOption(
                es,
                Market.CME,
                OptionStyle.American,
                OptionRight.Put,
                3400m,
                new DateTime(2020, 6, 19));

            _esFuture = AddFutureContract(es, Resolution.Minute).Symbol;
            _esFutureOption = AddFutureOptionContract(esOption, Resolution.Minute).Symbol;
        }

        public override void OnData(Slice slice)
        {
            foreach (var delisting in slice.Delistings.Values)
            {
                // Two warnings and two delisted events should be received for a grand total of 4 events.
                _delistingsReceived++;

                if (delisting.Type == DelistingType.Warning &&
                    delisting.Time != _expectedExpiryWarningTime)
                {
                    throw new RegressionTestException($"Expiry warning with time {delisting.Time} but is expected to be {_expectedExpiryWarningTime}");
                }
                if (delisting.Type == DelistingType.Warning && delisting.Time != Time.Date)
                {
                    throw new RegressionTestException($"Delisting warning received at an unexpected date: {Time} - expected {delisting.Time}");
                }
                if (delisting.Type == DelistingType.Delisted &&
                    delisting.Time != _expectedExpiryDelistingTime)
                {
                    throw new RegressionTestException($"Delisting occurred at unexpected time: {delisting.Time} - expected: {_expectedExpiryDelistingTime}");
                }
                if (delisting.Type == DelistingType.Delisted &&
                    delisting.Time != Time.Date)
                {
                    throw new RegressionTestException($"Delisting notice received at an unexpected date: {Time} - expected {delisting.Time}");
                }
            }

            if (!_invested &&
                (slice.Bars.ContainsKey(_esFuture) || slice.QuoteBars.ContainsKey(_esFuture)) &&
                (slice.Bars.ContainsKey(_esFutureOption) || slice.QuoteBars.ContainsKey(_esFutureOption)))
            {
                _invested = true;

                MarketOrder(_esFuture, 1);

                var optionContract = Securities[_esFutureOption];
                var marginModel = optionContract.BuyingPowerModel as FuturesOptionsMarginModel;
                if (marginModel.InitialIntradayMarginRequirement == 0
                    || marginModel.InitialOvernightMarginRequirement == 0
                    || marginModel.MaintenanceIntradayMarginRequirement == 0
                    || marginModel.MaintenanceOvernightMarginRequirement == 0)
                {
                    throw new RegressionTestException("Unexpected margin requirements");
                }

                if (marginModel.GetInitialMarginRequirement(optionContract, 1) == 0)
                {
                    throw new RegressionTestException("Unexpected Initial Margin requirement");
                }
                if (marginModel.GetMaintenanceMargin(optionContract) != 0)
                {
                    throw new RegressionTestException("Unexpected Maintenance Margin requirement");
                }

                MarketOrder(_esFutureOption, 1);

                if (marginModel.GetMaintenanceMargin(optionContract) == 0)
                {
                    throw new RegressionTestException("Unexpected Maintenance Margin requirement");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Direction != OrderDirection.Sell || orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            // * Future Liquidation
            // * Future Option Exercise

            // * We expect NO Underlying Future Liquidation because we already hold a Long future position so the FOP Put selling leaves us breakeven
            _liquidated++;
            if (orderEvent.Symbol.SecurityType == SecurityType.FutureOption && _expectedLiquidationTime != Time)
            {
                throw new RegressionTestException($"Expected to liquidate option {orderEvent.Symbol} at {_expectedLiquidationTime}, instead liquidated at {Time}");
            }
            if (orderEvent.Symbol.SecurityType == SecurityType.Future && _expectedLiquidationTime.AddMinutes(-1) != Time && _expectedLiquidationTime != Time)
            {
                throw new RegressionTestException($"Expected to liquidate future {orderEvent.Symbol} at {_expectedLiquidationTime} (+1 minute), instead liquidated at {Time}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_invested)
            {
                throw new RegressionTestException("Never invested in ES futures and FOPs");
            }
            if (_delistingsReceived != 4)
            {
                throw new RegressionTestException($"Expected 4 delisting events received, found: {_delistingsReceived}");
            }
            if (_liquidated != 2)
            {
                throw new RegressionTestException($"Expected 3 liquidation events, found {_liquidated}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 212942;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "10.36%"},
            {"Average Loss", "-10.99%"},
            {"Compounding Annual Return", "-1.942%"},
            {"Drawdown", "2.000%"},
            {"Expectancy", "-0.028"},
            {"Start Equity", "100000"},
            {"End Equity", "98233.93"},
            {"Net Profit", "-1.766%"},
            {"Sharpe Ratio", "-1.141"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0.020%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.94"},
            {"Alpha", "-0.02"},
            {"Beta", "0.001"},
            {"Annual Standard Deviation", "0.017"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.602"},
            {"Tracking Error", "0.291"},
            {"Treynor Ratio", "-16.65"},
            {"Total Fees", "$3.57"},
            {"Estimated Strategy Capacity", "$16000000.00"},
            {"Lowest Capacity Asset", "ES XFH59UK0MYO1"},
            {"Portfolio Turnover", "1.04%"},
            {"OrderListHash", "873800e40d1b38b08fc1764cd578bb4a"}
        };
    }
}
