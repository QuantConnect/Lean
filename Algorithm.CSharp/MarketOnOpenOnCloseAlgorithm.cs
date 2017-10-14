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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of the Market On Close order for US Equities.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="placing orders" />
    public class MarketOnOpenOnCloseAlgorithm : QCAlgorithm
    {
        private bool _submittedMarketOnCloseToday;
        private Security _security;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Second, fillDataForward: true, extendedMarketHours: true);

            _security = Securities["SPY"];
        }

        private DateTime last = DateTime.MinValue;

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (Time.Date != last.Date) // each morning submit a market on open order
            {
                _submittedMarketOnCloseToday = false;
                MarketOnOpenOrder("SPY", 100);
                last = Time;
            }
            if (!_submittedMarketOnCloseToday && _security.Exchange.ExchangeOpen) // once the exchange opens submit a market on close order
            {
                _submittedMarketOnCloseToday = true;
                MarketOnCloseOrder("SPY", -100);
            }
        }

        public override void OnOrderEvent(OrderEvent fill)
        {
            var order = Transactions.GetOrderById(fill.OrderId);
            Console.WriteLine(Time + " - " + order.Type + " - " + fill.Status + ":: " + fill);
        }
    }
}