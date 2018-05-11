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
using QuantConnect.Brokerages;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for guaranteed Volume Weighted Average Price orders.
    /// This algorithm shows how to submit VWAP orders.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class GuaranteedVwapRegressionAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);
            AddEquity("SPY", Resolution.Minute);

            // The guaranteed VWAP order will be submitted in pre-market,
            // so we warmup one day to ensure we have a price for the security
            // when the first order is submitted.
            SetWarmUp(TimeSpan.FromDays(1));

            // IB will only accept Guaranteed VWAP orders at or before 9.29 AM,
            // so LEAN requires them to be submitted at least one minute earlier.

            // Guaranteed VWAP orders must be submitted at or before 9:28 AM
            Schedule.On(DateRules.EveryDay(), TimeRules.At(9, 15), EveryDayBeforeMarketOpen);
        }

        private void EveryDayBeforeMarketOpen()
        {
            Debug($"Submitting VWAP order at: {Time}");

            // Submit the VWAP order
            VwapOrder("SPY", 100);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"OnOrderEvent(): {Time}: {orderEvent}");
        }
    }
}