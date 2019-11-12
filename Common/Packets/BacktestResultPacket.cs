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
 *
*/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Statistics;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Backtest result packet: send backtest information to GUI for user consumption.
    /// </summary>
    public class BacktestResultPacket : Packet
    {
        /// <summary>
        /// User Id placing this task
        /// </summary>
        [JsonProperty(PropertyName = "iUserID")]
        public int UserId = 0;

        /// <summary>
        /// Project Id of the this task.
        /// </summary>
        [JsonProperty(PropertyName = "iProjectID")]
        public int ProjectId = 0;

        /// <summary>
        /// User Session Id
        /// </summary>
        [JsonProperty(PropertyName = "sSessionID")]
        public string SessionId = "";

        /// <summary>
        /// BacktestId for this result packet
        /// </summary>
        [JsonProperty(PropertyName = "sBacktestID")]
        public string BacktestId = "";

        /// <summary>
        /// Compile Id for the algorithm which generated this result packet.
        /// </summary>
        [JsonProperty(PropertyName = "sCompileID")]
        public string CompileId = "";

        /// <summary>
        /// Start of the backtest period as defined in Initialize() method.
        /// </summary>
        [JsonProperty(PropertyName = "dtPeriodStart")]
        public DateTime PeriodStart = DateTime.Now;

        /// <summary>
        /// End of the backtest period as defined in the Initialize() method.
        /// </summary>
        [JsonProperty(PropertyName = "dtPeriodFinish")]
        public DateTime PeriodFinish = DateTime.Now;

        /// <summary>
        /// DateTime (EST) the user requested this backtest.
        /// </summary>
        [JsonProperty(PropertyName = "dtDateRequested")]
        public DateTime DateRequested = DateTime.Now;

        /// <summary>
        /// DateTime (EST) when the backtest was completed.
        /// </summary>
        [JsonProperty(PropertyName = "dtDateFinished")]
        public DateTime DateFinished = DateTime.Now;

        /// <summary>
        /// Progress of the backtest as a percentage from 0-1 based on the days lapsed from start-finish.
        /// </summary>
        [JsonProperty(PropertyName = "dProgress")]
        public decimal Progress = 0;

        /// <summary>
        /// Runmode for this backtest.
        /// </summary>
        /// <obsolete>The RunMode property has been made obsolete and all backtests will be run in series mode.</obsolete>
        [Obsolete("The RunMode property has been made obsolete and all backtests will be run in series mode.")]
        [JsonProperty(PropertyName = "eRunMode")]
        public RunMode RunMode = RunMode.Series;

        /// <summary>
        /// Name of this backtest.
        /// </summary>
        [JsonProperty(PropertyName = "sName")]
        public string Name = String.Empty;

        /// <summary>
        /// Result data object for this backtest
        /// </summary>
        [JsonProperty(PropertyName = "oResults")]
        public BacktestResult Results = new BacktestResult();

        /// <summary>
        /// Processing time of the algorithm (from moment the algorithm arrived on the algorithm node)
        /// </summary>
        [JsonProperty(PropertyName = "dProcessingTime")]
        public double ProcessingTime = 0;

        /// <summary>
        /// Estimated number of tradeable days in the backtest based on the start and end date or the backtest
        /// </summary>
        [JsonProperty(PropertyName = "iTradeableDates")]
        public int TradeableDates = 0;

        /// <summary>
        /// Default constructor for JSON Serialization
        /// </summary>
        public BacktestResultPacket()
            : base(PacketType.BacktestResult)
        { }

        /// <summary>
        /// Compose the packet from a JSON string:
        /// </summary>
        public BacktestResultPacket(string json)
            : base (PacketType.BacktestResult)
        {
            try
            {
                var packet = JsonConvert.DeserializeObject<BacktestResultPacket>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                CompileId           = packet.CompileId;
                Channel             = packet.Channel;
                PeriodFinish        = packet.PeriodFinish;
                PeriodStart         = packet.PeriodStart;
                Progress            = packet.Progress;
                SessionId           = packet.SessionId;
                BacktestId          = packet.BacktestId;
                Type                = packet.Type;
                UserId              = packet.UserId;
                DateFinished        = packet.DateFinished;
                DateRequested       = packet.DateRequested;
                Name                = packet.Name;
                ProjectId           = packet.ProjectId;
                Results             = packet.Results;
                ProcessingTime      = packet.ProcessingTime;
                TradeableDates      = packet.TradeableDates;
            }
            catch (Exception err)
            {
                Log.Trace($"BacktestResultPacket(): Error converting json: {err}");
            }
        }


        /// <summary>
        /// Compose result data packet - with tradable dates from the backtest job task and the partial result packet.
        /// </summary>
        /// <param name="job">Job that started this request</param>
        /// <param name="results">Results class for the Backtest job</param>
        /// <param name="endDate">The algorithms backtest end date</param>
        /// <param name="startDate">The algorithms backtest start date</param>
        /// <param name="progress">Progress of the packet. For the packet we assume progess of 100%.</param>
        public BacktestResultPacket(BacktestNodePacket job, BacktestResult results, DateTime endDate, DateTime startDate, decimal progress = 1m)
            : base(PacketType.BacktestResult)
        {
            try
            {
                Progress = Math.Round(progress, 3);
                SessionId = job.SessionId;
                PeriodFinish = endDate;
                PeriodStart = startDate;
                CompileId = job.CompileId;
                Channel = job.Channel;
                BacktestId = job.BacktestId;
                Results = results;
                Name = job.Name;
                UserId = job.UserId;
                ProjectId = job.ProjectId;
                SessionId = job.SessionId;
                TradeableDates = job.TradeableDates;
            }
            catch (Exception err) {
                Log.Error(err);
            }
        }

    } // End Queue Packet:


    /// <summary>
    /// Backtest results object class - result specific items from the packet.
    /// </summary>
    public class BacktestResult : Result
    {
        /// <summary>
        /// Rolling window detailed statistics.
        /// </summary>
        public Dictionary<string, AlgorithmPerformance> RollingWindow = new Dictionary<string, AlgorithmPerformance>();

        /// <summary>
        /// Rolling window detailed statistics.
        /// </summary>
        public AlgorithmPerformance TotalPerformance = null;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public BacktestResult()
        {

        }

        /// <summary>
        /// Constructor for the result class using dictionary objects.
        /// </summary>
        public BacktestResult(IDictionary<string, Chart> charts, IDictionary<int, Order> orders, IDictionary<DateTime, decimal> profitLoss, IDictionary<string, string> statistics, IDictionary<string, string> runtimeStatistics, Dictionary<string, AlgorithmPerformance> rollingWindow, AlgorithmPerformance totalPerformance = null)
        {
            Charts = charts;
            Orders = orders;
            ProfitLoss = profitLoss;
            Statistics = statistics;
            RuntimeStatistics = runtimeStatistics;
            RollingWindow = rollingWindow;
            TotalPerformance = totalPerformance;
        }
    }

} // End of Namespace:
