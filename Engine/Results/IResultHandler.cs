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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Handle the results of the backtest: where should we send the profit, portfolio updates:
    /// Backtester or the Live trading platform:
    /// </summary>
    [InheritedExport(typeof(IResultHandler))]
    public interface IResultHandler
    {
        /// <summary>
        /// Put messages to process into the queue so they are processed by this thread.
        /// </summary>
        ConcurrentQueue<Packet> Messages
        {
            get;
            set;
        }

        /// <summary>
        /// Charts collection for storing the master copy of user charting data.
        /// </summary>
        ConcurrentDictionary<string, Chart> Charts
        {
            get;
            set;
        }

        /// <summary>
        /// Sampling period for timespans between resamples of the charting equity.
        /// </summary>
        /// <remarks>Specifically critical for backtesting since with such long timeframes the sampled data can get extreme.</remarks>
        TimeSpan ResamplePeriod
        {
            get;
        }

        /// <summary>
        /// How frequently the backtests push messages to the browser.
        /// </summary>
        /// <remarks>Update frequency of notification packets</remarks>
        TimeSpan NotificationPeriod
        {
            get;
        }

        /// <summary>
        /// Boolean flag indicating the result hander thread is busy.
        /// False means it has completely finished and ready to dispose.
        /// </summary>
        bool IsActive
        {
            get;
        }

        /// <summary>
        /// Initialize the result handler with this result packet.
        /// </summary>
        /// <param name="job">Algorithm job packet for this result handler</param>
        /// <param name="messagingHandler">The messaging handler provider to use</param>
        /// <param name="api">The api implementation to use</param>
        /// <param name="transactionHandler"></param>
        void Initialize(AlgorithmNodePacket job, IMessagingHandler messagingHandler, IApi api, ITransactionHandler transactionHandler);

        /// <summary>
        /// Primary result thread entry point to process the result message queue and send it to whatever endpoint is set.
        /// </summary>
        void Run();

        /// <summary>
        /// Process debug messages with the preconfigured settings.
        /// </summary>
        /// <param name="message">String debug message</param>
        void DebugMessage(string message);

        /// <summary>
        /// Process system debug messages with the preconfigured settings.
        /// </summary>
        /// <param name="message">String debug message</param>
        void SystemDebugMessage(string message);

        /// <summary>
        /// Send a list of security types to the browser
        /// </summary>
        /// <param name="types">Security types list inside algorithm</param>
        void SecurityType(List<SecurityType> types);

        /// <summary>
        /// Send a logging message to the log list for storage.
        /// </summary>
        /// <param name="message">Message we'd in the log.</param>
        void LogMessage(string message);

        /// <summary>
        /// Send an error message back to the browser highlighted in red with a stacktrace.
        /// </summary>
        /// <param name="error">Error message we'd like shown in console.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        void ErrorMessage(string error, string stacktrace = "");

        /// <summary>
        /// Send a runtime error message back to the browser highlighted with in red
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        void RuntimeError(string message, string stacktrace = "");

        /// <summary>
        /// Add a sample to the chart specified by the chartName, and seriesName.
        /// </summary>
        /// <param name="chartName">String chart name to place the sample.</param>
        /// <param name="seriesName">Series name for the chart.</param>
        /// <param name="seriesType">Series type for the chart.</param>
        /// <param name="time">Time for the sample</param>
        /// <param name="value">Value for the chart sample.</param>
        /// <param name="unit">Unit for the sample chart</param>
        /// <param name="seriesIndex">Index of the series we're sampling</param>
        /// <remarks>Sample can be used to create new charts or sample equity - daily performance.</remarks>
        void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, DateTime time, decimal value, string unit = "$");

        /// <summary>
        /// Wrapper methond on sample to create the equity chart.
        /// </summary>
        /// <param name="time">Time of the sample.</param>
        /// <param name="value">Equity value at this moment in time.</param>
        /// <seealso cref="Sample(string,string,int,SeriesType,DateTime,decimal,string)"/>
        void SampleEquity(DateTime time, decimal value);

        /// <summary>
        /// Sample the current daily performance directly with a time-value pair.
        /// </summary>
        /// <param name="time">Current backtest date.</param>
        /// <param name="value">Current daily performance value.</param>
        /// <seealso cref="Sample(string,string,int,SeriesType,DateTime,decimal,string)"/>
        void SamplePerformance(DateTime time, decimal value);

        /// <summary>
        /// Sample the current benchmark performance directly with a time-value pair.
        /// </summary>
        /// <param name="time">Current backtest date.</param>
        /// <param name="value">Current benchmark value.</param>
        /// <seealso cref="Sample(string,string,int,SeriesType,DateTime,decimal,string)"/>
        void SampleBenchmark(DateTime time, decimal value);

        /// <summary>
        /// Sample the asset prices to generate plots.
        /// </summary>
        /// <param name="symbol">Symbol we're sampling.</param>
        /// <param name="time">Time of sample</param>
        /// <param name="value">Value of the asset price</param>
        /// <seealso cref="Sample(string,string,int,SeriesType,DateTime,decimal,string)"/>
        void SampleAssetPrices(Symbol symbol, DateTime time, decimal value);

        /// <summary>
        /// Add a range of samples from the users algorithms to the end of our current list.
        /// </summary>
        /// <param name="samples">Chart updates since the last request.</param>
        /// <seealso cref="Sample(string,string,int,SeriesType,DateTime,decimal,string)"/>
        void SampleRange(List<Chart> samples);

        /// <summary>
        /// Set the algorithm of the result handler after its been initialized.
        /// </summary>
        /// <param name="algorithm">Algorithm object matching IAlgorithm interface</param>
        /// <param name="startingPortfolioValue">Algorithm starting capital for statistics calculations</param>
        void SetAlgorithm(IAlgorithm algorithm, decimal startingPortfolioValue);

        /// <summary>
        /// Sets the current alpha runtime statistics
        /// </summary>
        /// <param name="statistics">The current alpha runtime statistics</param>
        void SetAlphaRuntimeStatistics(AlphaRuntimeStatistics statistics);

        /// <summary>
        /// Save the snapshot of the total results to storage.
        /// </summary>
        /// <param name="packet">Packet to store.</param>
        /// <param name="async">Store the packet asyncronously to speed up the thread.</param>
        /// <remarks>Async creates crashes in Mono 3.10 if the thread disappears before the upload is complete so it is disabled for now.</remarks>
        void StoreResult(Packet packet, bool async = false);

        /// <summary>
        /// Post the final result back to the controller worker if backtesting, or to console if local.
        /// </summary>
        void SendFinalResult();

        /// <summary>
        /// Send a algorithm status update to the user of the algorithms running state.
        /// </summary>
        /// <param name="status">Status enum of the algorithm.</param>
        /// <param name="message">Optional string message describing reason for status change.</param>
        void SendStatusUpdate(AlgorithmStatus status, string message = "");

        /// <summary>
        /// Set a dynamic runtime statistic to show in the (live) algorithm header
        /// </summary>
        /// <param name="key">Runtime headline statistic name</param>
        /// <param name="value">Runtime headline statistic value</param>
        void RuntimeStatistic(string key, string value);

        /// <summary>
        /// Send a new order event.
        /// </summary>
        /// <param name="newEvent">Update, processing or cancellation of an order, update the IDE in live mode or ignore in backtesting.</param>
        void OrderEvent(OrderEvent newEvent);

        /// <summary>
        /// Terminate the result thread and apply any required exit proceedures.
        /// </summary>
        void Exit();

        /// <summary>
        /// Purge/clear any outstanding messages in message queue.
        /// </summary>
        void PurgeQueue();

        /// <summary>
        /// Process any synchronous events in here that are primarily triggered from the algorithm loop
        /// </summary>
        void ProcessSynchronousEvents(bool forceProcess = false);

        /// <summary>
        /// Save the logs
        /// </summary>
        /// <param name="id">Id that will be incorporated into the algorithm log name</param>
        /// <param name="logs">The logs to save</param>
        string SaveLogs(string id, IEnumerable<string> logs);

        /// <summary>
        /// Save the results
        /// </summary>
        /// <param name="name">The name of the results</param>
        /// <param name="result">The results to save</param>
        void SaveResults(string name, Result result);

        /// <summary>
        /// Sets the current Data Manager instance
        /// </summary>
        void SetDataManager(IDataFeedSubscriptionManager dataManager);
    }
}
