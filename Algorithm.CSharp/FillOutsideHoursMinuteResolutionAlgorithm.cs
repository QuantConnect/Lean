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
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that orders submitted outside of market hours are:
    ///     - Filled outside of market hours for daily resolution
    ///     - Not filled outside of market hours for the rest of the resolutions
    ///
    /// This specific algorithm tests this for minute resolution and is intended to be used as a base class for the other resolutions.
    /// </summary>
    public class FillOutsideHoursMinuteResolutionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected virtual Resolution Resolution => Resolution.Minute;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 08);
            SetCash(100000);

            var spy = AddEquity("SPY", Resolution);

            Schedule.On(DateRules.Today, TimeRules.At(new TimeSpan(23, 0, 0)), () =>
            {
                if (!Portfolio.Invested && spy.HasData)
                {
                    var ticket = SubmitOrderRequest(new SubmitOrderRequest(OrderType.Market, spy.Type, spy.Symbol, 1, 0, 0, Time, ""));

                    if (Resolution == Resolution.Daily)
                    {
                        if (ticket.Status != OrderStatus.Filled)
                        {
                            throw new Exception($"Order was expected to be filled on {Time}. Resolution: {Resolution}");
                        }
                    }
                    else if (ticket.Status.IsFill())
                    {
                        throw new Exception($"Order was not expected to be filled on {Time}. Resolution: {Resolution}");
                    }
                }
            });
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 1582;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99997.25"},
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
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$12000000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.07%"},
            {"OrderListHash", "6a55ff7bccb41a538e1733ccbde482b3"}
        };
    }
}
