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
using System.Threading;
using Fasterflect;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Server;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Volatility;
using QuantConnect.Util.RateLimit;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Algorithm manager class executes the algorithm and generates and passes through the algorithm events.
    /// </summary>
    public class AlgorithmManager
    {
        private IAlgorithm _algorithm;
        private readonly object _lock;
        private readonly bool _liveMode;
        private bool _cancelRequested;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Publicly accessible algorithm status
        /// </summary>
        public AlgorithmStatus State => _algorithm?.Status ?? AlgorithmStatus.Running;

        /// <summary>
        /// Public access to the currently running algorithm id.
        /// </summary>
        public string AlgorithmId { get; private set; }

        /// <summary>
        /// Provides the isolator with a function for verifying that we're not spending too much time in each
        /// algorithm manager time loop
        /// </summary>
        public AlgorithmTimeLimitManager TimeLimit { get; }

        /// <summary>
        /// Quit state flag for the running algorithm. When true the user has requested the backtest stops through a Quit() method.
        /// </summary>
        /// <seealso cref="QCAlgorithm.Quit(String)"/>
        public bool QuitState => State == AlgorithmStatus.Deleted;

        /// <summary>
        /// Gets the number of data points processed per second
        /// </summary>
        public long DataPoints { get; private set; }

        /// <summary>
        /// Gets the number of data points of algorithm history provider
        /// </summary>
        public int AlgorithmHistoryDataPoints => _algorithm?.HistoryProvider?.DataPointCount ?? 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmManager"/> class
        /// </summary>
        /// <param name="liveMode">True if we're running in live mode, false for backtest mode</param>
        /// <param name="job">Provided by LEAN when creating a new algo manager. This is the job
        /// that the algo manager is about to execute. Research and other consumers can provide the
        /// default value of null</param>
        public AlgorithmManager(bool liveMode, AlgorithmNodePacket job = null)
        {
            AlgorithmId = "";
            _liveMode = liveMode;
            _lock = new object();

            // initialize the time limit manager
            TimeLimit = new AlgorithmTimeLimitManager(
                CreateTokenBucket(job?.Controls?.TrainingLimits),
                TimeSpan.FromMinutes(Config.GetDouble("algorithm-manager-time-loop-maximum", 20))
            );
        }

        /// <summary>
        /// Launch the algorithm manager to run this strategy
        /// </summary>
        /// <param name="job">Algorithm job</param>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="synchronizer">Instance which implements <see cref="ISynchronizer"/>. Used to stream the data</param>
        /// <param name="transactions">Transaction manager object</param>
        /// <param name="results">Result handler object</param>
        /// <param name="realtime">Realtime processing object</param>
        /// <param name="leanManager">ILeanManager implementation that is updated periodically with the IAlgorithm instance</param>
        /// <param name="cancellationTokenSource">Cancellation token source to monitor</param>
        /// <remarks>Modify with caution</remarks>
        public void Run(AlgorithmNodePacket job, IAlgorithm algorithm, ISynchronizer synchronizer, ITransactionHandler transactions, IResultHandler results, IRealTimeHandler realtime, ILeanManager leanManager, CancellationTokenSource cancellationTokenSource)
        {
            //Initialize:
            DataPoints = 0;
            _algorithm = algorithm;

            var token = cancellationTokenSource.Token;
            _cancellationTokenSource = cancellationTokenSource;

            var backtestMode = (job.Type == PacketType.BacktestNode);
            var methodInvokers = new Dictionary<Type, MethodInvoker>();
            var marginCallFrequency = TimeSpan.FromMinutes(5);
            var nextMarginCallTime = DateTime.MinValue;
            var nextSecurityModelScan = algorithm.UtcTime.RoundDown(Time.OneHour) + Time.OneHour;
            var time = algorithm.StartDate.Date;

            var pendingDelistings = new List<Delisting>();
            var splitWarnings = new List<Split>();

            //Initialize Properties:
            AlgorithmId = job.AlgorithmId;

            //Go through the subscription types and create invokers to trigger the event handlers for each custom type:
            foreach (var config in algorithm.SubscriptionManager.Subscriptions)
            {
                //If type is a custom feed, check for a dedicated event handler
                if (config.IsCustomData)
                {
                    //Get the matching method for this event handler - e.g. public void OnData(Quandl data) { .. }
                    var genericMethod = (algorithm.GetType()).GetMethod("OnData", new[] { config.Type });

                    //If we already have this Type-handler then don't add it to invokers again.
                    if (methodInvokers.ContainsKey(config.Type)) continue;

                    if (genericMethod != null)
                    {
                        methodInvokers.Add(config.Type, genericMethod.DelegateForCallMethod());
                    }
                }
            }

            // Schedule a daily event for sampling at midnight every night
            algorithm.Schedule.On("Daily Sampling", algorithm.Schedule.DateRules.EveryDay(),
                algorithm.Schedule.TimeRules.Midnight, () =>
                {
                    results.Sample(algorithm.UtcTime);
                });

            //Loop over the queues: get a data collection, then pass them all into relevent methods in the algorithm.
            Log.Trace($"AlgorithmManager.Run(): Begin DataStream - Start: {algorithm.StartDate} Stop: {algorithm.EndDate} Time: {algorithm.Time} Warmup: {algorithm.IsWarmingUp}");
            foreach (var timeSlice in Stream(algorithm, synchronizer, results, token))
            {
                // reset our timer on each loop
                TimeLimit.StartNewTimeStep();

                //Check this backtest is still running:
                if (_algorithm.Status != AlgorithmStatus.Running && _algorithm.RunTimeError == null)
                {
                    Log.Error($"AlgorithmManager.Run(): Algorithm state changed to {_algorithm.Status} at {timeSlice.Time.ToStringInvariant()}");
                    break;
                }

                //Execute with TimeLimit Monitor:
                if (token.IsCancellationRequested)
                {
                    Log.Error($"AlgorithmManager.Run(): CancellationRequestion at {timeSlice.Time.ToStringInvariant()}");
                    return;
                }

                // Update the ILeanManager
                leanManager.Update();

                time = timeSlice.Time;
                DataPoints += timeSlice.DataPointCount;

                if (backtestMode && algorithm.Portfolio.TotalPortfolioValue <= 0)
                {
                    var logMessage = "AlgorithmManager.Run(): Portfolio value is less than or equal to zero, stopping algorithm.";
                    Log.Error(logMessage);
                    results.SystemDebugMessage(logMessage);
                    break;
                }

                // If backtesting/warmup, we need to check if there are realtime events in the past
                // which didn't fire because at the scheduled times there was no data (i.e. markets closed)
                // and fire them with the correct date/time.
                realtime.ScanPastEvents(time);

                // will scan registered consolidators for which we've past the expected scan call.
                // In live mode we want to round down to the second, so we don't scan too far into the future:
                // The time slice might carry the data needed to complete a current consolidated bar but the
                // time slice time might be slightly ahead (a few milliseconds or even ticks) because in live we
                // use DateTime.UtcNow. So we don't want to scan past the data time so that the consolidators can
                // complete the current bar.
                var pastConsolidatorsScanTime = _liveMode ? time.RoundDown(Time.OneSecond) : time;
                algorithm.SubscriptionManager.ScanPastConsolidators(pastConsolidatorsScanTime, algorithm);

                //Set the algorithm and real time handler's time
                algorithm.SetDateTime(time);

                // the time pulse are just to advance algorithm time, lets shortcut the loop here
                if (timeSlice.IsTimePulse)
                {
                    continue;
                }

                // Update the current slice before firing scheduled events or any other task
                algorithm.SetCurrentSlice(timeSlice.Slice);

                if (timeSlice.SecurityChanges != SecurityChanges.None)
                {
                    algorithm.ProcessSecurityChanges(timeSlice.SecurityChanges);

                    leanManager.OnSecuritiesChanged(timeSlice.SecurityChanges);
                    realtime.OnSecuritiesChanged(timeSlice.SecurityChanges);
                    results.OnSecuritiesChanged(timeSlice.SecurityChanges);
                }

                //Update the securities properties: first before calling user code to avoid issues with data
                foreach (var update in timeSlice.SecuritiesUpdateData)
                {
                    var security = update.Target;

                    security.Update(update.Data, update.DataType, update.ContainsFillForwardData);

                    // Send market price updates to the TradeBuilder
                    algorithm.TradeBuilder.SetMarketPrice(security.Symbol, security.Price);
                }

                // TODO: potentially push into a scheduled event
                if (time >= nextSecurityModelScan)
                {
                    foreach (var security in algorithm.Securities.Values)
                    {
                        security.MarginInterestRateModel.ApplyMarginInterestRate(new MarginInterestRateParameters(security, time));

                        // perform check for settlement of unsettled funds
                        security.SettlementModel.Scan(new ScanSettlementModelParameters(algorithm.Portfolio, security, time));
                    }
                    nextSecurityModelScan = time.RoundDown(Time.OneHour) + Time.OneHour;
                }

                //Update the securities properties with any universe data
                if (timeSlice.UniverseData.Count > 0)
                {
                    foreach (var dataCollection in timeSlice.UniverseData.Values)
                    {
                        if (!dataCollection.ShouldCacheToSecurity()) continue;

                        foreach (var data in dataCollection.Data)
                        {
                            if (algorithm.Securities.TryGetValue(data.Symbol, out var security))
                            {
                                security.Cache.StoreData(new[] { data }, data.GetType());
                            }
                        }
                    }
                }

                // poke each cash object to update from the recent security data
                foreach (var cash in algorithm.Portfolio.CashBook.Values.Where(x => x.CurrencyConversion != null))
                {
                    cash.Update();
                }

                // security prices got updated
                algorithm.Portfolio.InvalidateTotalPortfolioValue();

                if (timeSlice.Slice.SymbolChangedEvents.Count != 0)
                {
                    try
                    {
                        algorithm.OnSymbolChangedEvents(timeSlice.Slice.SymbolChangedEvents);
                    }
                    catch (Exception err)
                    {
                        algorithm.SetRuntimeError(err, "OnSymbolChangedEvents");
                        return;
                    }

                    foreach (var symbol in timeSlice.Slice.SymbolChangedEvents.Keys)
                    {
                        // cancel all orders for the old symbol
                        foreach (var ticket in transactions.GetOpenOrderTickets(x => x.Symbol == symbol))
                        {
                            ticket.Cancel("Open order cancelled on symbol changed event");
                        }
                    }
                }

                // process fill models on the updated data before entering algorithm, applies to all non-market orders
                transactions.ProcessSynchronousEvents();

                // fire real time events after we've updated based on the new data
                realtime.SetTime(timeSlice.Time);

                // process split warnings for options
                ProcessSplitSymbols(algorithm, splitWarnings, pendingDelistings);

                //Check if the user's signalled Quit: loop over data until day changes.
                if (_algorithm.Status != AlgorithmStatus.Running && _algorithm.RunTimeError == null)
                {
                    Log.Error($"AlgorithmManager.Run(): Algorithm state changed to {_algorithm.Status} at {timeSlice.Time.ToStringInvariant()}");
                    break;
                }
                if (algorithm.RunTimeError != null)
                {
                    Log.Error($"AlgorithmManager.Run(): Stopping, encountered a runtime error at {algorithm.UtcTime} UTC.");
                    return;
                }

                // perform margin calls, in live mode we can also use realtime to emit these
                if (time >= nextMarginCallTime || (_liveMode && nextMarginCallTime > DateTime.UtcNow))
                {
                    // determine if there are possible margin call orders to be executed
                    bool issueMarginCallWarning;
                    var marginCallOrders = algorithm.Portfolio.MarginCallModel.GetMarginCallOrders(out issueMarginCallWarning);
                    var executedTicketsCount = 0;
                    if (marginCallOrders.Count != 0)
                    {
                        var executingMarginCall = false;
                        try
                        {

                            if (marginCallOrders.All(order => algorithm.Portfolio.Securities[order.Symbol].Exchange.ExchangeOpen))
                            {
                                // tell the algorithm we're about to issue the margin call
                                algorithm.OnMarginCall(marginCallOrders);

                                // execute the margin call orders
                                var executedTickets = algorithm.Portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders);
                                executedTicketsCount = executedTickets.Count;

                                foreach (var ticket in executedTickets)
                                {
                                    algorithm.Error($"{algorithm.Time.ToStringInvariant()} - Executed MarginCallOrder: {ticket.Symbol} - " +
                                        $"Quantity: {ticket.Quantity.ToStringInvariant()} @ {ticket.AverageFillPrice.ToStringInvariant()}"
                                    );
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            algorithm.SetRuntimeError(err, executingMarginCall ? "Portfolio.MarginCallModel.ExecuteMarginCall" : "OnMarginCall");
                            return;
                        }
                    }
                    // we didn't perform a margin call, but got the warning flag back, so issue the warning to the algorithm
                    if (executedTicketsCount == 0 && issueMarginCallWarning)
                    {
                        try
                        {
                            algorithm.OnMarginCallWarning();
                        }
                        catch (Exception err)
                        {
                            algorithm.SetRuntimeError(err, "OnMarginCallWarning");
                            return;
                        }
                    }

                    nextMarginCallTime = time + marginCallFrequency;
                }

                // before we call any events, let the algorithm know about universe changes
                if (timeSlice.SecurityChanges != SecurityChanges.None)
                {
                    try
                    {
                        var algorithmSecurityChanges = new SecurityChanges(timeSlice.SecurityChanges)
                        {
                            // by default for user code we want to filter out custom securities
                            FilterCustomSecurities = true,
                            // by default for user code we want to filter out internal securities
                            FilterInternalSecurities = true
                        };

                        algorithm.OnSecuritiesChanged(algorithmSecurityChanges);
                        algorithm.OnFrameworkSecuritiesChanged(algorithmSecurityChanges);
                    }
                    catch (Exception err)
                    {
                        algorithm.SetRuntimeError(err, "OnSecuritiesChanged");
                        return;
                    }
                }

                // apply dividends
                HandleDividends(timeSlice, algorithm, _liveMode);

                // apply splits
                HandleSplits(timeSlice, algorithm, _liveMode);

                //Update registered consolidators for this symbol index
                try
                {
                    if (timeSlice.ConsolidatorUpdateData.Count > 0)
                    {
                        var timeKeeper = algorithm.TimeKeeper;
                        foreach (var update in timeSlice.ConsolidatorUpdateData)
                        {
                            var localTime = timeKeeper.GetLocalTimeKeeper(update.Target.ExchangeTimeZone).LocalTime;
                            var consolidators = update.Target.Consolidators;
                            foreach (var consolidator in consolidators)
                            {
                                foreach (var dataPoint in update.Data)
                                {
                                    consolidator.Update(dataPoint);
                                }

                                // scan for time after we've pumped all the data through for this consolidator
                                consolidator.Scan(localTime);
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    algorithm.SetRuntimeError(err, "Consolidators update");
                    return;
                }

                // fire custom event handlers
                foreach (var update in timeSlice.CustomData)
                {
                    MethodInvoker methodInvoker;
                    if (!methodInvokers.TryGetValue(update.DataType, out methodInvoker))
                    {
                        continue;
                    }

                    try
                    {
                        foreach (var dataPoint in update.Data)
                        {
                            if (update.DataType.IsInstanceOfType(dataPoint))
                            {
                                methodInvoker(algorithm, dataPoint);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        algorithm.SetRuntimeError(err, "Custom Data");
                        return;
                    }
                }

                try
                {
                    if (timeSlice.Slice.Splits.Count != 0)
                    {
                        algorithm.OnSplits(timeSlice.Slice.Splits);
                    }
                }
                catch (Exception err)
                {
                    algorithm.SetRuntimeError(err, "OnSplits");
                    return;
                }

                try
                {
                    if (timeSlice.Slice.Dividends.Count != 0)
                    {
                        algorithm.OnDividends(timeSlice.Slice.Dividends);
                    }
                }
                catch (Exception err)
                {
                    algorithm.SetRuntimeError(err, "OnDividends");
                    return;
                }

                try
                {
                    if (timeSlice.Slice.Delistings.Count != 0)
                    {
                        algorithm.OnDelistings(timeSlice.Slice.Delistings);
                    }
                }
                catch (Exception err)
                {
                    algorithm.SetRuntimeError(err, "OnDelistings");
                    return;
                }

                // Only track pending delistings in non-live mode.
                if (!algorithm.LiveMode)
                {
                    // Keep this up to date even though we don't process delistings here anymore
                    foreach (var delisting in timeSlice.Slice.Delistings.Values)
                    {
                        if (delisting.Type == DelistingType.Warning)
                        {
                            // Store our delistings warnings because they are still used by ProcessSplitSymbols above
                            pendingDelistings.Add(delisting);
                        }
                        else
                        {
                            // If we have an actual delisting event, remove it from pending delistings
                            var index = pendingDelistings.FindIndex(x => x.Symbol == delisting.Symbol);
                            if (index != -1)
                            {
                                pendingDelistings.RemoveAt(index);
                            }
                        }
                    }
                }

                // run split logic after firing split events
                HandleSplitSymbols(timeSlice.Slice.Splits, splitWarnings);

                try
                {
                    if (timeSlice.Slice.HasData)
                    {
                        // EVENT HANDLER v3.0 -- all data in a single event
                        algorithm.OnData(algorithm.CurrentSlice);
                    }

                    // always turn the crank on this method to ensure universe selection models function properly on day changes w/out data
                    algorithm.OnFrameworkData(timeSlice.Slice);
                }
                catch (Exception err)
                {
                    algorithm.SetRuntimeError(err, "OnData");
                    return;
                }

                //If its the historical/paper trading models, wait until market orders have been "filled"
                // Manually trigger the event handler to prevent thread switch.
                transactions.ProcessSynchronousEvents();

                // Process any required events of the results handler such as sampling assets, equity, or stock prices.
                results.ProcessSynchronousEvents();

                // poke the algorithm at the end of each time step
                algorithm.OnEndOfTimeStep();

            } // End of ForEach feed.Bridge.GetConsumingEnumerable

            // stop timing the loops
            TimeLimit.StopEnforcingTimeLimit();

            //Stream over:: Send the final packet and fire final events:
            Log.Trace("AlgorithmManager.Run(): Firing On End Of Algorithm...");
            try
            {
                algorithm.OnEndOfAlgorithm();
            }
            catch (Exception err)
            {
                algorithm.SetRuntimeError(err, "OnEndOfAlgorithm");
                return;
            }

            // Process any required events of the results handler such as sampling assets, equity, or stock prices.
            results.ProcessSynchronousEvents(forceProcess: true);

            //Liquidate Holdings for Calculations:
            if (_algorithm.Status == AlgorithmStatus.Liquidated && _liveMode)
            {
                Log.Trace("AlgorithmManager.Run(): Liquidating algorithm holdings...");
                algorithm.Liquidate();
                results.LogMessage("Algorithm Liquidated");
                results.SendStatusUpdate(AlgorithmStatus.Liquidated);
            }

            //Manually stopped the algorithm
            if (_algorithm.Status == AlgorithmStatus.Stopped)
            {
                Log.Trace("AlgorithmManager.Run(): Stopping algorithm...");
                results.LogMessage("Algorithm Stopped");
                results.SendStatusUpdate(AlgorithmStatus.Stopped);
            }

            //Backtest deleted.
            if (_algorithm.Status == AlgorithmStatus.Deleted)
            {
                Log.Trace("AlgorithmManager.Run(): Deleting algorithm...");
                results.DebugMessage("Algorithm Id:(" + job.AlgorithmId + ") Deleted by request.");
                results.SendStatusUpdate(AlgorithmStatus.Deleted);
            }

            //Algorithm finished, send regardless of commands:
            results.SendStatusUpdate(AlgorithmStatus.Completed);
            SetStatus(AlgorithmStatus.Completed);

            //Take final samples:
            results.Sample(time);

        } // End of Run();

        /// <summary>
        /// Set the quit state.
        /// </summary>
        public void SetStatus(AlgorithmStatus state)
        {
            lock (_lock)
            {
                //We don't want anyone else to set our internal state to "Running".
                //This is controlled by the algorithm private variable only.
                //Algorithm could be null after it's initialized and they call Run on us
                if (state != AlgorithmStatus.Running && _algorithm != null)
                {
                    _algorithm.SetStatus(state);
                }

                if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested && !_cancelRequested)
                {
                    if (state == AlgorithmStatus.Deleted)
                    {
                        _cancelRequested = true;
                        // if the algorithm was deleted, let's give the algorithm a few seconds to shutdown and cancel it out
                        _cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                    }
                    else if (state == AlgorithmStatus.Stopped)
                    {
                        _cancelRequested = true;
                        // if the algorithm was stopped, let's give the algorithm a few seconds to shutdown and cancel it out
                        _cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));
                    }
                }
            }
        }

        private IEnumerable<TimeSlice> Stream(IAlgorithm algorithm, ISynchronizer synchronizer, IResultHandler results, CancellationToken cancellationToken)
        {
            var nextWarmupStatusTime = DateTime.MinValue;
            var warmingUp = algorithm.IsWarmingUp;
            var warmingUpPercent = 0;
            if (warmingUp)
            {
                nextWarmupStatusTime = DateTime.UtcNow.AddSeconds(1);
                algorithm.Debug("Algorithm starting warm up...");
                results.SendStatusUpdate(AlgorithmStatus.History, $"{warmingUpPercent}");
            }
            else
            {
                results.SendStatusUpdate(AlgorithmStatus.Running);
                // let's be polite, and call warmup finished even though there was no warmup period and avoid algorithms having to handle it instead.
                // we trigger this callback here and not internally in the algorithm so that we can go through python if required
                algorithm.OnWarmupFinished();
            }

            // bellow we compare with slice.Time which is in UTC
            var startTimeTicks = algorithm.UtcTime.Ticks;
            var warmupEndTicks = algorithm.StartDate.ConvertToUtc(algorithm.TimeZone).Ticks;

            // fulfilling history requirements of volatility models in live mode
            if (algorithm.LiveMode)
            {
                warmupEndTicks = DateTime.UtcNow.Ticks;
                ProcessVolatilityHistoryRequirements(algorithm, _liveMode);
            }

            foreach (var timeSlice in synchronizer.StreamData(cancellationToken))
            {
                if (algorithm.IsWarmingUp)
                {
                    var now = DateTime.UtcNow;
                    if (now > nextWarmupStatusTime)
                    {
                        // send some status to the user letting them know we're done history, but still warming up,
                        // catching up to real time data
                        nextWarmupStatusTime = now.AddSeconds(2);
                        var newPercent = (int)(100 * (timeSlice.Time.Ticks - startTimeTicks) / (double)(warmupEndTicks - startTimeTicks));
                        // if there isn't any progress don't send the same update many times
                        if (newPercent != warmingUpPercent)
                        {
                            warmingUpPercent = newPercent;
                            algorithm.Debug($"Processing algorithm warm-up request {warmingUpPercent}%...");
                            results.SendStatusUpdate(AlgorithmStatus.History, $"{warmingUpPercent}");
                        }
                    }
                }
                else if (warmingUp)
                {
                    // warmup finished, send an update
                    warmingUp = false;
                    // we trigger this callback here and not internally in the algorithm so that we can go through python if required
                    algorithm.OnWarmupFinished();
                    algorithm.Debug("Algorithm finished warming up.");
                    results.SendStatusUpdate(AlgorithmStatus.Running, "100");
                }
                yield return timeSlice;
            }
        }

        /// <summary>
        /// Helper method used to process securities volatility history requirements
        /// </summary>
        /// <remarks>Implemented as static to facilitate testing</remarks>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="liveMode">Whether the algorithm is in live mode</param>
        public static void ProcessVolatilityHistoryRequirements(IAlgorithm algorithm, bool liveMode)
        {
            Log.Trace("ProcessVolatilityHistoryRequirements(): Updating volatility models with historical data...");

            foreach (var security in algorithm.Securities.Values)
            {
                security.VolatilityModel.WarmUp(algorithm.HistoryProvider, algorithm.SubscriptionManager, security, algorithm.UtcTime,
                    algorithm.TimeZone, liveMode);
            }

            Log.Trace("ProcessVolatilityHistoryRequirements(): finished.");
        }

        /// <summary>
        /// Helper method to apply a split to an algorithm instance
        /// </summary>
        public static void HandleSplits(TimeSlice timeSlice, IAlgorithm algorithm, bool liveMode)
        {
            foreach (var split in timeSlice.Slice.Splits.Values)
            {
                try
                {
                    // only process split occurred events (ignore warnings)
                    if (split.Type != SplitType.SplitOccurred)
                    {
                        continue;
                    }

                    if (liveMode && algorithm.IsWarmingUp)
                    {
                        // skip past split during live warmup, the algorithms position already reflects them
                        Log.Trace($"AlgorithmManager.Run(): {algorithm.Time}: Skip Split during live warmup: {split}");
                        continue;
                    }

                    if (Log.DebuggingEnabled)
                    {
                        Log.Debug($"AlgorithmManager.Run(): {algorithm.Time}: Applying Split for {split.Symbol}");
                    }

                    Security security = null;
                    if (algorithm.Securities.TryGetValue(split.Symbol, out security) && liveMode)
                    {
                        Log.Trace($"AlgorithmManager.Run(): {algorithm.Time}: Pre-Split for {split}. Security Price: {security.Price} Holdings: {security.Holdings.Quantity}");
                    }

                    var mode = algorithm.SubscriptionManager.SubscriptionDataConfigService
                        .GetSubscriptionDataConfigs(split.Symbol)
                        .DataNormalizationMode();

                    // apply the split event to the portfolio
                    algorithm.Portfolio.ApplySplit(split, security, liveMode, mode);

                    // apply the split event to the trade builder
                    algorithm.TradeBuilder.ApplySplit(split, liveMode, mode);

                    // apply the split event to the security volatility model
                    ApplySplitOrDividendToVolatilityModel(algorithm, security, liveMode, mode);

                    if (liveMode && security != null)
                    {
                        Log.Trace($"AlgorithmManager.Run(): {algorithm.Time}: Post-Split for {split}. Security Price: {security.Price} Holdings: {security.Holdings.Quantity}");
                    }

                    // apply the split to open orders as well in raw mode, all other modes are split adjusted
                    if (liveMode || mode == DataNormalizationMode.Raw)
                    {
                        // in live mode we always want to have our order match the order at the brokerage, so apply the split to the orders
                        var openOrders = algorithm.Transactions.GetOpenOrderTickets(ticket => ticket.Symbol == split.Symbol);
                        algorithm.BrokerageModel.ApplySplit(openOrders.ToList(), split);
                    }
                }
                catch (Exception err)
                {
                    algorithm.SetRuntimeError(err, "Split event");
                    return;
                }
            }
        }

        /// <summary>
        /// Helper method to apply a dividend to an algorithm instance
        /// </summary>
        public static void HandleDividends(TimeSlice timeSlice, IAlgorithm algorithm, bool liveMode)
        {
            foreach (var dividend in timeSlice.Slice.Dividends.Values)
            {
                if (liveMode && algorithm.IsWarmingUp)
                {
                    // skip past dividends during live warmup, the algorithms position already reflects them
                    Log.Trace($"AlgorithmManager.Run(): {algorithm.Time}: Skip Dividend during live warmup: {dividend}");
                    continue;
                }

                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"AlgorithmManager.Run(): {algorithm.Time}: Applying Dividend: {dividend}");
                }

                Security security = null;
                if (algorithm.Securities.TryGetValue(dividend.Symbol, out security) && liveMode)
                {
                    Log.Trace($"AlgorithmManager.Run(): {algorithm.Time}: Pre-Dividend: {dividend}. " +
                        $"Security Holdings: {security.Holdings.Quantity} Account Currency Holdings: " +
                        $"{algorithm.Portfolio.CashBook[algorithm.AccountCurrency].Amount}");
                }

                var mode = algorithm.SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(dividend.Symbol)
                    .DataNormalizationMode();

                // apply the dividend event to the portfolio
                algorithm.Portfolio.ApplyDividend(dividend, liveMode, mode);

                // apply the dividend event to the security volatility model
                ApplySplitOrDividendToVolatilityModel(algorithm, security, liveMode, mode);

                if (liveMode && security != null)
                {
                    Log.Trace($"AlgorithmManager.Run(): {algorithm.Time}: Post-Dividend: {dividend}. Security " +
                        $"Holdings: {security.Holdings.Quantity} Account Currency Holdings: " +
                        $"{algorithm.Portfolio.CashBook[algorithm.AccountCurrency].Amount}");
                }
            }
        }

        /// <summary>
        /// Keeps track of split warnings so we can later liquidate option contracts
        /// </summary>
        private void HandleSplitSymbols(Splits newSplits, List<Split> splitWarnings)
        {
            foreach (var split in newSplits.Values)
            {
                if (split.Type != SplitType.Warning)
                {
                    if (Log.DebuggingEnabled)
                    {
                        Log.Debug($"AlgorithmManager.HandleSplitSymbols(): {_algorithm.Time} - Security split occurred: Split Factor: {split} Reference Price: {split.ReferencePrice}");
                    }
                    continue;
                }

                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"AlgorithmManager.HandleSplitSymbols(): {_algorithm.Time} - Security split warning: {split}");
                }

                if (!splitWarnings.Any(x => x.Symbol == split.Symbol && x.Type == SplitType.Warning))
                {
                    splitWarnings.Add(split);
                }
            }
        }

        /// <summary>
        /// Liquidate option contact holdings who's underlying security has split
        /// </summary>
        private void ProcessSplitSymbols(IAlgorithm algorithm, List<Split> splitWarnings, List<Delisting> pendingDelistings)
        {
            // NOTE: This method assumes option contracts have the same core trading hours as their underlying contract
            //       This is a small performance optimization to prevent scanning every contract on every time step,
            //       instead we scan just the underlyings, thereby reducing the time footprint of this methods by a factor
            //       of N, the number of derivative subscriptions
            for (int i = splitWarnings.Count - 1; i >= 0; i--)
            {
                var split = splitWarnings[i];
                var security = algorithm.Securities[split.Symbol];

                if (!security.IsTradable
                    && !algorithm.UniverseManager.ActiveSecurities.Keys.Contains(split.Symbol))
                {
                    Log.Debug($"AlgorithmManager.ProcessSplitSymbols(): {_algorithm.Time} - Removing split warning for {security.Symbol}");

                    // remove the warning from out list
                    splitWarnings.RemoveAt(i);
                    // Since we are storing the split warnings for a loop
                    // we need to check if the security was removed.
                    // When removed, it will be marked as non tradable but just in case
                    // we expect it not to be an active security either
                    continue;
                }

                var nextMarketClose = security.Exchange.Hours.GetNextMarketClose(security.LocalTime, false);

                // determine the latest possible time we can submit a MOC order
                var configs = algorithm.SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(security.Symbol);

                if (configs.Count == 0)
                {
                    // should never happen at this point, if it does let's give some extra info
                    throw new Exception(
                        $"AlgorithmManager.ProcessSplitSymbols(): {_algorithm.Time} - No subscriptions found for {security.Symbol}" +
                        $", IsTradable: {security.IsTradable}" +
                        $", Active: {algorithm.UniverseManager.ActiveSecurities.Keys.Contains(split.Symbol)}");
                }

                var latestMarketOnCloseTimeRoundedDownByResolution = nextMarketClose.Subtract(MarketOnCloseOrder.SubmissionTimeBuffer)
                    .RoundDownInTimeZone(configs.GetHighestResolution().ToTimeSpan(), security.Exchange.TimeZone, configs.First().DataTimeZone);

                // we don't need to do anyhing until the market closes
                if (security.LocalTime < latestMarketOnCloseTimeRoundedDownByResolution) continue;

                // fetch all option derivatives of the underlying with holdings (excluding the canonical security)
                var derivatives = algorithm.Securities.Values.Where(potentialDerivate =>
                    potentialDerivate.Symbol.SecurityType.IsOption() &&
                    potentialDerivate.Symbol.Underlying == security.Symbol &&
                    !potentialDerivate.Symbol.Underlying.IsCanonical() &&
                    potentialDerivate.HoldStock
                );

                foreach (var derivative in derivatives)
                {
                    var optionContractSymbol = derivative.Symbol;
                    var optionContractSecurity = (Option)derivative;

                    if (pendingDelistings.Any(x => x.Symbol == optionContractSymbol
                        && x.Time.Date == optionContractSecurity.LocalTime.Date))
                    {
                        // if the option is going to be delisted today we skip sending the market on close order
                        continue;
                    }

                    // close any open orders
                    algorithm.Transactions.CancelOpenOrders(optionContractSymbol, "Canceled due to impending split. Separate MarketOnClose order submitted to liquidate position.");

                    var request = new SubmitOrderRequest(OrderType.MarketOnClose, optionContractSecurity.Type, optionContractSymbol,
                        -optionContractSecurity.Holdings.Quantity, 0, 0, algorithm.UtcTime,
                        "Liquidated due to impending split. Option splits are not currently supported."
                    );

                    // send MOC order to liquidate option contract holdings
                    algorithm.Transactions.AddOrder(request);

                    // mark option contract as not tradable
                    optionContractSecurity.IsTradable = false;

                    algorithm.Debug($"MarketOnClose order submitted for option contract '{optionContractSymbol}' due to impending {split.Symbol.Value} split event. "
                        + "Option splits are not currently supported.");
                }

                // remove the warning from out list
                splitWarnings.RemoveAt(i);
            }
        }

        /// <summary>
        /// Warms up the security's volatility model in the case of a split or dividend to avoid discontinuities when data is raw or in live mode
        /// </summary>
        private static void ApplySplitOrDividendToVolatilityModel(IAlgorithm algorithm, Security security, bool liveMode,
            DataNormalizationMode dataNormalizationMode)
        {
            if (security.Type == SecurityType.Equity && (liveMode || dataNormalizationMode == DataNormalizationMode.Raw))
            {
                security?.VolatilityModel.WarmUp(algorithm.HistoryProvider, algorithm.SubscriptionManager, security, algorithm.UtcTime,
                    algorithm.TimeZone, liveMode, dataNormalizationMode);
            }
        }

        /// <summary>
        /// Constructs the correct <see cref="ITokenBucket"/> instance per the provided controls.
        /// The provided controls will be null when
        /// </summary>
        private static ITokenBucket CreateTokenBucket(LeakyBucketControlParameters controls)
        {
            if (controls == null)
            {
                // this will only be null when the AlgorithmManager is being initialized outside of LEAN
                // for example, in unit tests that don't provide a job package as well as from Research
                // in each of the above cases, it seems best to not enforce the leaky bucket restrictions
                return TokenBucket.Null;
            }

            Log.Trace("AlgorithmManager.CreateTokenBucket(): Initializing LeakyBucket: " +
                $"Capacity: {controls.Capacity} " +
                $"RefillAmount: {controls.RefillAmount} " +
                $"TimeInterval: {controls.TimeIntervalMinutes}"
            );

            // these parameters view 'minutes' as the resource being rate limited. the capacity is the total
            // number of minutes available for burst operations and after controls.TimeIntervalMinutes time
            // has passed, we'll add controls.RefillAmount to the 'minutes' available, maxing at controls.Capacity
            return new LeakyBucket(
                controls.Capacity,
                controls.RefillAmount,
                TimeSpan.FromMinutes(controls.TimeIntervalMinutes)
            );
        }
    }
}
