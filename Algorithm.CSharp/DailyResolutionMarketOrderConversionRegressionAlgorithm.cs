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
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that a market order placed through an intraday scheduled event on an asset
    /// subscribed only at daily resolution is automatically converted, so it fills at a real daily open/close instead
    /// of the stale previous close (no fresh intraday price is available for a daily resolution subscription):
    ///     - While the market is open (before the MarketOnClose submission buffer): converted to MarketOnClose,
    ///       filling at today's close.
    ///     - Within the MarketOnClose submission buffer near the close: converted to MarketOnOpen, filling at the
    ///       next open.
    /// At the same time, a market order on a minute resolution asset placed at the same intraday time is NOT converted:
    /// it stays a regular market order and fills immediately, since fresh intraday data is available.
    /// </summary>
    public class DailyResolutionMarketOrderConversionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _daily;
        private Symbol _minute;

        private OrderTicket _marketOnCloseTicket;
        private OrderTicket _marketOnOpenTicket;
        private OrderTicket _minuteTicket;

        private static readonly DateTime _tradingDay = new(2013, 10, 8);

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 09);
            SetCash(100000);

            // Daily resolution: market orders have no fresh intraday price, so they get converted.
            _daily = AddEquity("SPY", Resolution.Daily).Symbol;
            // Minute resolution: fresh intraday data is available, so market orders are left untouched.
            _minute = AddEquity("IBM", Resolution.Minute).Symbol;

            // Right after the open (09:31): even one minute into the session only the previous daily close is
            // available for the daily asset, so its order must still be converted to MarketOnClose. The minute order,
            // which has fresh intraday data, must NOT be converted.
            Schedule.On(DateRules.On(_tradingDay.Year, _tradingDay.Month, _tradingDay.Day), TimeRules.At(9, 31), () =>
            {
                if (!Securities[_daily].HasData || !Securities[_minute].HasData)
                {
                    throw new RegressionTestException($"Expected both securities to have data on {Time}");
                }

                // Minute asset: not converted, fills immediately on fresh intraday data
                _minuteTicket = MarketOrder(_minute, 10);
                if (_minuteTicket.OrderType != OrderType.Market)
                {
                    throw new RegressionTestException(
                        $"Expected the minute resolution order to remain a Market order but was {_minuteTicket.OrderType}. Time: {Time}");
                }
                if (_minuteTicket.Status != OrderStatus.Filled)
                {
                    throw new RegressionTestException($"Expected the minute resolution order to fill immediately on {Time}");
                }

                // Daily asset: converted to MarketOnClose
                _marketOnCloseTicket = MarketOrder(_daily, 10);
                if (_marketOnCloseTicket.OrderType != OrderType.MarketOnClose)
                {
                    throw new RegressionTestException(
                        $"Expected the daily market order to be converted to MarketOnClose but was {_marketOnCloseTicket.OrderType}. Time: {Time}");
                }
                if (_marketOnCloseTicket.Status.IsFill())
                {
                    throw new RegressionTestException($"The MarketOnClose order was not expected to fill at submission time {Time}");
                }
            });

            // Within the MarketOnClose submission buffer near the close: the daily order falls back to MarketOnOpen.
            Schedule.On(DateRules.On(_tradingDay.Year, _tradingDay.Month, _tradingDay.Day), TimeRules.At(15, 55), () =>
            {
                _marketOnOpenTicket = MarketOrder(_daily, 10);
                if (_marketOnOpenTicket.OrderType != OrderType.MarketOnOpen)
                {
                    throw new RegressionTestException(
                        $"Expected the daily market order near the close to be converted to MarketOnOpen but was {_marketOnOpenTicket.OrderType}. Time: {Time}");
                }
                if (_marketOnOpenTicket.Status.IsFill())
                {
                    throw new RegressionTestException($"The MarketOnOpen order was not expected to fill at submission time {Time}");
                }
            });
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            var fillLocalTime = orderEvent.UtcTime.ConvertFromUtc(Securities[orderEvent.Symbol].Exchange.TimeZone);

            if (_marketOnCloseTicket != null && orderEvent.OrderId == _marketOnCloseTicket.OrderId)
            {
                // MarketOnClose must fill at the close of the submission day, not on the stale previous close
                var expectedClose = Securities[_daily].Exchange.Hours.GetNextMarketClose(_tradingDay, false);
                if (fillLocalTime != expectedClose)
                {
                    throw new RegressionTestException(
                        $"Expected the MarketOnClose order to fill at {expectedClose} but filled at {fillLocalTime}");
                }
            }
            else if (_marketOnOpenTicket != null && orderEvent.OrderId == _marketOnOpenTicket.OrderId)
            {
                // MarketOnOpen must fill on a later session (the next open), not on the submission day
                if (fillLocalTime.Date <= _tradingDay.Date)
                {
                    throw new RegressionTestException(
                        $"Expected the MarketOnOpen order to fill on a later session than {_tradingDay:yyyy-MM-dd} but filled at {fillLocalTime}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_minuteTicket == null || _minuteTicket.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException("The minute resolution market order was expected to be filled");
            }

            if (_marketOnCloseTicket == null || _marketOnCloseTicket.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException("The converted MarketOnClose order was expected to be filled");
            }

            if (_marketOnOpenTicket == null || _marketOnOpenTicket.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException("The converted MarketOnOpen order was expected to be filled");
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
        public long DataPoints => 3150;

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
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-1.206%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99991.14"},
            {"Net Profit", "-0.009%"},
            {"Sharpe Ratio", "-3.861"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.008"},
            {"Beta", "0.035"},
            {"Annual Standard Deviation", "0.005"},
            {"Annual Variance", "0"},
            {"Information Ratio", "5.642"},
            {"Tracking Error", "0.131"},
            {"Treynor Ratio", "-0.526"},
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$42000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "1.41%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "ebf45813288201552f706cd072fe9ad8"}
        };
    }
}
