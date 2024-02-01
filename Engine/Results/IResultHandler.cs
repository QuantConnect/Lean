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
using QuantConnect.Brokerages;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Handle the results of the backtest: where should we send the profit, portfolio updates:
    /// Backtester or the Live trading platform:
    /// </summary>
    [InheritedExport(typeof(IResultHandler))]
    public interface IResultHandler : IStatisticsService
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
        /// Boolean flag indicating the result hander thread is busy.
        /// False means it has completely finished and ready to dispose.
        /// </summary>
        bool IsActive
        {
            get;
        }

        /// <summary>
        /// Event fired each time that we add/remove securities from the data feed
        /// </summary>
        void OnSecuritiesChanged(SecurityChanges changes);

        /// <summary>
        /// Initialize the result handler with this result packet.
        /// </summary>
        /// <param name="parameters">DTO parameters class to initialize a result handler</param>
        void Initialize(ResultHandlerInitializeParameters parameters);

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
        /// Process brokerage message events
        /// </summary>
        /// <param name="brokerageMessageEvent">The brokerage message event</param>
        void BrokerageMessage(BrokerageMessageEvent brokerageMessageEvent);

        /// <summary>
        /// Method to update the <see cref="IResultHandler"/> with various performance metrics.
        /// Called once a day by scheduled event in AlgorithmManager
        /// </summary>
        /// <param name="time">Current time</param>
        void Sample(DateTime time);

        /// <summary>
        /// Set the algorithm of the result handler after its been initialized.
        /// </summary>
        /// <param name="algorithm">Algorithm object matching IAlgorithm interface</param>
        /// <param name="startingPortfolioValue">Algorithm starting capital for statistics calculations</param>
        void SetAlgorithm(IAlgorithm algorithm, decimal startingPortfolioValue);

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
        /// Terminate the result thread and apply any required exit procedures like sending final results.
        /// </summary>
        void Exit();

        /// <summary>
        /// Process any synchronous events in here that are primarily triggered from the algorithm loop
        /// </summary>
        void ProcessSynchronousEvents(bool forceProcess = false);

        /// <summary>
        /// Save the results
        /// </summary>
        /// <param name="name">The name of the results</param>
        /// <param name="result">The results to save</param>
        void SaveResults(string name, Result result);

        /// <summary>
        /// Handles updates to the algorithm's name
        /// </summary>
        /// <param name="name">The new name</param>
        void AlgorithmNameUpdated(string name);

        /// <summary>
        /// Handles updates to the algorithm's tags
        /// </summary>
        /// <param name="tags">The new tags</param>
        void AlgorithmTagsUpdated(HashSet<string> tags);
    }
}
