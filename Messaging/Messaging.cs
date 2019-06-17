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
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Notifications;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Messaging
{
    /// <summary>
    /// Local/desktop implementation of messaging system for Lean Engine.
    /// </summary>
    public class Messaging : IMessagingHandler
    {
        // used to aid in generating regression tests via Cosole.WriteLine(...)
        private static readonly TextWriter Console = System.Console.Out;
        private static readonly bool UpdateRegressionStatistics = Config.GetBool("regression-update-statistics", false);

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
                        // inject alpha statistics into backtesting result statistics
                        // this is primarily so we can easily regression test these values
                        var alphaStatistics = result.Results.AlphaRuntimeStatistics?.ToDictionary().ToList() ?? new List<KeyValuePair<string, string>>();
                        alphaStatistics.ForEach(kvp => result.Results.Statistics.Add(kvp));

                        if (UpdateRegressionStatistics && _job.Language == Language.CSharp)
                        {
                            UpdateRegressionStatisticsInSourceFile(result);
                        }

                        var statisticsStr = $"{Environment.NewLine}" +
                            $"{string.Join(Environment.NewLine,result.Results.Statistics.Select(x => $"STATISTICS:: {x.Key} {x.Value}"))}";
                        Log.Trace(statisticsStr);
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

        private void UpdateRegressionStatisticsInSourceFile(BacktestResultPacket result)
        {
            if (!result.Results.Statistics.Any())
            {
                Log.Error("Messaging.UpdateRegressionStatisticsInSourceFile(): No statistics generated. Skipping update.");
                return;
            }

            var algorithmSource = $"../../../Algorithm.CSharp/{_job.AlgorithmId}.cs";
            var file = File.ReadAllLines(algorithmSource).ToList().GetEnumerator();
            var lines = new List<string>();
            var insideStats = false;
            while (file.MoveNext())
            {
                var line = file.Current;
                if (line == null)
                {
                    continue;
                }

                if (line.Contains("public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>"))
                {
                    insideStats = true;
                    lines.Add(line);
                    lines.Add("        {");
                }

                if (!insideStats)
                {
                    lines.Add(line);
                }
                else
                {
                    insideStats = false;
                    foreach (var pair in result.Results.Statistics)
                    {
                        lines.Add($"            {{\"{pair.Key}\", \"{pair.Value}\"}},");
                    }

                    // remove trailing comma
                    var lastLine = lines[lines.Count - 1];
                    lines[lines.Count - 1] = lastLine.Substring(0, lastLine.Length - 1);

                    while (file.MoveNext())
                    {
                        line = file.Current;
                        if (line == null)
                        {
                            continue;
                        }

                        if (line.Contains("};"))
                        {
                            lines.Add(line);
                            break;
                        }
                    }
                }
            }

            file.DisposeSafely();
            File.WriteAllLines(algorithmSource, lines);
        }
    }
}
