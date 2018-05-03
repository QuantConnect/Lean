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

using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration algorithm of time in force order settings.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class TimeInForceAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;
        private OrderTicket _gtcOrderTicket;
        private OrderTicket _dayOrderTicket;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            // The default time in force setting for all orders is GoodTilCancelled (GTC),
            // uncomment this line to set a different time in force.
            // We currently only support GTC and DAY.
            // DefaultOrderProperties.TimeInForce = TimeInForce.Day;

            _symbol = AddEquity("SPY", Resolution.Minute).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_gtcOrderTicket == null)
            {
                // This order has a default time in force of GoodTilCanceled,
                // it will never expire and will not be canceled automatically.

                DefaultOrderProperties.TimeInForce = TimeInForce.GoodTilCanceled;
                _gtcOrderTicket = LimitOrder(_symbol, 10, 160m);
            }

            if (_dayOrderTicket == null)
            {
                // This order will expire at market close,
                // if not filled by then it will be canceled automatically.

                DefaultOrderProperties.TimeInForce = TimeInForce.Day;
                _dayOrderTicket = LimitOrder(_symbol, 10, 160m);
            }
        }

        /// <summary>
        /// Order event handler. This handler will be called for all order events, including submissions, fills, cancellations.
        /// </summary>
        /// <param name="orderEvent">Order event instance containing details of the event</param>
        /// <remarks>This method can be called asynchronously, ensure you use proper locks on thread-unsafe objects</remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{Time} {orderEvent}");
        }

    }
}
