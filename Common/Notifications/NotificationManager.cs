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
/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace QuantConnect.Notifications
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Local/desktop implementation of messaging system for Lean Engine.
    /// </summary>
    public class NotificationManager
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        private bool _liveMode;
        private int _count;
        private readonly int _rateLimit = 30;
        private DateTime _resetTime;

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Public access to the messages
        /// </summary>
        public ConcurrentQueue<Notification> Messages
        {
            get; set;
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Initialize the messaging system
        /// </summary>
        public NotificationManager(bool liveMode)
        {
            _liveMode = liveMode;
            Messages = new ConcurrentQueue<Notification>();
            _count = 0;
            _resetTime = DateTime.Now;
        }

        /// <summary>
        /// Maintain a rate limit of the notification messages per hour send of roughly 20 messages per hour.
        /// </summary>
        /// <returns>True on under rate limit and acceptable to send message</returns>
        private bool Allow()
        {
            if (DateTime.Now > _resetTime)
            {
                _count = 0;
                _resetTime = DateTime.Now.RoundUp(TimeSpan.FromHours(1));
            }

            if (_count < _rateLimit)
            {
                _count++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Send an email to the address specified for live trading notifications.
        /// </summary>
        /// <param name="subject">Subject of the email</param>
        /// <param name="message">Message body, up to 10kb</param>
        /// <param name="data">Data attachment (optional)</param>
        /// <param name="address">Email address to send to</param>
        public bool Email(string address, string subject, string message, string data = "")
        {
            if (!_liveMode) return false;
            var allow = Allow();

            if (allow)
            {
                var email = new NotificationEmail(address, subject, message, data);
                Messages.Enqueue(email);
            }

            return allow;
        }

        /// <summary>
        /// Send an SMS to the phone number specified
        /// </summary>
        /// <param name="phoneNumber">Phone number to send to</param>
        /// <param name="message">Message to send</param>
        public bool Sms(string phoneNumber, string message)
        {
            if (!_liveMode) return false;
            var allow = Allow();
            if (allow)
            {
                var sms = new NotificationSms(phoneNumber, message);
                Messages.Enqueue(sms);
            }
            return allow;
        }

        /// <summary>
        /// Place REST POST call to the specified address with the specified DATA.
        /// </summary>
        /// <param name="address">Endpoint address</param>
        /// <param name="data">Data to send in body JSON encoded (optional)</param>
        public bool Web(string address, object data = null)
        {
            if (!_liveMode) return false;
            var allow = Allow();
            if (allow)
            {
                var web = new NotificationWeb(address, data);
                Messages.Enqueue(web);
            }
            return allow;
        }
    }
}
