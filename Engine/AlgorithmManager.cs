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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Fasterflect;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine
{
    /******************************************************** 
    * QUANTCONNECT NAMESPACES
    *********************************************************/
    /// <summary>
    /// Algorithm manager class executes the algorithm and generates and passes through the algorithm events.
    /// </summary>
    public static class AlgorithmManager
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        private static DateTime _previousTime;
        private static AlgorithmStatus _algorithmState = AlgorithmStatus.Running;
        private static readonly object _lock = new object();
        private static string _algorithmId = "";
        private static DateTime _currentTimeStepTime;
        private static readonly TimeSpan _timeLoopMaximum = TimeSpan.FromMinutes(Config.GetDouble("algorithm-manager-time-loop-maximum", 10));

        private static long _dataPointCount;

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/

        /// <summary>
        /// Publicly accessible algorithm status
        /// </summary>
        public static AlgorithmStatus State
        {
            get
            {
                return _algorithmState;
            }
        }

        /// <summary>
        /// Public access to the currently running algorithm id.
        /// </summary>
        public static string AlgorithmId
        {
            get
            {
                return _algorithmId;
            }
        }

        /// <summary>
        /// Gets the amount of time spent on the current time step
        /// </summary>
        public static TimeSpan CurrentTimeStepElapsed
        {
            get { return _currentTimeStepTime == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - _currentTimeStepTime; }
        }

        /// <summary>
        /// Gets a function used with the Isolator for verifying we're not spending too much time in each
        /// algo manager timer loop
        /// </summary>
        public static readonly Func<string> TimeLoopWithinLimits = () =>
        {
            if (CurrentTimeStepElapsed > _timeLoopMaximum)
            {
                return "Algorithm took longer than 10 minutes on a single time loop.";
            }
            return null;
        };

        /// <summary>
        /// Quit state flag for the running algorithm. When true the user has requested the backtest stops through a Quit() method.
        /// </summary>
        /// <seealso cref="QCAlgorithm.Quit"/>
        public static bool QuitState
        {
            get
            {
                return _algorithmState == AlgorithmStatus.Deleted;
            }
        }

        /// <summary>
        /// Gets the number of data points processed per second
        /// </summary>
        public static long DataPoints
        {
            get
            {
                return _dataPointCount;
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Launch the algorithm manager to run this strategy
        /// </summary>
        /// <param name="job">Algorithm job</param>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="feed">Datafeed object</param>
        /// <param name="transactions">Transaction manager object</param>
        /// <param name="results">Result handler object</param>
        /// <param name="setup">Setup handler object</param>
        /// <param name="realtime">Realtime processing object</param>
        /// <remarks>Modify with caution</remarks>
        public static void Run(AlgorithmNodePacket job, IAlgorithm algorithm, IDataFeed feed, ITransactionHandler transactions, IResultHandler results, ISetupHandler setup, IRealTimeHandler realtime) 
        {
            //Initialize:
            _dataPointCount = 0;
            var startingPortfolioValue = setup.StartingPortfolioValue;
            var backtestMode = (job.Type == PacketType.BacktestNode);
            var methodInvokers = new Dictionary<Type, MethodInvoker>();
            var marginCallFrequency = TimeSpan.FromMinutes(5);
            var nextMarginCallTime = DateTime.MinValue;

            //Initialize Properties:
            _algorithmId = job.AlgorithmId;
            _algorithmState = AlgorithmStatus.Running;
            _previousTime = setup.StartingDate.Date;

            //Create the method accessors to push generic types into algorithm: Find all OnData events:

            // Algorithm 1.0 data accessors
            var hasOnTradeBar = AddMethodInvoker<Dictionary<string, TradeBar>>(algorithm, methodInvokers, "OnTradeBar");
            var hasOnTick = AddMethodInvoker<Dictionary<string, List<Tick>>>(algorithm, methodInvokers, "OnTick");

            // Algorithm 2.0 data accessors
            var hasOnDataTradeBars = AddMethodInvoker<TradeBars>(algorithm, methodInvokers);
            var hasOnDataTicks = AddMethodInvoker<Ticks>(algorithm, methodInvokers);

            // determine what mode we're in
            var backwardsCompatibilityMode = !hasOnDataTradeBars && !hasOnDataTicks;

            // dividend and split events
            var hasOnDataDividends = AddMethodInvoker<Dividends>(algorithm, methodInvokers);
            var hasOnDataSplits = AddMethodInvoker<Splits>(algorithm, methodInvokers);

            //Go through the subscription types and create invokers to trigger the event handlers for each custom type:
            foreach (var config in feed.Subscriptions) 
            {
                //If type is a tradebar, combine tradebars and ticks into unified array:
                if (config.Type.Name != "TradeBar" && config.Type.Name != "Tick") 
                {
                    //Get the matching method for this event handler - e.g. public void OnData(Quandl data) { .. }
                    var genericMethod = (algorithm.GetType()).GetMethod("OnData", new[] { config.Type });

                    //If we already have this Type-handler then don't add it to invokers again.
                    if (methodInvokers.ContainsKey(config.Type)) continue;

                    //If we couldnt find the event handler, let the user know we can't fire that event.
                    if (genericMethod == null)
                    {
                        algorithm.RunTimeError = new Exception("Data event handler not found, please create a function matching this template: public void OnData(" + config.Type.Name + " data) {  }");
                        _algorithmState = AlgorithmStatus.RuntimeError;
                        return;
                    }
                    methodInvokers.Add(config.Type, genericMethod.DelegateForCallMethod());
                }
            }

            //Loop over the queues: get a data collection, then pass them all into relevent methods in the algorithm.
            Log.Trace("AlgorithmManager.Run(): Begin DataStream - Start: " + algorithm.StartDate + " Stop: " + algorithm.EndDate);
            foreach (var newData in DataStream.GetData(feed, setup.StartingDate))
            {
                // reset our timer on each loop
                _currentTimeStepTime = DateTime.UtcNow;

                //Check this backtest is still running:
                if (_algorithmState != AlgorithmStatus.Running) break;

                //Execute with TimeLimit Monitor:
                if (Isolator.IsCancellationRequested)
                {
                    return;
                }

                var time = DataStream.AlgorithmTime;

                //If we're in backtest mode we need to capture the daily performance. We do this here directly
                //before updating the algorithm state with the new data from this time step, otherwise we'll
                //produce incorrect samples (they'll take into account this time step's new price values)
                if (backtestMode)
                {
                    //Refresh the realtime event monitor: 
                    //in backtest mode use the algorithms clock as realtime.
                    realtime.SetTime(time);

                    //On day-change sample equity and daily performance for statistics calculations
                    if (_previousTime.Date != time.Date)
                    {
                        //Sample the portfolio value over time for chart.
                        results.SampleEquity(_previousTime, Math.Round(algorithm.Portfolio.TotalPortfolioValue, 4));

                        //Check for divide by zero
                        if (startingPortfolioValue == 0m)
                        {
                            results.SamplePerformance(_previousTime.Date, 0);
                        }
                        else
                        {
                            results.SamplePerformance(_previousTime.Date, Math.Round((algorithm.Portfolio.TotalPortfolioValue - startingPortfolioValue) * 100 / startingPortfolioValue, 10));
                        }
                        startingPortfolioValue = algorithm.Portfolio.TotalPortfolioValue;
                    }
                }

                //Update algorithm state after capturing performance from previous day

                //On each time step push the real time prices to the cashbook so we can have updated conversion rates
                algorithm.Portfolio.CashBook.Update(newData);

                //Update the securities properties: first before calling user code to avoid issues with data
                algorithm.Securities.Update(time, newData);

                //Pass in the new time first:
                algorithm.SetDateTime(time);

                // process fill models on the updated data before entering algorithm, applies to all non-market orders
                transactions.ProcessSynchronousEvents();

                //Check if the user's signalled Quit: loop over data until day changes.
                if (algorithm.GetQuit())
                {
                    _algorithmState = AlgorithmStatus.Quit;
                    break;
                }
                if (algorithm.RunTimeError != null)
                {
                    _algorithmState = AlgorithmStatus.RuntimeError;
                    break;
                }

                // perform margin calls, in live mode we can also use realtime to emit these
                if (time >= nextMarginCallTime || (Engine.LiveMode && nextMarginCallTime > DateTime.Now))
                {
                    // determine if there are possible margin call orders to be executed
                    bool issueMarginCallWarning;
                    var marginCallOrders = algorithm.Portfolio.ScanForMarginCall(out issueMarginCallWarning);
                    if (marginCallOrders.Count != 0)
                    {
                        try
                        {
                            // tell the algorithm we're about to issue the margin call
                            algorithm.OnMarginCall(marginCallOrders);
                        }
                        catch (Exception err)
                        {
                            algorithm.RunTimeError = err;
                            _algorithmState = AlgorithmStatus.RuntimeError;
                            Log.Debug("AlgorithmManager.Run(): RuntimeError: OnMarginCall: " + err.Message + " STACK >>> " + err.StackTrace);
                            return;
                        }

                        // execute the margin call orders
                        var executedOrders = algorithm.Portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders);
                        foreach (var order in executedOrders)
                        {
                            algorithm.Error(string.Format("{0} - Executed MarginCallOrder: {1} - Quantity: {2} @ {3}", algorithm.Time, order.Symbol, order.Quantity, order.Price));
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
                            Log.Debug("AlgorithmManager.Run(): RuntimeError: OnMarginCallWarning: " + err.Message + " STACK >>> " + err.StackTrace);
                        }
                    }

                    nextMarginCallTime = time + marginCallFrequency;
                }

                //Trigger the data events: Invoke the types we have data for:
                var oldBars = new Dictionary<string, TradeBar>();
                var oldTicks = new Dictionary<string, List<Tick>>();
                var newBars = new TradeBars(time);
                var newTicks = new Ticks(time);
                var newDividends = new Dividends(time);
                var newSplits = new Splits(time);

                //Invoke all non-tradebars, non-ticks methods and build up the TradeBars and Ticks dictionaries
                // --> i == Subscription Configuration Index, so we don't need to compare types.
                foreach (var i in newData.Keys)
                {
                    //Data point and config of this point:
                    var dataPoints = newData[i];
                    var config = feed.Subscriptions[i];

                    //Keep track of how many data points we've processed
                    _dataPointCount += dataPoints.Count;

                    //We don't want to pump data that we added just for currency conversions
                    if (config.IsInternalFeed)
                    {
                        continue;
                    }

                    //Create TradeBars Unified Data --> OR --> invoke generic data event. One loop.
                    //  Aggregate Dividends and Splits -- invoke portfolio application methods
                    foreach (var dataPoint in dataPoints)
                    {
                        var dividend = dataPoint as Dividend;
                        if (dividend != null)
                        {
                            Log.Trace("AlgorithmManager.Run(): Applying Dividend for " + dividend.Symbol);
                            // if this is a dividend apply to portfolio
                            algorithm.Portfolio.ApplyDividend(dividend);
                            if (hasOnDataDividends)
                            {
                                // and add to our data dictionary to pump into OnData(Dividends data)
                                newDividends.Add(dividend);
                            }
                            continue;
                        }

                        var split = dataPoint as Split;
                        if (split != null)
                        {
                            Log.Trace("AlgorithmManager.Run(): Applying Split for " + split.Symbol);

                            // if this is a split apply to portfolio
                            algorithm.Portfolio.ApplySplit(split);
                            if (hasOnDataSplits)
                            {
                                // and add to our data dictionary to pump into OnData(Splits data)
                                newSplits.Add(split);
                            }
                            continue;
                        }

                        //Update registered consolidators for this symbol index
                        try
                        {
                            for (var j = 0; j < config.Consolidators.Count; j++)
                            {
                                config.Consolidators[j].Update(dataPoint);
                            }
                        }
                        catch (Exception err)
                        {
                            algorithm.RunTimeError = err;
                            _algorithmState = AlgorithmStatus.RuntimeError;
                            Log.Error("AlgorithmManager.Run(): RuntimeError: Consolidators update: " + err.Message);
                            return;
                        }

                        // TRADEBAR -- add to our dictionary
                        if (dataPoint.DataType == MarketDataType.TradeBar)
                        {
                            var bar = dataPoint as TradeBar;
                            if (bar != null)
                            {
                                if (backwardsCompatibilityMode)
                                {
                                    oldBars[bar.Symbol] = bar;
                                }
                                else
                                {
                                    newBars[bar.Symbol] = bar;
                                }
                                continue;
                            }
                        }

                        // TICK -- add to our dictionary
                        if (dataPoint.DataType == MarketDataType.Tick)
                        {
                            var tick = dataPoint as Tick;
                            if (tick != null)
                            {
                                if (backwardsCompatibilityMode)
                                {
                                    List<Tick> ticks;
                                    if (!oldTicks.TryGetValue(tick.Symbol, out ticks))
                                    {
                                        ticks = new List<Tick>(3);
                                        oldTicks.Add(tick.Symbol, ticks);
                                    }
                                    ticks.Add(tick);
                                }
                                else
                                {
                                    List<Tick> ticks;
                                    if (!newTicks.TryGetValue(tick.Symbol, out ticks))
                                    {
                                        ticks = new List<Tick>(3);
                                        newTicks.Add(tick.Symbol, ticks);
                                    }
                                    ticks.Add(tick);
                                }
                                continue;
                            }
                        }

                        // if it was nothing else then it must be custom data

                        // CUSTOM DATA -- invoke on data method
                        //Send data into the generic algorithm event handlers
                        try
                        {
                            methodInvokers[config.Type](algorithm, dataPoint);
                        }
                        catch (Exception err)
                        {
                            algorithm.RunTimeError = err;
                            _algorithmState = AlgorithmStatus.RuntimeError;
                            Log.Debug("AlgorithmManager.Run(): RuntimeError: Custom Data: " + err.Message + " STACK >>> " + err.StackTrace);
                            return;
                        }
                    }
                }

                try
                {
                    // fire off the dividend and split events before pricing events
                    if (hasOnDataDividends && newDividends.Count != 0)
                    {
                        methodInvokers[typeof (Dividends)](algorithm, newDividends);
                    }
                    if (hasOnDataSplits && newSplits.Count != 0)
                    {
                        methodInvokers[typeof (Splits)](algorithm, newSplits);
                    }
                }
                catch (Exception err)
                {
                    algorithm.RunTimeError = err;
                    _algorithmState = AlgorithmStatus.RuntimeError;
                    Log.Debug("AlgorithmManager.Run(): RuntimeError: Dividends/Splits: " + err.Message + " STACK >>> " + err.StackTrace);
                    return;
                }

                //After we've fired all other events in this second, fire the pricing events:
                if (backwardsCompatibilityMode)
                {
                    //Log.Debug("AlgorithmManager.Run(): Invoking v1.0 Event Handlers...");
                    try
                    {
                        if (hasOnTradeBar && oldBars.Count > 0) methodInvokers[typeof (Dictionary<string, TradeBar>)](algorithm, oldBars);
                        if (hasOnTick && oldTicks.Count > 0) methodInvokers[typeof (Dictionary<string, List<Tick>>)](algorithm, oldTicks);
                    }
                    catch (Exception err)
                    {
                        algorithm.RunTimeError = err;
                        _algorithmState = AlgorithmStatus.RuntimeError;
                        Log.Debug("AlgorithmManager.Run(): RuntimeError: Backwards Compatibility Mode: " + err.Message + " STACK >>> " + err.StackTrace);
                        return;
                    }
                }
                else
                {
                    //Log.Debug("AlgorithmManager.Run(): Invoking v2.0 Event Handlers...");
                    try
                    {
                        if (hasOnDataTradeBars && newBars.Count > 0) methodInvokers[typeof (TradeBars)](algorithm, newBars);
                        if (hasOnDataTicks && newTicks.Count > 0) methodInvokers[typeof (Ticks)](algorithm, newTicks);
                    }
                    catch (Exception err)
                    {
                        algorithm.RunTimeError = err;
                        _algorithmState = AlgorithmStatus.RuntimeError;
                        Log.Debug("AlgorithmManager.Run(): RuntimeError: New Style Mode: " + err.Message + " STACK >>> " + err.StackTrace);
                        return;
                    }
                }

                //If its the historical/paper trading models, wait until market orders have been "filled"
                // Manually trigger the event handler to prevent thread switch.
                transactions.ProcessSynchronousEvents();

                //Save the previous time for the sample calculations
                _previousTime = time;

                // Process any required events of the results handler such as sampling assets, equity, or stock prices.
                results.ProcessSynchronousEvents();
            } // End of ForEach DataStream

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
                Log.Debug("AlgorithmManager.OnEndOfAlgorithm(): " + err.Message + " STACK >>> " + err.StackTrace);
                return;
            }

            // Process any required events of the results handler such as sampling assets, equity, or stock prices.
            results.ProcessSynchronousEvents(forceProcess: true);

            //Liquidate Holdings for Calculations:
            if (_algorithmState == AlgorithmStatus.Liquidated || !Engine.LiveMode)
            {
                // without this we can't liquidate equities since the exchange is 'technically' closed
                var hackedFrontier = algorithm.Time == DateTime.MinValue ? DateTime.MinValue : algorithm.Time.AddMilliseconds(-1);
                algorithm.SetDateTime(hackedFrontier);
                foreach (var security in algorithm.Securities)
                {
                    security.Value.SetMarketPrice(hackedFrontier, null);
                }

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
            results.SampleEquity(DataStream.AlgorithmTime, Math.Round(algorithm.Portfolio.TotalPortfolioValue, 4));
            results.SamplePerformance(DataStream.AlgorithmTime, Math.Round((algorithm.Portfolio.TotalPortfolioValue - startingPortfolioValue) * 100 / startingPortfolioValue, 10));
        } // End of Run();

        /// <summary>
        /// Reset all variables required before next loops
        /// </summary>
        public static void ResetManager() 
        {
            //Reset before the next loop/
            DataStream.ResetFrontier();
            _algorithmId = "";
            _currentTimeStepTime = new DateTime();
            _algorithmState = AlgorithmStatus.Running;
        }


        /// <summary>
        /// Set the quit state.
        /// </summary>
        public static void SetStatus(AlgorithmStatus state)
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

        /// <summary>
        /// Adds a method invoker if the method exists to the method invokers dictionary
        /// </summary>
        /// <typeparam name="T">The data type to check for 'OnData(T data)</typeparam>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="methodInvokers">The dictionary of method invokers</param>
        /// <param name="methodName">The name of the method to search for</param>
        /// <returns>True if the method existed and was added to the collection</returns>
        private static bool AddMethodInvoker<T>(IAlgorithm algorithm, Dictionary<Type, MethodInvoker> methodInvokers, string methodName = "OnData")
        {
            var newSplitMethodInfo = algorithm.GetType().GetMethod(methodName, new[] {typeof (T)});
            if (newSplitMethodInfo != null)
            {
                methodInvokers.Add(typeof(T), newSplitMethodInfo.DelegateForCallMethod());
                return true;
            }
            return false;
        }
    } // End of AlgorithmManager

} // End of Namespace.
