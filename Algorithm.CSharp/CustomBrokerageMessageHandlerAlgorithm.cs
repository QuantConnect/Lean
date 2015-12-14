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
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /*
    *	QCU How do I handle brokerage messages in a custom way?
    *
    *	Often you may want more stability and fault tolerance so you may want to
    *	control what happens with brokerage messages. Using the custom messaging 
    *	handler you can ensure your algorithm continues operation through connection 
    *	failures.
    */
    public class CustomBrokerageErrorHandlerAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2013, 1, 1);
            SetEndDate(DateTime.Now.Date.AddDays(-1));
            SetCash(25000);
            AddSecurity(SecurityType.Equity, "SPY");

            //Set the brokerage message handler:
            SetBrokerageMessageHandler(new CustomBrokerageMessageHandler(this));
        }

        public void OnData(TradeBars data)
        {
            if (Portfolio.HoldStock) return;
            Order("SPY", 100);
            Debug("Purchased SPY on " + Time.ToShortDateString());
        }
    }

    /// <summary>
    /// Handle the error messages in a custom manner
    /// </summary>
    public class CustomBrokerageMessageHandler : IBrokerageMessageHandler
    {
        private readonly IAlgorithm _algo;
        public CustomBrokerageMessageHandler(IAlgorithm algo) { _algo = algo; }

        /// <summary>
        /// Process the brokerage message event. Trigger any actions in the algorithm or notifications system required.
        /// </summary>
        /// <param name="message">Message object</param>
        public void Handle(BrokerageMessageEvent message)
        {
            var toLog = _algo.Time.ToString("o") + " Event: " + message.Message;
            _algo.Debug(toLog);
            _algo.Log(toLog);
        }
    }
}