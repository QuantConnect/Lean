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
using System.Linq;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    ///  Base class that provides shared code for
    /// the <see cref="ISetupHandler"/> implementations
    /// </summary>
    public static class BaseSetupHandler
    {
        /// <summary>
        /// Will first check and add all the required conversion rate securities
        /// and later will seed an initial value to them.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="universeSelection">The universe selection instance</param>
        public static void SetupCurrencyConversions(
            IAlgorithm algorithm,
            UniverseSelection universeSelection)
        {
            // this is needed to have non-zero currency conversion rates during warmup
            // will also set the Cash.ConversionRateSecurity
            universeSelection.EnsureCurrencyDataFeeds(SecurityChanges.None);

            // now set conversion rates
            var cashToUpdate = algorithm.Portfolio.CashBook.Values
                .Where(x => x.ConversionRateSecurity != null && x.ConversionRate == 0)
                .ToList();

            var historyRequestFactory = new HistoryRequestFactory(algorithm);
            var historyRequests = new List<HistoryRequest>();
            foreach (var cash in cashToUpdate)
            {
                var configs = algorithm
                    .SubscriptionManager
                    .SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(cash.ConversionRateSecurity.Symbol,
                        includeInternalConfigs:true);

                var resolution = configs.GetHighestResolution();

                var startTime = historyRequestFactory.GetStartTimeAlgoTz(
                    cash.ConversionRateSecurity.Symbol,
                    1,
                    resolution,
                    cash.ConversionRateSecurity.Exchange.Hours);
                var endTime = algorithm.Time.RoundDown(resolution.ToTimeSpan());

                // we need to order and select a specific configuration type
                // so the conversion rate is deterministic
                var configToUse = configs.OrderBy(x => x.TickType).First();

                historyRequests.Add(historyRequestFactory.CreateHistoryRequest(
                    configToUse,
                    startTime,
                    endTime,
                    cash.ConversionRateSecurity.Exchange.Hours,
                    resolution));
            }

            var slices = algorithm.HistoryProvider.GetHistory(historyRequests, algorithm.TimeZone);
            slices.PushThrough(data =>
            {
                foreach (var cash in cashToUpdate
                    .Where(x => x.ConversionRateSecurity.Symbol == data.Symbol))
                {
                    cash.Update(data);
                }
            });

            Log.Trace("BaseSetupHandler.SetupCurrencyConversions():" +
                $"{Environment.NewLine}{algorithm.Portfolio.CashBook}");
        }

        /// <summary>
        /// Initialize the debugger
        /// </summary>
        /// <param name="algorithmNodePacket">The algorithm node packet</param>
        /// <param name="workerThread">The worker thread instance to use</param>
        public static bool InitializeDebugging(AlgorithmNodePacket algorithmNodePacket, WorkerThread workerThread)
        {
            var isolator = new Isolator();
            return isolator.ExecuteWithTimeLimit(TimeSpan.FromMinutes(5),
                () => DebuggerHelper.Initialize(algorithmNodePacket.Language),
                algorithmNodePacket.RamAllocation,
                sleepIntervalMillis: 100,
                workerThread: workerThread);
        }

        /// <summary>
        /// Sets the initial cash for the algorithm if set in the job packet.
        /// </summary>
        /// <remarks>Should be called after initialize <see cref="LoadBacktestJobAccountCurrency"/></remarks>
        public static void LoadBacktestJobCashAmount(IAlgorithm algorithm, BacktestNodePacket job)
        {
            // set initial cash, if present in the job
            if (job.CashAmount.HasValue)
            {
                // Zero the CashBook - we'll populate directly from job
                foreach (var kvp in algorithm.Portfolio.CashBook)
                {
                    kvp.Value.SetAmount(0);
                }

                algorithm.SetCash(job.CashAmount.Value.Amount);
            }
        }

        /// <summary>
        /// Sets the account currency the algorithm should use if set in the job packet
        /// </summary>
        /// <remarks>Should be called before initialize <see cref="LoadBacktestJobCashAmount"/></remarks>
        public static void LoadBacktestJobAccountCurrency(IAlgorithm algorithm, BacktestNodePacket job)
        {
            // set account currency if present in the job
            if (job.CashAmount.HasValue)
            {
                algorithm.SetAccountCurrency(job.CashAmount.Value.Currency);
            }
        }
    }
}
