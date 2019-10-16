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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Custom.TradingEconomics;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Trades on interest rate announcements from data provided by Trading Economics
    /// </summary>
    public class TradingEconomicsAlgorithm : QCAlgorithm
    {
        private Symbol _interestRate;

        public override void Initialize()
        {
            SetStartDate(2013, 11, 1);
            SetEndDate(2019, 10, 3);
            SetCash(100000);

            AddEquity("AGG", Resolution.Hour);
            AddEquity("SPY", Resolution.Hour);

            _interestRate = AddData<TradingEconomicsCalendar>(TradingEconomics.Calendar.UnitedStates.InterestRate).Symbol;

            // Request 365 days of interest rate history with the TradingEconomicsCalendar custom data Symbol.
            // We should expect no historical data because 2013-11-01 is before the absolute first point of data
            var history = History<TradingEconomicsCalendar>(_interestRate, 365, Resolution.Daily);

            // Count the number of items we get from our history request (should be zero)
            Debug($"We got {history.Count()} items from our history request");
        }

        public override void OnData(Slice data)
        {
            // Make sure we have an interest rate calendar event
            if (!data.ContainsKey(_interestRate))
            {
                return;
            }

            var announcement = data.Get<TradingEconomicsCalendar>(_interestRate);

            // Confirm it's a FED Rate Decision
            if (announcement.Event != "Fed Interest Rate Decision")
            {
                return;
            }

            // In the event of a rate increase, rebalance 50% to Bonds.
            var interestRateDecreased = announcement.Actual <= announcement.Previous;

            if (interestRateDecreased)
            {
                SetHoldings("SPY", 1);
                SetHoldings("AGG", 0);
            }
            else
            {
                SetHoldings("SPY", 0.5);
                SetHoldings("AGG", 0.5);
            }

        }
    }
}