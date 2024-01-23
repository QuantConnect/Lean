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
using System.Globalization;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating the usage of custom brokerage message handler and the new brokerage-side order handling/filtering.
    /// This test is supposed to be ran by the CustomBrokerageMessageHandlerTests unit test fixture.
    ///
    /// All orders are sent from the brokerage, none of them will be placed by the algorithm.
    /// </summary>
    public class CustomBrokerageSideOrderHandlingRegressionAlgorithm : QCAlgorithm
    {
        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            SetBrokerageMessageHandler(new CustomBrokerageMessageHandler(this));
        }

        public override void OnEndOfAlgorithm()
        {
            // The security should have been added
            if (!Securities.ContainsKey(_spy))
            {
                throw new Exception("Expected security to have been added");
            }

            if (Transactions.OrdersCount == 0)
            {
                throw new Exception("Expected orders to be added from brokerage side");
            }

            if (Portfolio.Positions.Groups.Count != 1)
            {
                throw new Exception("Expected only one position");
            }
        }

        public class CustomBrokerageMessageHandler : IBrokerageMessageHandler
        {
            private readonly IAlgorithm _algorithm;
            public CustomBrokerageMessageHandler(IAlgorithm algo) { _algorithm = algo; }

            /// <summary>
            /// Process the brokerage message event. Trigger any actions in the algorithm or notifications system required.
            /// </summary>
            /// <param name="message">Message object</param>
            public void HandleMessage(BrokerageMessageEvent message)
            {
                _algorithm.Debug($"{_algorithm.Time.ToStringInvariant("o")} Event: {message.Message}");
            }

            /// <summary>
            /// Handles a new order placed manually in the brokerage side
            /// </summary>
            /// <param name="eventArgs">The new order event</param>
            /// <returns>Whether the order should be added to the transaction handler</returns>
            public bool HandleOrder(NewBrokerageOrderNotificationEventArgs eventArgs)
            {
                var order = eventArgs.Order;
                if (string.IsNullOrEmpty(order.Tag) || !int.TryParse(order.Tag, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    throw new Exception("Expected all new brokerage-side orders to have a valid tag");
                }

                // We will only process orders with even tags
                return value % 2 == 0;
            }
        }
    }
}
