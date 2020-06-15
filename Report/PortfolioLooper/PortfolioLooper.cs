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

using Deedle;
using QuantConnect.Orders;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Report
{
    /// <summary>
    /// Runs LEAN to calculate the portfolio at a given time from <see cref="Order"/> objects.
    /// Generates and returns <see cref="PointInTimePortfolio"/> objects that represents
    /// the holdings and other miscellaneous metrics at a point in time by reprocessing the orders
    /// as they were filled.
    /// </summary>
    public class PortfolioLooper
    {
        /// <summary>
        /// Default resolution to read. This will affect the granularity of the results generated for FX and Crypto
        /// </summary>
        private const Resolution _resolution = Resolution.Hour;

        private SecurityService _securityService;
        private DataManager _dataManager;
        private IEnumerable<Slice> _conversionSlices = new List<Slice>();

        /// <summary>
        /// QCAlgorithm derived class that sets up internal data feeds for
        /// use with crypto and forex data, as well as managing the <see cref="SecurityPortfolioManager"/>
        /// </summary>
        public PortfolioLooperAlgorithm Algorithm { get; protected set; }

        /// <summary>
        /// Creates an instance of the PortfolioLooper class
        /// </summary>
        /// <param name="startingCash">Equity curve</param>
        /// <param name="orders">Order events</param>
        /// <param name="resolution">Optional parameter to override default resolution (Hourly)</param>
        private PortfolioLooper(double startingCash, List<Order> orders, Resolution resolution = _resolution)
        {
            // Initialize the providers that the HistoryProvider requires
            var factorFileProvider = Composer.Instance.GetExportedValueByTypeName<IFactorFileProvider>("LocalDiskFactorFileProvider");
            var mapFileProvider = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>("LocalDiskMapFileProvider");
            var dataCacheProvider = new ZipDataCacheProvider(new DefaultDataProvider(), false);
            var historyProvider = Composer.Instance.GetExportedValueByTypeName<IHistoryProvider>("SubscriptionDataReaderHistoryProvider");

            var dataPermissionManager = new DataPermissionManager();
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, dataCacheProvider, mapFileProvider, factorFileProvider, (_) => { }, false, dataPermissionManager));
            Algorithm = new PortfolioLooperAlgorithm((decimal)startingCash, orders);
            Algorithm.SetHistoryProvider(historyProvider);

            // Dummy LEAN datafeed classes and initializations that essentially do nothing
            var job = new BacktestNodePacket(1, 2, "3", null, 9m, $"");
            var feed = new MockDataFeed();

            // Create MHDB and Symbol properties DB instances for the DataManager
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            _dataManager = new DataManager(feed,
                new UniverseSelection(
                    Algorithm,
                    new SecurityService(Algorithm.Portfolio.CashBook,
                        marketHoursDatabase,
                        symbolPropertiesDataBase,
                        Algorithm,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCacheProvider(Algorithm.Portfolio)),
                    dataPermissionManager),
                Algorithm,
                Algorithm.TimeKeeper,
                marketHoursDatabase,
                false,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager);

            _securityService = new SecurityService(Algorithm.Portfolio.CashBook,
                marketHoursDatabase,
                symbolPropertiesDataBase,
                Algorithm,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(Algorithm.Portfolio));

            var transactions = new BacktestingTransactionHandler();
            var results = new BacktestingResultHandler();

            // Initialize security services and other properties so that we
            // don't get null reference exceptions during our re-calculation
            Algorithm.Securities.SetSecurityService(_securityService);
            Algorithm.SubscriptionManager.SetDataManager(_dataManager);

            // Initializes all the proper Securities from the orders provided by the user
            Algorithm.FromOrders(orders);

            // Initialize the algorithm
            Algorithm.Initialize();
            Algorithm.PostInitialize();

            // More initialization, this time with Algorithm and other misc. classes
            results.Initialize(job, new Messaging.Messaging(), new Api.Api(), transactions);
            results.SetAlgorithm(Algorithm, Algorithm.Portfolio.TotalPortfolioValue);
            transactions.Initialize(Algorithm, new BacktestingBrokerage(Algorithm), results);
            feed.Initialize(Algorithm, job, results, null, null, null, _dataManager, null, null);

            // Begin setting up the currency conversion feed if needed
            var coreSecurities = Algorithm.Securities.Values.ToList();

            if (coreSecurities.Any(x => x.Symbol.SecurityType == SecurityType.Forex || x.Symbol.SecurityType == SecurityType.Crypto))
            {
                BaseSetupHandler.SetupCurrencyConversions(Algorithm, _dataManager.UniverseSelection);
                var conversionSecurities = Algorithm.Securities.Values.Where(s => !coreSecurities.Contains(s)).ToList();

                // Skip the history request if we don't need to convert anything
                if (conversionSecurities.Any())
                {
                    // Point-in-time Slices to convert FX and Crypto currencies to the portfolio currency
                    _conversionSlices = GetHistory(Algorithm, conversionSecurities, resolution);
                }
            }
        }

        /// <summary>
        /// Utility method to get historical data for a list of securities
        /// </summary>
        /// <param name="securities">List of securities you want historical data for</param>
        /// <param name="start">Start date of the history request</param>
        /// <param name="end">End date of the history request</param>
        /// <param name="resolution">Resolution of the history request</param>
        /// <returns>Enumerable of slices</returns>
        public static IEnumerable<Slice> GetHistory(List<Security> securities, DateTime start, DateTime end, Resolution resolution)
        {
            var looper = new PortfolioLooper(0, new List<Order>(), resolution);
            looper.Algorithm.SetStartDate(start);
            looper.Algorithm.SetEndDate(end);

            return GetHistory(looper.Algorithm, securities, resolution);
        }

        /// <summary>
        /// Gets the history for the given symbols from the <paramref name="start"/> to the <paramref name="end"/>
        /// </summary>
        /// <param name="symbols">Symbols to request history for</param>
        /// <param name="start">Start date of history request</param>
        /// <param name="end">End date of history request</param>
        /// <param name="resolution">Resolution of history request</param>
        /// <returns>Enumerable of slices</returns>
        public static IEnumerable<Slice> GetHistory(List<Symbol> symbols, DateTime start, DateTime end, Resolution resolution)
        {
            // Handles the conversion of Symbol to Security for us.
            var looper = new PortfolioLooper(0, new List<Order>(), resolution);
            var securities = new List<Security>();

            looper.Algorithm.SetStartDate(start);
            looper.Algorithm.SetEndDate(end);

            foreach (var symbol in symbols)
            {
                var configs = looper.Algorithm.SubscriptionManager.SubscriptionDataConfigService.Add(symbol, resolution, false, false);
                securities.Add(looper.Algorithm.Securities.CreateSecurity(symbol, configs));
            }

            return GetHistory(looper.Algorithm, securities, resolution);
        }

        /// <summary>
        /// Internal method to get the history for the given securities
        /// </summary>
        /// <param name="algorithm">Algorithm</param>
        /// <param name="securities">Securities to get history for</param>
        /// <param name="resolution">Resolution to retrieve data in</param>
        /// <returns>History of the given securities</returns>
        /// <remarks>Method is static because we want to use it from the constructor as well</remarks>
        private static IEnumerable<Slice> GetHistory(IAlgorithm algorithm, List<Security> securities, Resolution resolution)
        {
            var historyRequests = new List<Data.HistoryRequest>();

            // Create the history requests
            foreach (var security in securities)
            {
                var historyRequestFactory = new HistoryRequestFactory(algorithm);
                var configs = algorithm.SubscriptionManager
                    .SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(security.Symbol, includeInternalConfigs: true);

                var startTime = historyRequestFactory.GetStartTimeAlgoTz(
                    security.Symbol,
                    1,
                    resolution,
                    security.Exchange.Hours);
                var endTime = algorithm.EndDate;

                // we need to order and select a specific configuration type
                // so the conversion rate is deterministic
                var configToUse = configs.OrderBy(x => x.TickType).First();

                historyRequests.Add(historyRequestFactory.CreateHistoryRequest(
                    configToUse,
                    startTime,
                    endTime,
                    security.Exchange.Hours,
                    resolution
                ));
            }

            return algorithm.HistoryProvider.GetHistory(historyRequests, algorithm.TimeZone).ToList();
        }

        /// <summary>
        /// Gets the point in time portfolio over multiple deployments
        /// </summary>
        /// <param name="equityCurve">Equity curve series</param>
        /// <param name="orders">Orders</param>
        /// <param name="liveSeries">Equity curve series originates from LiveResult</param>
        /// <returns>Enumerable of <see cref="PointInTimePortfolio"/></returns>
        public static IEnumerable<PointInTimePortfolio> FromOrders(Series<DateTime, double> equityCurve, IEnumerable<Order> orders, bool liveSeries = false)
        {
            // Don't do anything if we have no orders or equity curve to process
            if (!orders.Any() || equityCurve.IsEmpty)
            {
                yield break;
            }

            // Chunk different deployments into separate Lists for separate processing
            var portfolioDeployments = new List<List<Order>>();

            // Orders are guaranteed to start counting from 1. This ensures that we have
            // no collision at all with the start of a deployment
            var previousOrderId = 0;
            var currentDeployment = new List<Order>();

            // Make use of reference semantics to add new deployments to the list
            portfolioDeployments.Add(currentDeployment);

            foreach (var order in orders)
            {
                // In case we have two different deployments with only a single
                // order in the deployments, <= was chosen because it covers duplicate values
                if (order.Id <= previousOrderId)
                {
                    currentDeployment = new List<Order>();
                    portfolioDeployments.Add(currentDeployment);
                }

                currentDeployment.Add(order);
                previousOrderId = order.Id;
            }

            PointInTimePortfolio prev = null;
            foreach (var deploymentOrders in portfolioDeployments)
            {
                if (deploymentOrders.Count == 0)
                {
                    Log.Trace($"PortfolioLooper.FromOrders(): Deployment contains no orders");
                    continue;
                }
                var startTime = deploymentOrders.First().Time;
                var deployment = equityCurve.Where(kvp => kvp.Key <= startTime);
                if (deployment.IsEmpty)
                {
                    Log.Trace($"PortfolioLooper.FromOrders(): Equity series is empty after filtering with upper bound: {startTime}");
                    continue;
                }

                // Skip any deployments that haven't been ran long enough to be generated in live mode
                if (liveSeries && deploymentOrders.First().Time.Date == deploymentOrders.Last().Time.Date)
                {
                    Log.Trace("PortfolioLooper.FromOrders(): Filtering deployment because it has not been deployed for more than one day");
                    continue;
                }

                // For every deployment, we want to start fresh.
                var looper = new PortfolioLooper(deployment.LastValue(), deploymentOrders);

                foreach (var portfolio in looper.ProcessOrders(deploymentOrders))
                {
                    prev = portfolio;
                    yield return portfolio;
                }
            }

            if (prev != null)
            {
                yield return new PointInTimePortfolio(prev, equityCurve.LastKey());
            }
        }

        /// <summary>
        /// Process the orders
        /// </summary>
        /// <param name="orders">orders</param>
        /// <returns>PointInTimePortfolio</returns>
        private IEnumerable<PointInTimePortfolio> ProcessOrders(IEnumerable<Order> orders)
        {
            // Portfolio.ProcessFill(...) does not filter out invalid orders. We must do so ourselves
            foreach (var order in orders)
            {
                Algorithm.SetDateTime(order.Time);

                var orderSecurity = Algorithm.Securities[order.Symbol];
                if (order.LastFillTime == null)
                {
                    Log.Trace($"Order with ID: {order.Id} has been skipped because of null LastFillTime");
                    continue;
                }

                var tick = new Tick { Quantity = order.Quantity, AskPrice = order.Price, BidPrice = order.Price, Value = order.Price, EndTime = order.LastFillTime.Value };

                // Set the market price of the security
                orderSecurity.SetMarketPrice(tick);

                // security price got updated
                Algorithm.Portfolio.InvalidateTotalPortfolioValue();

                // Check if we have a base currency (i.e. forex or crypto that requires currency conversion)
                // to ensure the proper conversion rate is set for them
                var baseCurrency = orderSecurity as IBaseCurrencySymbol;

                if (baseCurrency != null)
                {
                    // We want slices that apply to either this point in time, or the last most recent point in time
                    var updateSlices = _conversionSlices.Where(x => x.Time <= order.Time).ToList();

                    // This is put here because there can potentially be no slices
                    if (updateSlices.Count != 0)
                    {
                        var updateSlice = updateSlices.Last();

                        foreach (var quoteBar in updateSlice.QuoteBars.Values)
                        {
                            // We need to ensure that the currency pair we're converting matches our slice symbol
                            foreach (var cash in Algorithm.Portfolio.CashBook.Values.Where(x => x.ConversionRateSecurity != null && x.ConversionRateSecurity.Symbol == quoteBar.Symbol))
                            {
                                cash.Update(quoteBar);
                            }
                        }

                        // Needed to ensure that TotalPortfolioValue gets updated on the next call
                        Algorithm.Portfolio.InvalidateTotalPortfolioValue();
                    }
                }

                var orderEvent = new OrderEvent(order, order.Time, Orders.Fees.OrderFee.Zero) { FillPrice = order.Price, FillQuantity = order.Quantity };

                // Process the order
                Algorithm.Portfolio.ProcessFill(orderEvent);

                // Create portfolio statistics and return back to the user
                yield return new PointInTimePortfolio(order, Algorithm.Portfolio);
            }
        }
    }
}
