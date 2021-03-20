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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Python.Runtime;

namespace QuantConnect.Notifications
{
    /// <summary>
    /// Local/desktop implementation of messaging system for Lean Engine.
    /// </summary>
    public class NotificationManager
    {
        private const int RateLimit = 30;

        private int _count;
        private DateTime _resetTime;

        private readonly bool _liveMode;
        private readonly object _sync = new object();

        /// <summary>
        /// Public access to the messages
        /// </summary>
        public ConcurrentQueue<Notification> Messages { get; set; }

        /// <summary>
        /// Initialize the messaging system
        /// </summary>
        public NotificationManager(bool liveMode)
        {
            _count = 0;
            _liveMode = liveMode;
            Messages = new ConcurrentQueue<Notification>();

            // start counting reset time based on first invocation of NotificationManager
            _resetTime = default(DateTime);
        }

        /// <summary>
        /// Send an email to the address specified for live trading notifications.
        /// </summary>
        /// <param name="subject">Subject of the email</param>
        /// <param name="message">Message body, up to 10kb</param>
        /// <param name="data">Data attachment (optional)</param>
        /// <param name="address">Email address to send to</param>
        /// <param name="headers">Optional email headers to use</param>
        public bool Email(string address, string subject, string message, string data, PyObject headers)
        {
            return Email(address, subject, message, data, headers.ConvertToDictionary<string, string>());
        }

        /// <summary>
        /// Send an email to the address specified for live trading notifications.
        /// </summary>
        /// <param name="subject">Subject of the email</param>
        /// <param name="message">Message body, up to 10kb</param>
        /// <param name="data">Data attachment (optional)</param>
        /// <param name="address">Email address to send to</param>
        /// <param name="headers">Optional email headers to use</param>
        public bool Email(string address, string subject, string message, string data = "", Dictionary<string, string> headers = null)
        {
            if (!Allow())
            {
                return false;
            }

            var email = new NotificationEmail(address, subject, message, data, headers);
            Messages.Enqueue(email);

            return true;
        }

        /// <summary>
        /// Send an SMS to the phone number specified
        /// </summary>
        /// <param name="phoneNumber">Phone number to send to</param>
        /// <param name="message">Message to send</param>
        public bool Sms(string phoneNumber, string message)
        {
            if (!Allow())
            {
                return false;
            }

            var sms = new NotificationSms(phoneNumber, message);
            Messages.Enqueue(sms);

            return true;
        }

        /// <summary>
        /// Place REST POST call to the specified address with the specified DATA.
        /// </summary>
        /// <param name="address">Endpoint address</param>
        /// <param name="data">Data to send in body JSON encoded (optional)</param>
        /// <param name="headers">Optional headers to use</param>
        public bool Web(string address, object data = null, Dictionary<string, string> headers = null)
        {
            if (!Allow())
            {
                return false;
            }

            var web = new NotificationWeb(address, data, headers);
            Messages.Enqueue(web);

            return true;
        }

        /// <summary>
        /// Maintain a rate limit of the notification messages per hour send of roughly 20 messages per hour.
        /// </summary>
        /// <returns>True when running in live mode and under the rate limit</returns>
        private bool Allow()
        {
            if (!_liveMode)
            {
                return false;
            }

            lock (_sync)
            {
                var now = DateTime.UtcNow;
                if (now > _resetTime)
                {
                    _count = 0;

                    // rate limiting set at 30/hour
                    _resetTime = now.Add(TimeSpan.FromHours(1));
                }

                if (_count < RateLimit)
                {
                    _count++;
                    return true;
                }

                return false;
            }
        }
    }
}
