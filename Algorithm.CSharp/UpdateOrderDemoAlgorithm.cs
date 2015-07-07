
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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class UpdateOrderDemoAlgorithm : QCAlgorithm
    {
        private IDataConsolidator _consolidator;
        private readonly List<OrderTicket> _tickets = new List<OrderTicket>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Second);

            _consolidator = ResolveConsolidator("SPY", Resolution.Daily);
            SubscriptionManager.AddConsolidator("SPY", _consolidator);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_consolidator.Consolidated == null) return;

            // submit limit order for yesterdays low each morning
            if (Time.TimeOfDay == new TimeSpan(9, 30, 1))
            {
                var newOrderTicket = LimitOrder("SPY", 100, .995m*((TradeBar)_consolidator.Consolidated).Low);
                _tickets.Add(newOrderTicket);
            }

            if (_tickets.Count == 0) return;

            var lastTicket = _tickets.Last();

            // start brining in the limit at noon, add a penny each time
            var afterNoonEvery15Minutes = Time.TimeOfDay >= new TimeSpan(12, 0, 0) && Time.TimeOfDay.Minutes%15 == 0 && Time.TimeOfDay.Seconds == 0;
            if (afterNoonEvery15Minutes && lastTicket.Status.IsOpen())
            {
                // move the limit price a penny higher
                lastTicket.Update(new UpdateOrderFields {LimitPrice = lastTicket.Get(OrderField.LimitPrice) + 0.01m});
            }

            if (Time.TimeOfDay >= new TimeSpan(15, 30, 0) && lastTicket.Status.IsOpen())
            {
                lastTicket.Cancel();
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Console.WriteLine(Time + " - " + orderEvent);
        }
    }
}