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
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Algorithm manager class executes the algorithm and generates and passes through the algorithm events.
    /// </summary>
    public class AlgorithmManager
    {
        private DateTime _previousTime;
        private AlgorithmStatus _algorithmState = AlgorithmStatus.Running;
        private readonly object _lock = new object();
        private string _algorithmId = "";
        private DateTime _currentTimeStepTime;
        private readonly TimeSpan _timeLoopMaximum = TimeSpan.FromMinutes(Config.GetDouble("algorithm-manager-time-loop-maximum", 10));
        private long _dataPointCount;

        /// <summary>
        /// Publicly accessible algorithm status
        /// </summary>
        public AlgorithmStatus State
        {
            get
            {
                return _algorithmState;
            }
        }

        /// <summary>
        /// Public access to the currently running algorithm id.
        /// </summary>
        public string AlgorithmId
        {
            get
            {
                return _algorithmId;
            }
        }

        /// <summary>
        /// Gets the amount of time spent on the current time step
        /// </summary>
        public TimeSpan CurrentTimeStepElapsed
        {
            get { return _currentTimeStepTime == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - _currentTimeStepTime; }
        }

        /// <summary>
        /// Gets a function used with the Isolator for verifying we're not spending too much time in each
        /// algo manager timer loop
        /// </summary>
        public readonly Func<string> TimeLoopWithinLimits;

        private readonly bool _liveMode;

        /// <summary>
        /// Quit state flag for the running algorithm. When true the user has requested the backtest stops through a Quit() method.
        /// </summary>
        /// <seealso cref="QCAlgorithm.Quit"/>
        public bool QuitState
        {
            get
            {
                return _algorithmState == AlgorithmStatus.Deleted;
            }
        }

        /// <summary>
        /// Gets the number of data points processed per second
        /// </summary>
        public long DataPoints
        {
            get
            {
                return _dataPointCount;
            }
        }

        public AlgorithmManager(bool liveMode)
        {
            TimeLoopWithinLimits = () =>
            {
                if (CurrentTimeStepElapsed > _timeLoopMaximum)
                {
                    return "Algorithm took longer than 10 minutes on a single time loop.";
                }
                return null;
            };
            _liveMode = liveMode;
        }

        /// <summary>
        /// Launch the algorithm manager to run this strategy
        /// </summary>
        /// <param name="job">Algorithm job</param>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="feed">Datafeed object</param>
        /// <param name="transactions">Transaction manager object</param>
        /// <param name="results">Result handler object</param>
        /// <param name="realtime">Realtime processing object</param>
        /// <param name="token">Cancellation token</param>
        /// <remarks>Modify with caution</remarks>
        public void Run(AlgorithmNodePacket job, IAlgorithm algorithm, IDataFeed feed, ITransactionHandler transactions, IResultHandler results, IRealTimeHandler realtime, CancellationToken token) 
        {
            //Initialize:
            _dataPointCount = 0;
            var portfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            var backtestMode = (job.Type == PacketType.BacktestNode);
            var methodInvokers = new Dictionary<Type, MethodInvoker>();
            var marginCallFrequency = TimeSpan.FromMinutes(5);
            var nextMarginCallTime = DateTime.MinValue;
            var delistingTickets = new List<OrderTicket>();

            //Initialize Properties:
            _algorithmId = job.AlgorithmId;
            _algorithmState = AlgorithmStatus.Running;
            _previousTime = algorithm.StartDate.Date;

            //Create the method accessors to push generic types into algorithm: Find all OnData events:

            // Algorithm 2.0 data accessors
            var hasOnDataTradeBars = AddMethodInvoker<TradeBars>(algorithm, methodInvokers);
            var hasOnDataTicks = AddMethodInvoker<Ticks>(algorithm, methodInvokers);

            // dividend and split events
            var hasOnDataDividends = AddMethodInvoker<Dividends>(algorithm, methodInvokers);
            var hasOnDataSplits = AddMethodInvoker<Splits>(algorithm, methodInvokers);
            var hasOnDataDelistings = AddMethodInvoker<Delistings>(algorithm, methodInvokers);
            var hasOnDataSymbolChangedEvents = AddMethodInvoker<SymbolChangedEvents>(algorithm, methodInvokers);

            // Algorithm 3.0 data accessors
            var hasOnDataSlice = algorithm.GetType().GetMethods()
                .Where(x => x.Name == "OnData" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof (Slice))
                .FirstOrDefault(x => x.DeclaringType == algorithm.GetType()) != null;

            //Go through the subscription types and create invokers to trigger the event handlers for each custom type:
            foreach (var config in feed.Subscriptions.Select(x => x.Configuration)) 
            {
                //If type is a tradebar, combine tradebars and ticks into unified array:
                if (config.Type.Name != "TradeBar" && config.Type.Name != "Tick" && !config.IsInternalFeed) 
                {
                    //Get the matching method for this event handler - e.g. public void OnData(Quandl data) { .. }
                    var genericMethod = (algorithm.GetType()).GetMethod("OnData", new[] { config.Type });

                    //If we already have this Type-handler then don't add it to invokers again.
                    if (methodInvokers.ContainsKey(config.Type)) continue;

                    //If we couldnt find the event handler, let the user know we can't fire that event.
                    if (genericMethod == null && !hasOnDataSlice)
                    {
                        algorithm.RunTimeError = new Exception("Data event handler not found, please create a function matching this template: public void OnData(" + config.Type.Name + " data) {  }");
                        _algorithmState = AlgorithmStatus.RuntimeError;
                        return;
                    }
                    if (genericMethod != null)
                    {
                        methodInvokers.Add(config.Type, genericMethod.DelegateForCallMethod());
                    }
                }
            }

            //Loop over the queues: get a data collection, then pass them all into relevent methods in the algorithm.
            Log.Trace("AlgorithmManager.Run(): Begin DataStream - Start: " + algorithm.StartDate + " Stop: " + algorithm.EndDate);
            foreach (var timeSlice in Stream(job, algorithm, feed, results, token))
            {
                // reset our timer on each loop
                _currentTimeStepTime = DateTime.UtcNow;

                //Check this backtest is still running:
                if (_algorithmState != AlgorithmStatus.Running)
                {
                    Log.Error(string.Format("AlgorithmManager.Run(): Algorthm state changed to {0} at {1}", _algorithmState, timeSlice.Time));
                    break;
                }

                //Execute with TimeLimit Monitor:
                if (token.IsCancellationRequested)
                {
                    Log.Error("AlgorithmManager.Run(): CancellationRequestion at " + timeSlice.Time);
                    return;
                }

                var time = timeSlice.Time;
                _dataPointCount += timeSlice.DataPointCount;

                //If we're in backtest mode we need to capture the daily performance. We do this here directly
                //before updating the algorithm state with the new data from this time step, otherwise we'll
                //produce incorrect samples (they'll take into account this time step's new price values)
                if (backtestMode)
                {
                    //On day-change sample equity and daily performance for statistics calculations
                    if (_previousTime.Date != time.Date)
                    {
                        SampleBenchmark(algorithm, results, _previousTime.Date);

                        //Sample the portfolio value over time for chart.
                        results.SampleEquity(_previousTime, Math.Round(algorithm.Portfolio.TotalPortfolioValue, 4));

                        //Check for divide by zero
                        if (portfolioValue == 0m)
                        {
                            results.SamplePerformance(_previousTime.Date, 0);
                        }
                        else
                        {
                            results.SamplePerformance(_previousTime.Date, Math.Round((algorithm.Portfolio.TotalPortfolioValue - portfolioValue) * 100 / portfolioValue, 10));
                        }
                        portfolioValue = algorithm.Portfolio.TotalPortfolioValue;
                    }
                }
                else
                {
                    // live mode continously sample the benchmark
                    SampleBenchmark(algorithm, results, time);
                }

                //Update algorithm state after capturing performance from previous day

                //Set the algorithm and real time handler's time
                algorithm.SetDateTime(time);

                if (timeSlice.Slice.SymbolChangedEvents.Count != 0)
                {
                    if (hasOnDataSymbolChangedEvents)
                    {
                        methodInvokers[typeof (SymbolChangedEvents)](algorithm, timeSlice.Slice.SymbolChangedEvents);
                    }
                    foreach (var symbol in timeSlice.Slice.SymbolChangedEvents.Keys)
                    {
                        // cancel all orders for the old symbol
                        foreach (var ticket in transactions.GetOrderTickets(x => x.Status.IsOpen() && x.Symbol == symbol))
                        {
                            ticket.Cancel("Open order cancelled on symbol changed event");
                        }
                    }
                }

                if (timeSlice.SecurityChanges != SecurityChanges.None)
                {
                    foreach (var security in timeSlice.SecurityChanges.AddedSecurities)
                    {
                        if (!algorithm.Securities.ContainsKey(security.Symbol))
                        {
                            // add the new security
                            algorithm.Securities.Add(security);
                        }
                    }
                }

                //On each time step push the real time prices to the cashbook so we can have updated conversion rates
                foreach (var kvp in timeSlice.CashBookUpdateData)
                {
                    kvp.Key.Update(kvp.Value);
                }

                //Update the securities properties: first before calling user code to avoid issues with data
                foreach (var kvp in timeSlice.SecuritiesUpdateData)
                {
                    kvp.Key.SetMarketPrice(kvp.Value);

                    // Send market price updates to the TradeBuilder
                    if (kvp.Value != null)
                    {
                        algorithm.TradeBuilder.SetMarketPrice(kvp.Key.Symbol, kvp.Value.Price);
                    }
                }

                // fire real time events after we've updated based on the new data
                realtime.SetTime(timeSlice.Time);

                // process fill models on the updated data before entering algorithm, applies to all non-market orders
                transactions.ProcessSynchronousEvents();

                if (delistingTickets.Count != 0)
                {
                    for (int i = 0; i < delistingTickets.Count; i++)
                    {
                        var ticket = delistingTickets[i];
                        if (ticket.Status == OrderStatus.Filled)
                        {
                            algorithm.Securities.Remove(ticket.Symbol);
                            delistingTickets.RemoveAt(i--);
                            Log.Trace("AlgorithmManager.Run(): Delisted Security removed: " + ticket.Symbol.Permtick);
                        }
                    }
                }

                //Check if the user's signalled Quit: loop over data until day changes.
                if (algorithm.GetQuit())
                {
                    _algorithmState = AlgorithmStatus.Quit;
                    Log.Trace("AlgorithmManager.Run(): Algorithm quit requested.");
                    break;
                }
                if (algorithm.RunTimeError != null)
                {
                    _algorithmState = AlgorithmStatus.RuntimeError;
                    Log.Trace(string.Format("AlgorithmManager.Run(): Algorithm encountered a runtime error at {0}. Error: {1}", timeSlice.Time, algorithm.RunTimeError));
                    break;
                }

                // perform margin calls, in live mode we can also use realtime to emit these
                if (time >= nextMarginCallTime || (_liveMode && nextMarginCallTime > DateTime.Now))
                {
                    // determine if there are possible margin call orders to be executed
                    bool issueMarginCallWarning;
                    var marginCallOrders = algorithm.Portfolio.ScanForMarginCall(out issueMarginCallWarning);
                    if (marginCallOrders.Count != 0)
                    {
                        var executingMarginCall = false;
                        try
                        {
                            // tell the algorithm we're about to issue the margin call
                            algorithm.OnMarginCall(marginCallOrders);

                            executingMarginCall = true;

                            // execute the margin call orders
                            var executedTickets = algorithm.Portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders);
                            foreach (var ticket in executedTickets)
                            {
                                algorithm.Error(string.Format("{0} - Executed MarginCallOrder: {1} - Quantity: {2} @ {3}", algorithm.Time, ticket.Symbol, ticket.Quantity, ticket.AverageFillPrice));
                            }
                        }
                        catch (Exception err)
                        {
                            algorithm.RunTimeError = err;
                            _algorithmState = AlgorithmStatus.RuntimeError;
                            var locator = executingMarginCall ? "Portfolio.MarginCallModel.ExecuteMarginCall" : "OnMarginCall";
                            Log.Error(string.Format("AlgorithmManager.Run(): RuntimeError: {0}: ", locator) + err.Message + " STACK >>> " + err.StackTrace);
                            return;
                        }
                    }
                    // we didn't perform a margin call, but got the warning flag back, so issue the warning to the algorithm
                    else if (issueMarginCallWarning)
                    {
                        try
                        {
                            algorithm.OnMarginCallWarning();
                        }
                        catch (Exception err)
                        {
                            algorithm.RunTimeError = err;
                            _algorithmState = AlgorithmStatus.RuntimeError;
                            Log.Error("AlgorithmManager.Run(): RuntimeError: OnMarginCallWarning: " + err.Message + " STACK >>> " + err.StackTrace);
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
                        algorithm.OnSecuritiesChanged(timeSlice.SecurityChanges);
                    }
                    catch (Exception err)
                    {
                        algorithm.RunTimeError = err;
                        _algorithmState = AlgorithmStatus.RuntimeError;
                        Log.Error("AlgorithmManager.Run(): RuntimeError: OnSecuritiesChanged event: " + err.Message);
                        return;
                    }
                }

                // apply dividends
                foreach (var dividend in timeSlice.Slice.Dividends.Values)
                {
                    Log.Trace("AlgorithmManager.Run(): Applying Dividend for " + dividend.Symbol, true);
                    algorithm.Portfolio.ApplyDividend(dividend);
                }

                // apply splits
                foreach (var split in timeSlice.Slice.Splits.Values)
                {
                    try
                    {
                        Log.Trace("AlgorithmManager.Run(): Applying Split for " + split.Symbol, true);
                        algorithm.Portfolio.ApplySplit(split);
                        // apply the split to open orders as well in raw mode, all other modes are split adjusted
                        if (_liveMode || algorithm.Securities[split.Symbol].SubscriptionDataConfig.DataNormalizationMode == DataNormalizationMode.Raw)
                        {
                            // in live mode we always want to have our order match the order at the brokerage, so apply the split to the orders
                            var openOrders = transactions.GetOrderTickets(ticket => ticket.Status.IsOpen() && ticket.Symbol == split.Symbol);
                            algorithm.BrokerageModel.ApplySplit(openOrders.ToList(), split);
                        }
                    }
                    catch (Exception err)
                    {
                        algorithm.RunTimeError = err;
                        _algorithmState = AlgorithmStatus.RuntimeError;
                        Log.Error("AlgorithmManager.Run(): RuntimeError: Split event: " + err.Message);
                        return;
                    }
                }

                //Update registered consolidators for this symbol index
                try
                {
                    foreach (var kvp in timeSlice.ConsolidatorUpdateData)
                    {
                        var consolidators = kvp.Key.Consolidators;
                        foreach (var dataPoint in kvp.Value)
                        {
                            foreach (var consolidator in consolidators)
                            {
                                consolidator.Update(dataPoint);
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    algorithm.RunTimeError = err;
                    _algorithmState = AlgorithmStatus.RuntimeError;
                    Log.Error("AlgorithmManager.Run(): RuntimeError: Consolidators update: " + err.Message);
                    return;
                }

                // fire custom event handlers
                foreach (var kvp in timeSlice.CustomData)
                {
                    MethodInvoker methodInvoker;
                    if (!methodInvokers.TryGetValue(kvp.Key.SubscriptionDataConfig.Type, out methodInvoker))
                    {
                        continue;
                    }

                    try
                    {
                        foreach (var dataPoint in kvp.Value)
                        {
                            methodInvoker(algorithm, dataPoint);
                        }
                    }
                    catch (Exception err)
                    {
                        algorithm.RunTimeError = err;
                        _algorithmState = AlgorithmStatus.RuntimeError;
                        Log.Error("AlgorithmManager.Run(): RuntimeError: Custom Data: " + err.Message + " STACK >>> " + err.StackTrace);
                        return;
                    }
                }

                try
                {
                    // fire off the dividend and split events before pricing events
                    if (hasOnDataDividends && timeSlice.Slice.Dividends.Count != 0)
                    {
                        methodInvokers[typeof(Dividends)](algorithm, timeSlice.Slice.Dividends);
                    }
                    if (hasOnDataSplits && timeSlice.Slice.Splits.Count != 0)
                    {
                        methodInvokers[typeof(Splits)](algorithm, timeSlice.Slice.Splits);
                    }
                    if (hasOnDataDelistings && timeSlice.Slice.Delistings.Count != 0)
                    {
                        methodInvokers[typeof(Delistings)](algorithm, timeSlice.Slice.Delistings);
                    }
                }
                catch (Exception err)
                {
                    algorithm.RunTimeError = err;
                    _algorithmState = AlgorithmStatus.RuntimeError;
                    Log.Error("AlgorithmManager.Run(): RuntimeError: Dividends/Splits/Delistings: " + err.Message + " STACK >>> " + err.StackTrace);
                    return;
                }

                // run the delisting logic after firing delisting events
                HandleDelistedSymbols(algorithm, timeSlice.Slice.Delistings, delistingTickets);

                //After we've fired all other events in this second, fire the pricing events:
                try
                {
                    if (hasOnDataTradeBars && timeSlice.Slice.Bars.Count > 0) methodInvokers[typeof(TradeBars)](algorithm, timeSlice.Slice.Bars);
                    if (hasOnDataTicks && timeSlice.Slice.Ticks.Count > 0) methodInvokers[typeof(Ticks)](algorithm, timeSlice.Slice.Ticks);
                }
                catch (Exception err)
                {
                    algorithm.RunTimeError = err;
                    _algorithmState = AlgorithmStatus.RuntimeError;
                    Log.Error("AlgorithmManager.Run(): RuntimeError: New Style Mode: " + err.Message + " STACK >>> " + err.StackTrace);
                    return;
                }

                try
                {
                    if (timeSlice.Slice.Count != 0)
                    {
                        // EVENT HANDLER v3.0 -- all data in a single event
                        algorithm.OnData(timeSlice.Slice);
                    }
                }
                catch (Exception err)
                {
                    algorithm.RunTimeError = err;
                    _algorithmState = AlgorithmStatus.RuntimeError;
                    Log.Error("AlgorithmManager.Run(): RuntimeError: Slice: " + err.Message + " STACK >>> " + err.StackTrace);
                    return;
                }

                //If its the historical/paper trading models, wait until market orders have been "filled"
                // Manually trigger the event handler to prevent thread switch.
                transactions.ProcessSynchronousEvents();

                //Save the previous time for the sample calculations
                _previousTime = time;

                // Process any required events of the results handler such as sampling assets, equity, or stock prices.
                results.ProcessSynchronousEvents();
            } // End of ForEach feed.Bridge.GetConsumingEnumerable

            // stop timing the loops
            _currentTimeStepTime = DateTime.MinValue;

            //Stream over:: Send the final packet and fire final events:
            Log.Trace("AlgorithmManager.Run(): Firing On End Of Algorithm...");
            try
            {
                algorithm.OnEndOfAlgorithm();
            }
            catch (Exception err)
            {
                _algorithmState = AlgorithmStatus.RuntimeError;
                algorithm.RunTimeError = new Exception("Error running OnEndOfAlgorithm(): " + err.Message, err.InnerException);
                Log.Error("AlgorithmManager.OnEndOfAlgorithm(): " + err.Message + " STACK >>> " + err.StackTrace);
                return;
            }

            // Process any required events of the results handler such as sampling assets, equity, or stock prices.
            results.ProcessSynchronousEvents(forceProcess: true);

            //Liquidate Holdings for Calculations:
            if (_algorithmState == AlgorithmStatus.Liquidated && _liveMode)
            {
                Log.Trace("AlgorithmManager.Run(): Liquidating algorithm holdings...");
                algorithm.Liquidate();
                results.LogMessage("Algorithm Liquidated");
                results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Liquidated);
            }

            //Manually stopped the algorithm
            if (_algorithmState == AlgorithmStatus.Stopped)
            {
                Log.Trace("AlgorithmManager.Run(): Stopping algorithm...");
                results.LogMessage("Algorithm Stopped");
                results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Stopped);
            }

            //Backtest deleted.
            if (_algorithmState == AlgorithmStatus.Deleted)
            {
                Log.Trace("AlgorithmManager.Run(): Deleting algorithm...");
                results.DebugMessage("Algorithm Id:(" + job.AlgorithmId + ") Deleted by request.");
                results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Deleted);
            }

            //Algorithm finished, send regardless of commands:
            results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Completed);

            //Take final samples:
            results.SampleRange(algorithm.GetChartUpdates());
            results.SampleEquity(_previousTime, Math.Round(algorithm.Portfolio.TotalPortfolioValue, 4));
            SampleBenchmark(algorithm, results, _previousTime);
            results.SamplePerformance(_previousTime, Math.Round((algorithm.Portfolio.TotalPortfolioValue - portfolioValue)*100/portfolioValue, 10));
        } // End of Run();

        /// <summary>
        /// Set the quit state.
        /// </summary>
        public void SetStatus(AlgorithmStatus state)
        {
            lock (_lock)
            {
                //We don't want anyone elseto set our internal state to "Running". 
                //This is controlled by the algorithm private variable only.
                if (state != AlgorithmStatus.Running)
                {
                    _algorithmState = state;
                }
            }
        }

        private IEnumerable<TimeSlice> Stream(AlgorithmNodePacket job, IAlgorithm algorithm, IDataFeed feed, IResultHandler results, CancellationToken cancellationToken)
        {
            bool setStartTime = false;
            var timeZone = algorithm.TimeZone;
            var history = algorithm.HistoryProvider;

            // get the required history job from the algorithm
            DateTime? lastHistoryTimeUtc = null;
            var historyRequests = algorithm.GetWarmupHistoryRequests().ToList();

            // initialize variables for progress computation
            var start = DateTime.UtcNow.Ticks;
            var nextStatusTime = DateTime.UtcNow.AddSeconds(1);
            var minimumIncrement = algorithm.Securities.Min(x => x.Value.SubscriptionDataConfig.Increment);
            minimumIncrement = (minimumIncrement == TimeSpan.Zero ? Time.OneSecond : minimumIncrement);

            if (historyRequests.Count != 0)
            {
                // rewrite internal feed requests
                var minResolution = algorithm.SubscriptionManager.Subscriptions.Where(x => !x.IsInternalFeed).Min(x => x.Resolution);
                foreach (var request in historyRequests)
                {
                    Security security;
                    if (algorithm.Securities.TryGetValue(request.Symbol, out security) && security.SubscriptionDataConfig.IsInternalFeed)
                    {
                        if (request.Resolution < minResolution)
                        {
                            request.Resolution = minResolution;
                            request.FillForwardResolution = request.FillForwardResolution.HasValue ? minResolution : (Resolution?) null;
                        }
                    }
                }

                // rewrite all to share the same fill forward resolution
                if (historyRequests.Any(x => x.FillForwardResolution.HasValue))
                {
                    minResolution = historyRequests.Where(x => x.FillForwardResolution.HasValue).Min(x => x.FillForwardResolution.Value);
                    foreach (var request in historyRequests.Where(x => x.FillForwardResolution.HasValue))
                    {
                        request.FillForwardResolution = minResolution;
                    }
                }

                foreach (var request in historyRequests)
                {
                    start = Math.Min(request.StartTimeUtc.Ticks, start);
                    Log.Trace(string.Format("AlgorithmManager.Stream(): WarmupHistoryRequest: {0}: Start: {1} End: {2} Resolution: {3}", request.Symbol, request.StartTimeUtc, request.EndTimeUtc, request.Resolution));
                }

                // make the history request and build time slices
                foreach (var slice in history.GetHistory(historyRequests, timeZone))
                {
                    TimeSlice timeSlice;
                    try
                    {
                        // we need to recombine this slice into a time slice
                        var paired = new List<KeyValuePair<Security, List<BaseData>>>();
                        foreach (var symbol in slice.Keys)
                        {
                            var security = algorithm.Securities[symbol];
                            var data = slice[symbol];
                            var list = new List<BaseData>();
                            var ticks = data as List<Tick>;
                            if (ticks != null) list.AddRange(ticks);
                            else               list.Add(data);
                            paired.Add(new KeyValuePair<Security, List<BaseData>>(security, list));
                        }
                        timeSlice = TimeSlice.Create(slice.Time.ConvertToUtc(timeZone), timeZone, algorithm.Portfolio.CashBook, paired, SecurityChanges.None);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                        algorithm.RunTimeError = err;
                        yield break;
                    }

                    if (timeSlice != null)
                    {
                        if (!setStartTime)
                        {
                            setStartTime = true;
                            _previousTime = timeSlice.Time;
                            algorithm.Debug("Algorithm warming up...");
                        }
                        if (DateTime.UtcNow > nextStatusTime)
                        {
                            // send some status to the user letting them know we're done history, but still warming up,
                            // catching up to real time data
                            nextStatusTime = DateTime.UtcNow.AddSeconds(1);
                            var percent = (int)(100 * (timeSlice.Time.Ticks - start) / (double)(DateTime.UtcNow.Ticks - start));
                            results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.History, string.Format("Catching up to realtime {0}%...", percent));
                        }
                        yield return timeSlice;
                        lastHistoryTimeUtc = timeSlice.Time;
                    } 
                }
            }

            // if we're not live or didn't event request warmup, then set us as not warming up
            if (!algorithm.LiveMode || historyRequests.Count == 0)
            {
                algorithm.SetFinishedWarmingUp();
                results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Running);
                if (historyRequests.Count != 0)
                {
                    algorithm.Debug("Algorithm finished warming up.");
                    Log.Trace("AlgorithmManager.Stream(): Finished warmup");
                }
            }

            foreach (var timeSlice in feed.Bridge.GetConsumingEnumerable(cancellationToken))
            {
                if (!setStartTime)
                {
                    setStartTime = true;
                    _previousTime = timeSlice.Time;
                }
                if (algorithm.LiveMode && algorithm.IsWarmingUp)
                {
                    // this is hand-over logic, we spin up the data feed first and then request
                    // the history for warmup, so there will be some overlap between the data
                    if (lastHistoryTimeUtc.HasValue && timeSlice.Time <= lastHistoryTimeUtc)
                    {
                        continue;
                    }

                    // in live mode wait to mark us as finished warming up when
                    // the data feed has caught up to now within the min increment
                    if (timeSlice.Time > DateTime.UtcNow.Subtract(minimumIncrement))
                    {
                        algorithm.SetFinishedWarmingUp();
                        results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Running);
                        algorithm.Debug("Algorithm finished warming up.");
                        Log.Trace("AlgorithmManager.Stream(): Finished warmup");
                    }
                    else if (DateTime.UtcNow > nextStatusTime)
                    {
                        // send some status to the user letting them know we're done history, but still warming up,
                        // catching up to real time data
                        nextStatusTime = DateTime.UtcNow.AddSeconds(1);
                        var percent = (int) (100*(timeSlice.Time.Ticks - start)/(double) (DateTime.UtcNow.Ticks - start));
                        results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.History, string.Format("Catching up to realtime {0}%...", percent));   
                    }
                }
                yield return timeSlice;
            }
        }

        /// <summary>
        /// Adds a method invoker if the method exists to the method invokers dictionary
        /// </summary>
        /// <typeparam name="T">The data type to check for 'OnData(T data)</typeparam>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="methodInvokers">The dictionary of method invokers</param>
        /// <param name="methodName">The name of the method to search for</param>
        /// <returns>True if the method existed and was added to the collection</returns>
        private bool AddMethodInvoker<T>(IAlgorithm algorithm, Dictionary<Type, MethodInvoker> methodInvokers, string methodName = "OnData")
        {
            var newSplitMethodInfo = algorithm.GetType().GetMethod(methodName, new[] {typeof (T)});
            if (newSplitMethodInfo != null)
            {
                methodInvokers.Add(typeof(T), newSplitMethodInfo.DelegateForCallMethod());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Performs delisting logic for the securities specified in <paramref name="newDelistings"/> that are marked as <see cref="DelistingType.Delisted"/>. 
        /// This includes liquidating the position and removing the security from the algorithm's collection.
        /// If we're unable to liquidate the position (maybe daily data or EOD already) then we'll add it to the <paramref name="delistingTickets"/>
        /// for the algo manager time loop to check later
        /// </summary>
        private static void HandleDelistedSymbols(IAlgorithm algorithm, Delistings newDelistings, ICollection<OrderTicket> delistingTickets)
        {
            foreach (var delisting in newDelistings.Values)
            {
                // submit an order to liquidate on market close
                if (delisting.Type == DelistingType.Warning)
                {
                    Log.Trace("AlgorithmManager.Run(): Security delisting warning: " + delisting.Symbol);
                    var security = algorithm.Securities[delisting.Symbol];
                    var submitOrderRequest = new SubmitOrderRequest(OrderType.MarketOnClose, security.Type, security.Symbol,
                        -security.Holdings.Quantity, 0, 0, algorithm.UtcTime, "Liquidate from delisting");
                    var ticket = algorithm.Transactions.ProcessRequest(submitOrderRequest);
                    delisting.SetOrderTicket(ticket);
                    delistingTickets.Add(ticket);
                }
                else
                {
                    Log.Trace("AlgorithmManager.Run(): Security delisted: " + delisting.Symbol);
                    algorithm.Securities.Remove(delisting.Symbol);
                    Log.Trace("AlgorithmManager.Run(): Security removed: " + delisting.Symbol);
                }
            }
        }

        /// <summary>
        /// Samples the benchmark in a  try/catch block
        /// </summary>
        private void SampleBenchmark(IAlgorithm algorithm, IResultHandler results, DateTime time)
        {
            try
            {
                // backtest mode, sample benchmark on day changes
                results.SampleBenchmark(time, algorithm.Benchmark.Evaluate(time).SmartRounding());
            }
            catch (Exception err)
            {
                algorithm.RunTimeError = err;
                _algorithmState = AlgorithmStatus.RuntimeError;
                Log.Error("AlgorithmManager.Run(): RuntimeError: SampleBenchmark: " + err.Message + " STACK >>> " + err.StackTrace);
            }
        }


    } // End of AlgorithmManager

} // End of Namespace.
