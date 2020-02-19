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

/*
using System;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;


namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash. This is a skeleton
    /// framework you can use for designing an algorithm.
    /// </summary>
    /// <meta name="tag" content="live trading" />
    /// <meta name="tag" content="alerts" />
    /// <meta name="tag" content="sms alerts" />
    /// <meta name="tag" content="web hooks" />
    /// <meta name="tag" content="email alerts" />
    /// <meta name="tag" content="runtime statistics" />
    ///

    public class RabbitMQLive : QCAlgorithm
    {
        /// <summary>
        /// Initialise the Algorithm and Prepare Required Data.
        /// </summary>
        /// 
        Symbol _ibm = "IBM";
        Symbol _eurusd;
        OrderTicket _limitTicket;
        OrderTicket _stopMarketTicket;
        OrderTicket _stopLimitTicket;

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
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.
            if (LiveMode) //Live Mode Property
            {
                Debug("THIS IS LIVE");
            }
                AddEquity("SPY", Resolution.Minute);

            // There are other assets with similar methods. See "Selecting Options" etc for more details.
            // AddFuture, AddForex, AddCfd, AddOption
            //Equity Data for US Markets:
            AddSecurity(SecurityType.Equity, "IBM", Resolution.Second);

            //FOREX Data for Weekends: 24/6
            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Minute);

            //Custom/Bitcoin Live Data: 24/7
            AddData<Bitcoin>("BTC", Resolution.Second, TimeZones.Utc);
        }


        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 1);
                Debug("Purchased Stock");
            }

            if (_limitTicket == null)
            {
                MarketOrder(_ibm, 100, true, tag: "market order");
                _limitTicket = LimitOrder(_ibm, 100, Securities["IBM"].Close * 0.9m, tag: "limit order");
                _stopMarketTicket = StopMarketOrder(_ibm, -100, Securities["IBM"].Close * 0.95m, tag: "stop market");
                _stopLimitTicket = StopLimitOrder(_ibm, 100, Securities["IBM"].Close * 0.90m, Securities["IBM"].Close * 0.80m, tag: "stop limit");
            }
        }
        
}
*/

using System;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating how to setup a custom brokerage message handler. Using the custom messaging
    /// handler you can ensure your algorithm continues operation through connection failures.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="brokerage models" />
    public class RabbitMQLive : QCAlgorithm
    {
        private bool _submittedMarketOnCloseToday;
        private Security _security;
        private DateTime last = DateTime.MinValue;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(25000);

            AddSecurity(SecurityType.Equity, "SPY", Resolution.Second, fillDataForward: true, extendedMarketHours: true);

            _security = Securities["SPY"];

            //Set the brokerage message handler:
            SetBrokerageMessageHandler(new BrokerageMessageHandler(this));
        }

        public void OnData(TradeBars data)
        {

            if (Time.Date != last.Date) // each morning submit a market on open order
            {
                _submittedMarketOnCloseToday = false;
                MarketOnOpenOrder("SPY", 100);
                last = Time;

                if (Portfolio.HoldStock) return;
                Order("SPY", 100);
                Debug("Purchased SPY on " + Time.ToShortDateString());

                Sell("SPY", 50);
                Debug("Sell SPY on " + Time.ToShortDateString());
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

    /// <summary>
    /// Handle the error messages in a custom manner
    /// </summary>
    public class BrokerageMessageHandler : IBrokerageMessageHandler
    {
        private readonly IAlgorithm _algo;
        public BrokerageMessageHandler(IAlgorithm algo) { _algo = algo; }

        /// <summary>
        /// Process the brokerage message event. Trigger any actions in the algorithm or notifications system required.
        /// </summary>
        /// <param name="message">Message object</param>
        public void Handle(BrokerageMessageEvent message)
        {
            var toLog = $"{_algo.Time.ToStringInvariant("o")} Event: {message.Message}";
            _algo.Debug(toLog);
            _algo.Log(toLog);
        }
    }
}