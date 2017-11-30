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
using QuantConnect.Data.Market;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrates the runtime addition and removal of securities from your algorithm.
    /// With LEAN it is possible to add and remove securities after the initialization.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="assets" />
    /// <meta name="tag" content="regression test" />
    public class AddRemoveSecurityRegressionAlgorithm : QCAlgorithm
    {
        private DateTime lastAction;

        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private Symbol _aig = QuantConnect.Symbol.Create("AIG", SecurityType.Equity, Market.USA);
        private Symbol _bac = QuantConnect.Symbol.Create("BAC", SecurityType.Equity, Market.USA);

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            AddSecurity(SecurityType.Equity, "SPY");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public void OnData(TradeBars data)
        {
            if (lastAction.Date == Time.Date) return;

            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 0.5);
                lastAction = Time;
            }
            if (Time.DayOfWeek == DayOfWeek.Tuesday)
            {
                AddSecurity(SecurityType.Equity, "AIG");
                AddSecurity(SecurityType.Equity, "BAC");
                lastAction = Time;
            }
            else if (Time.DayOfWeek == DayOfWeek.Wednesday)
            {
                SetHoldings(_aig, .25);
                SetHoldings(_bac, .25);
                lastAction = Time;
            }
            else if (Time.DayOfWeek == DayOfWeek.Thursday)
            {
                RemoveSecurity(_bac);
                RemoveSecurity(_aig);
                lastAction = Time;
            }
        }

        /// <summary>
        /// Order events are triggered on order status changes. There are many order events including non-fill messages.
        /// </summary>
        /// <param name="orderEvent">OrderEvent object with details about the order status</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Submitted)
            {
                Debug(Time + ": Submitted: " + Transactions.GetOrderById(orderEvent.OrderId));
            }
            if (orderEvent.Status.IsFill())
            {
                Debug(Time + ": Filled: " + Transactions.GetOrderById(orderEvent.OrderId));
            }
        }
    }
}