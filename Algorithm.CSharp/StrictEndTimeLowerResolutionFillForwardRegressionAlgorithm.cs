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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Equity;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting fill forwarded data behavior for consolidators and indicators.
    /// 1. Test that the on-consolidated event is not called for fill forwarded data in identity and higher period consolidators
    /// 2. Test that the intra-day fill-forwarded data is not fed to indicators
    /// </summary>
    public class StrictEndTimeLowerResolutionFillForwardRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Equity _aapl;

        private BaseData _lastNonFilledForwardedData;
        private int _dataCount;
        private int _indicatorUpdateCount;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 30);

            Settings.DailyPreciseEndTime = true;

            // Fill forward resolution will be minute
            AddEquity("SPY", Resolution.Minute);
            _aapl = AddEquity("AAPL", Resolution.Daily);

            var tradableDates = QuantConnect.Time.EachTradeableDayInTimeZone(_aapl.Exchange.Hours, StartDate, EndDate,
                _aapl.Exchange.TimeZone, _aapl.IsExtendedMarketHours).ToList();

            TestIdentityConsolidator(tradableDates);
            TestHigherPeriodConsolidator(tradableDates);
            TestIndicator(tradableDates);
        }

        private void TestIdentityConsolidator(List<DateTime> tradableDates)
        {
            var i = 0;
            var consolidator = Consolidate<TradeBar>(_aapl.Symbol, TimeSpan.FromDays(1), (bar) =>
            {
                var expectedDate = tradableDates[i++];
                var schedule = LeanData.GetDailyCalendar(expectedDate.AddDays(1), _aapl.Exchange, _aapl.IsExtendedMarketHours);

                if (bar.Time != schedule.Start || bar.EndTime != schedule.End)
                {
                    throw new RegressionTestException($"Unexpected consolidated bar time. " +
                        $"Expected: [{schedule.Start} - {schedule.End}], Actual: [{bar.Time} - {bar.EndTime}]");
                }

                Debug($"Consolidated (identity) :: {bar}");
            });

            if (consolidator is not IdentityDataConsolidator<TradeBar>)
            {
                throw new RegressionTestException($"Unexpected consolidator type. " +
                    $"Expected {typeof(IdentityDataConsolidator<TradeBar>)} but was {consolidator.GetType()}");
            }
        }

        private void TestHigherPeriodConsolidator(List<DateTime> tradableDates)
        {
            var i = 0;
            // Add a consolidator to assert that fill forward data is not used
            Consolidate<TradeBar>(_aapl.Symbol, TimeSpan.FromDays(2), (bar) =>
            {
                var expectedStartDate = tradableDates[i++];
                var startDateSchedule = LeanData.GetDailyCalendar(expectedStartDate.AddDays(1), _aapl.Exchange, _aapl.IsExtendedMarketHours);

                var expectedStartTime = startDateSchedule.Start;
                var expectedEndTime = expectedStartTime.AddDays(2);

                if (bar.Time != expectedStartTime || bar.EndTime != expectedEndTime)
                {
                    throw new RegressionTestException($"Unexpected consolidated bar time. " +
                        $"Expected: [{expectedStartTime} - {expectedEndTime}], Actual: [{bar.Time} - {bar.EndTime}]");
                }

                if (tradableDates[i] == expectedStartDate.AddDays(1))
                {
                    i++;
                }

                Debug($"Consolidated (2 days) :: {bar}");
            });
        }

        private void TestIndicator(List<DateTime> tradableDates)
        {
            var i = 0;
            EMA(_aapl.Symbol, 3, Resolution.Daily).Updated += (sender, data) =>
            {
                _indicatorUpdateCount++;

                var expectedEndTime = _aapl.Exchange.Hours.GetNextMarketClose(tradableDates[i++], false);
                if (data.EndTime != expectedEndTime)
                {
                    throw new RegressionTestException($"Unexpected EMA time. Expected: {expectedEndTime}, Actual: {data.EndTime}");
                }

                Debug($"EMA :: [{data.EndTime}] {data}");
            };
        }

        public override void OnData(Slice slice)
        {
            if (slice.TryGetValue(_aapl.Symbol, out var data))
            {
                var baseData = data as BaseData;
                if (!baseData.IsFillForward)
                {
                    _lastNonFilledForwardedData = baseData;
                }

                var timeInExchangeTz = UtcTime.ConvertFromUtc(_aapl.Exchange.TimeZone);
                var daySchedule = LeanData.GetDailyCalendar(timeInExchangeTz, _aapl.Exchange, _aapl.IsExtendedMarketHours);

                if (timeInExchangeTz == daySchedule.End)
                {
                    if (baseData.IsFillForward)
                    {
                        throw new RegressionTestException("End of day data should not be fill forward for daily subscription when data is available");
                    }
                }
                else
                {
                    if (!baseData.IsFillForward
                        ||  _lastNonFilledForwardedData == null
                        || _lastNonFilledForwardedData.Time.Date != baseData.Time.Date
                        || _lastNonFilledForwardedData.EndTime.Date != baseData.EndTime.Date)
                    {
                        throw new RegressionTestException("Data should be fill forward to minute resolution during the day");
                    }
                }

                _dataCount++;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var tradableDatesCount = QuantConnect.Time.TradeableDates(new[] { _aapl }, StartDate, EndDate);
            var tradableDayMinutesCount = _aapl.Exchange.Hours.RegularMarketDuration.TotalMinutes;
            var expectedDataCount = (tradableDatesCount - 1) * tradableDayMinutesCount + 1;

            if (_dataCount != expectedDataCount)
            {
                throw new RegressionTestException($"Unexpected data count. Expected: {expectedDataCount}, Actual: {_dataCount}");
            }

            if (_indicatorUpdateCount != tradableDatesCount)
            {
                throw new RegressionTestException($"Unexpected indicator update count. Expected: {tradableDatesCount}, Actual: {_indicatorUpdateCount}");
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
        public long DataPoints => 20805;

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
            {"Information Ratio", "-7.12"},
            {"Tracking Error", "0.109"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
