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
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.WorkScheduling;
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
        /// Get the maximum time that the creation of an algorithm can take
        /// </summary>
        public static TimeSpan AlgorithmCreationTimeout { get; } = TimeSpan.FromSeconds(Config.GetDouble("algorithm-creation-timeout", 90));

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
                .Where(x => x.CurrencyConversion != null && x.ConversionRate == 0)
                .ToList();

            var securitiesToUpdate = cashToUpdate
                .SelectMany(x => x.CurrencyConversion.ConversionRateSecurities)
                .Distinct()
                .ToList();

            var historyRequestFactory = new HistoryRequestFactory(algorithm);
            var historyRequests = new List<HistoryRequest>();
            foreach (var security in securitiesToUpdate)
            {
                var configs = algorithm
                    .SubscriptionManager
                    .SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(security.Symbol,
                        includeInternalConfigs: true);

                // we need to order and select a specific configuration type
                // so the conversion rate is deterministic
                var configToUse = configs.OrderBy(x => x.TickType).First();
                var hours = security.Exchange.Hours;

                var resolution = configs.GetHighestResolution();
                var startTime = historyRequestFactory.GetStartTimeAlgoTz(
                    security.Symbol,
                    60,
                    resolution,
                    hours,
                    configToUse.DataTimeZone);
                var endTime = algorithm.Time;

                historyRequests.Add(historyRequestFactory.CreateHistoryRequest(
                    configToUse,
                    startTime,
                    endTime,
                    security.Exchange.Hours,
                    resolution));
            }

            // Attempt to get history for these requests and update cash
            var slices = algorithm.HistoryProvider.GetHistory(historyRequests, algorithm.TimeZone);
            slices.PushThrough(data =>
            {
                foreach (var security in securitiesToUpdate.Where(x => x.Symbol == data.Symbol))
                {
                    security.SetMarketPrice(data);
                }
            });

            foreach (var cash in cashToUpdate)
            {
                cash.Update();
            }

            // Any remaining unassigned cash will attempt to fall back to a daily resolution history request to resolve
            var unassignedCash = cashToUpdate.Where(x => x.ConversionRate == 0).ToList();
            if (unassignedCash.Any())
            {
                Log.Trace(
                    $"Failed to assign conversion rates for the following cash: {string.Join(",", unassignedCash.Select(x => x.Symbol))}." +
                    $" Attempting to request daily resolution history to resolve conversion rate");

                var unassignedCashSymbols = unassignedCash
                    .SelectMany(x => x.SecuritySymbols)
                    .ToHashSet();

                var replacementHistoryRequests = new List<HistoryRequest>();
                foreach (var request in historyRequests.Where(x =>
                    unassignedCashSymbols.Contains(x.Symbol) && x.Resolution < Resolution.Daily))
                {
                    var newRequest = new HistoryRequest(request.EndTimeUtc.AddDays(-10), request.EndTimeUtc,
                        request.DataType,
                        request.Symbol, Resolution.Daily, request.ExchangeHours, request.DataTimeZone,
                        request.FillForwardResolution,
                        request.IncludeExtendedMarketHours, request.IsCustomData, request.DataNormalizationMode,
                        request.TickType);

                    replacementHistoryRequests.Add(newRequest);
                }

                slices = algorithm.HistoryProvider.GetHistory(replacementHistoryRequests, algorithm.TimeZone);
                slices.PushThrough(data =>
                {
                    foreach (var security in securitiesToUpdate.Where(x => x.Symbol == data.Symbol))
                    {
                        security.SetMarketPrice(data);
                    }
                });

                foreach (var cash in unassignedCash)
                {
                    cash.Update();
                }
            }

            Log.Trace($"BaseSetupHandler.SetupCurrencyConversions():{Environment.NewLine}" +
                $"Account Type: {algorithm.BrokerageModel.AccountType}{Environment.NewLine}{Environment.NewLine}{algorithm.Portfolio.CashBook}");
            // this is useful for debugging
            algorithm.Portfolio.LogMarginInformation();
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
                () => {
                    DebuggerHelper.Initialize(algorithmNodePacket.Language, out var workersInitializationCallback);

                    if(workersInitializationCallback != null)
                    {
                        // initialize workers for debugging if required
                        WeightedWorkScheduler.Instance.AddSingleCallForAll(workersInitializationCallback);
                    }
                },
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

        /// <summary>
        /// Get the available data feeds from config.json,
        /// </summary>
        public static Dictionary<SecurityType, List<TickType>> GetConfiguredDataFeeds()
        {
            var dataFeedsConfigString = Config.Get("security-data-feeds");

            if (!dataFeedsConfigString.IsNullOrEmpty())
            {
                var dataFeeds = JsonConvert.DeserializeObject<Dictionary<SecurityType, List<TickType>>>(dataFeedsConfigString);
                return dataFeeds;
            }

            return null;
        }

        /// <summary>
        /// Set the number of trading days per year based on the specified brokerage model.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>
        /// The number of trading days per year. For specific brokerages (Coinbase, Binance, Bitfinex, Bybit, FTX, Kraken),
        /// the value is 365. For other brokerages, the default value is 252.
        /// </returns>
        public static void SetBrokerageTradingDayPerYear(IAlgorithm algorithm)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            algorithm.Settings.TradingDaysPerYear ??= algorithm.BrokerageModel switch
            {
                CoinbaseBrokerageModel
                or BinanceBrokerageModel
                or BitfinexBrokerageModel
                or BybitBrokerageModel
                or FTXBrokerageModel
                or KrakenBrokerageModel => 365,
                _ => 252
            };
        }
    }
}
