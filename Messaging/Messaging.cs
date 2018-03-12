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
using System.IO;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Notifications;
using QuantConnect.Packets;

namespace QuantConnect.Messaging
{
    /// <summary>
    /// Local/desktop implementation of messaging system for Lean Engine.
    /// </summary>
    public class Messaging : IMessagingHandler
    {
        // used to aid in generating regression tests via Cosole.WriteLine(...)
        private static readonly TextWriter Console = System.Console.Out;

        private AlgorithmNodePacket _job;

        /// <summary>
        /// This implementation ignores the <seealso cref="HasSubscribers"/> flag and
        /// instead will always write to the log.
        /// </summary>
        public bool HasSubscribers
        {
            get;
            set;
        }

        /// <summary>
        /// Initialize the messaging system
        /// </summary>
        public void Initialize()
        {
            //
        }

        /// <summary>
        /// Set the messaging channel
        /// </summary>
        public void SetAuthentication(AlgorithmNodePacket job)
        {
            _job = job;
        }

        /// <summary>
        /// Send a generic base packet without processing
        /// </summary>
        public void Send(Packet packet)
        {
            switch (packet.Type)
            {
                case PacketType.Debug:
                    var debug = (DebugPacket) packet;
                    Log.Trace("Debug: " + debug.Message);
                    break;

                case PacketType.SystemDebug:
                    var systemDebug = (SystemDebugPacket)packet;
                    Log.Trace("Debug: " + systemDebug.Message);
                    break;

                case PacketType.Log:
                    var log = (LogPacket) packet;
                    Log.Trace("Log: " + log.Message);
                    break;

                case PacketType.RuntimeError:
                    var runtime = (RuntimeErrorPacket) packet;
                    var rstack = (!string.IsNullOrEmpty(runtime.StackTrace) ? (Environment.NewLine + " " + runtime.StackTrace) : string.Empty);
                    Log.Error(runtime.Message + rstack);
                    break;

                case PacketType.HandledError:
                    var handled = (HandledErrorPacket) packet;
                    var hstack = (!string.IsNullOrEmpty(handled.StackTrace) ? (Environment.NewLine + " " + handled.StackTrace) : string.Empty);
                    Log.Error(handled.Message + hstack);
                    break;

                case PacketType.AlphaResult:
                    // spams the logs
                    //var insights = ((AlphaResultPacket) packet).Insights;
                    //foreach (var insight in insights)
                    //{
                    //    Log.Trace("Insight: " + insight);
                    //}
                    break;

                case PacketType.BacktestResult:
                    var result = (BacktestResultPacket) packet;

                    if (result.Progress == 1)
                    {
                        // uncomment these code traces to help write regression tests
                        //Console.WriteLine("new Dictionary<string, string>");
                        //Console.WriteLine("\t\t\t{");

                        // inject alpha statistics into backtesting result statistics
                        // this is primarily so we can easily regression test these values
                        var alphaStatistics = result.Results.AlphaRuntimeStatistics?.ToDictionary().ToList() ?? new List<KeyValuePair<string, string>>();
                        alphaStatistics.ForEach(kvp => result.Results.Statistics.Add(kvp));

                        foreach (var pair in result.Results.Statistics)
                        {
                            Log.Trace($"STATISTICS:: {pair.Key} {pair.Value}");
                            //Console.WriteLine("\t\t\t\t{{\"{0}\",\"{1}\"}},", pair.Key, pair.Value);
                        }
                        //Console.WriteLine("\t\t\t};");

                        //foreach (var pair in statisticsResults.RollingPerformances)
                        //{
                        //    Log.Trace("ROLLINGSTATS:: " + pair.Key + " SharpeRatio: " + Math.Round(pair.Value.PortfolioStatistics.SharpeRatio, 3));
                        //}
                    }
                    break;
            }


            if (StreamingApi.IsEnabled)
            {
                StreamingApi.Transmit(_job.UserId, _job.Channel, packet);
            }
        }

        /// <summary>
        /// Send any notification with a base type of Notification.
        /// </summary>
        public void SendNotification(Notification notification)
        {
            var type = notification.GetType();
            if (type == typeof (NotificationEmail)
             || type == typeof (NotificationWeb)
             || type == typeof (NotificationSms))
            {
                Log.Error("Messaging.SendNotification(): Send not implemented for notification of type: " + type.Name);
                return;
            }
            notification.Send();
        }
    }
}
