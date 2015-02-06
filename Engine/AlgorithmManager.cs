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
/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Collections.Generic;
using System.Threading;
using Fasterflect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Packets;

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
        private static DateTime _frontier;
        private static Exception _runtimeError = new Exception();
        private static AlgorithmStatus _algorithmState = AlgorithmStatus.Running;
        private static readonly object _lock = new object();
        private static string _algorithmId = "";

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Current time horizon of the algorithm
        /// </summary>
        public static DateTime Frontier
        {
            get 
            {
                return _frontier;
            }
        }

        /// <summary>
        /// Public flag for runtime error for this user
        /// </summary>
        public static Exception RunTimeError
        {
            get 
            {
                return _runtimeError;
            }
        }

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
            var backwardsCompatibilityMode = false;
            var tradebarsType = typeof (TradeBars);
            var ticksType = typeof(Ticks);
            var startingPerformance = setup.StartingCapital;
            var backtestMode = (job.Type == PacketType.BacktestNode);
            var methodInvokers = new Dictionary<Type, MethodInvoker>();

            //Initialize Properties:
            _frontier = setup.StartingDate;
            _runtimeError = null;
            _algorithmId = job.AlgorithmId;
            _algorithmState = AlgorithmStatus.Running;
            _previousTime = setup.StartingDate.Date;

            //Create the method accessors to push generic types into algorithm: Find all OnData events:

            //Algorithm 1.0 Data Accessors.
            //If the users defined these methods, add them in manually. This allows keeping backwards compatibility to algorithm 1.0.
            var oldTradeBarsMethodInfo = (algorithm.GetType()).GetMethod("OnTradeBar",   new[] { typeof(Dictionary<string, TradeBar>) });
            var oldTicksMethodInfo = (algorithm.GetType()).GetMethod("OnTick", new[] { typeof(Dictionary<string, List<Tick>>) });

            //Algorithm 2.0 Data Generics Accessors.
            //New hidden access to tradebars with custom type.
            var newTradeBarsMethodInfo = (algorithm.GetType()).GetMethod("OnData", new[] { tradebarsType });
            var newTicksMethodInfo = (algorithm.GetType()).GetMethod("OnData", new[] { ticksType });

            if (newTradeBarsMethodInfo == null && newTicksMethodInfo == null)
            {
                backwardsCompatibilityMode = true;
                if (oldTradeBarsMethodInfo != null) methodInvokers.Add(tradebarsType, oldTradeBarsMethodInfo.DelegateForCallMethod());
                if (oldTradeBarsMethodInfo != null) methodInvokers.Add(ticksType, oldTicksMethodInfo.DelegateForCallMethod());
            }
            else 
            { 
                backwardsCompatibilityMode = false;
                if (newTradeBarsMethodInfo != null) methodInvokers.Add(tradebarsType, newTradeBarsMethodInfo.DelegateForCallMethod());
                if (newTicksMethodInfo != null) methodInvokers.Add(ticksType, newTicksMethodInfo.DelegateForCallMethod());
            }

            //Go through the subscription types and create invokers to trigger the event handlers for each custom type:
            foreach (var config in feed.Subscriptions) 
            {
                //If type is a tradebar, combine tradebars and ticks into unified array:
                if (config.Type.Name != "TradeBar" && config.Type.Name != "Tick") 
                {
                    //Get the matching method for this event handler - e.g. public void OnData(Quandl data) { .. }
                    var genericMethod = (algorithm.GetType()).GetMethod("OnData", new[] { config.Type });

                    //Is we already have this Type-handler then don't add it to invokers again.
                    if (methodInvokers.ContainsKey(config.Type)) continue;

                    //If we couldnt find the event handler, let the user know we can't fire that event.
                    if (genericMethod == null)
                    {
                        _runtimeError = new Exception("Data event handler not found, please create a function matching this template: public void OnData(" + config.Type.Name + " data) {  }");
                        _algorithmState = AlgorithmStatus.RuntimeError;
                        return;
                    }
                    methodInvokers.Add(config.Type, genericMethod.DelegateForCallMethod());
                }
            }

            //Loop over the queues: get a data collection, then pass them all into relevent methods in the algorithm.
            Log.Debug("AlgorithmManager.Run(): Algorithm initialized, launching time loop.");
            foreach (var newData in DataStream.GetData(feed, setup.StartingDate)) 
            {
                //Check this backtest is still running:
                if (_algorithmState != AlgorithmStatus.Running) break;
                
                //Go over each time stamp we've collected, pass it into the algorithm in order:
                foreach (var time in newData.Keys) 
                {
                    //Set the time frontier:
                    _frontier = time;

                    //Execute with TimeLimit Monitor:
                    if (Isolator.IsCancellationRequested) return;

                    //Refresh the realtime event monitor:
                    realtime.SetTime(time);

                    //Fire EOD if the time packet we just processed is greater 
                    if (backtestMode && _previousTime.Date != time.Date)
                    {
                        //Sample the portfolio value over time for chart.
                        results.SampleEquity(_previousTime, Math.Round(algorithm.Portfolio.TotalPortfolioValue, 4));

                        if (startingPerformance == 0)
                        {
                            results.SamplePerformance(_previousTime.Date, 0);
                        }
                        else
                        {
                            results.SamplePerformance(_previousTime.Date, Math.Round((algorithm.Portfolio.TotalPortfolioValue - startingPerformance) * 100 / startingPerformance, 10));
                        }

                        startingPerformance = algorithm.Portfolio.TotalPortfolioValue;
                    }

                    //Check if the user's signalled Quit: loop over data until day changes.
                    if (algorithm.GetQuit())
                    {
                        _algorithmState = AlgorithmStatus.Quit;
                        break;
                    }

                    //Pass in the new time first:
                    algorithm.SetDateTime(time);

                    //Trigger the data events: Invoke the types we have data for:
                    var oldBars = new Dictionary<string, TradeBar>();
                    var oldTicks = new Dictionary<string, List<Tick>>();
                    var newBars = new TradeBars(time);
                    var newTicks = new Ticks(time);

                    //Invoke all non-tradebars, non-ticks methods:
                    // --> i == Subscription Configuration Index, so we don't need to compare types.
                    foreach (var i in newData[time].Keys) 
                    {    
                        //Data point and config of this point:
                        var dataPoints = newData[time][i];
                        var config = feed.Subscriptions[i];

                        //Create TradeBars Unified Data --> OR --> invoke generic data event. One loop.
                        foreach (var dataPoint in dataPoints) 
                        {
                            //Update the securities properties: first before calling user code to avoid issues with data
                            algorithm.Securities.Update(time, dataPoint);

                            //Update registered consolidators for this symbol index
                            for (var j = 0; j < config.Consolidators.Count; j++)
                            {
                                config.Consolidators[j].Update(dataPoint);
                            }

                            switch (config.Type.Name)
                            {
                                case "TradeBar":
                                    var bar = dataPoint as TradeBar;
                                    try 
                                    {
                                        if (bar != null) 
                                        {
                                            if (backwardsCompatibilityMode) 
                                            {
                                                if (!oldBars.ContainsKey(bar.Symbol)) oldBars.Add(bar.Symbol, bar);
                                            }
                                            else
                                            {
                                                if (!newBars.ContainsKey(bar.Symbol)) newBars.Add(bar.Symbol, bar);
                                            }
                                        }
                                    } 
                                    catch (Exception err) 
                                    {
                                        Log.Error(time.ToLongTimeString() + " >> " + bar.Time.ToLongTimeString() + " >> " + bar.Symbol + " >> " + bar.Value.ToString("C"));
                                        Log.Error("AlgorithmManager.Run(): Failed to add TradeBar (" + bar.Symbol + ") Time: (" + time.ToLongTimeString() + ") Count:(" + newBars.Count + ") " + err.Message);
                                    }
                                    break;

                                case "Tick":
                                    var tick = dataPoint as Tick;
                                    if (tick != null) 
                                    {
                                         if (backwardsCompatibilityMode) {
                                             if (!oldTicks.ContainsKey(tick.Symbol)) { oldTicks.Add(tick.Symbol, new List<Tick>()); }
                                             oldTicks[tick.Symbol].Add(tick);
                                         } 
                                         else 
                                         {
                                             if (!newTicks.ContainsKey(tick.Symbol)) { newTicks.Add(tick.Symbol, new List<Tick>()); }
                                             newTicks[tick.Symbol].Add(tick);
                                         }
                                    }
                                    break;

                                default:
                                    //Send data into the generic algorithm event handlers
                                    try 
                                    {
                                        methodInvokers[config.Type](algorithm, dataPoint);
                                    } 
                                    catch (Exception err) 
                                    {
                                        _runtimeError = err;
                                        _algorithmState = AlgorithmStatus.RuntimeError;
                                        Log.Error("AlgorithmManager.Run(): RuntimeError: Custom Data: " + err.Message + " STACK >>> " + err.StackTrace);
                                        return;
                                    }
                                    break;
                            }
                        }
                    }

                    //After we've fired all other events in this second, fire the pricing events:
                    if (backwardsCompatibilityMode) 
                    {
                        //Log.Debug("AlgorithmManager.Run(): Invoking v1.0 Event Handlers...");
                        try
                        {
                            if (oldTradeBarsMethodInfo != null && oldBars.Count > 0) methodInvokers[tradebarsType](algorithm, oldBars);
                            if (oldTicksMethodInfo != null && oldTicks.Count > 0) methodInvokers[ticksType](algorithm, oldTicks);
                        }
                        catch (Exception err) 
                        {
                            _runtimeError = err;
                            _algorithmState = AlgorithmStatus.RuntimeError;
                            Log.Error("AlgorithmManager.Run(): RuntimeError: Backwards Compatibility Mode: " + err.Message + " STACK >>> " + err.StackTrace);
                            return;
                        }
                    } 
                    else 
                    {
                        //Log.Debug("AlgorithmManager.Run(): Invoking v2.0 Event Handlers...");
                        try
                        {
                            if (newTradeBarsMethodInfo != null && newBars.Count > 0) methodInvokers[tradebarsType](algorithm, newBars);
                            if (newTicksMethodInfo != null && newTicks.Count > 0) methodInvokers[ticksType](algorithm, newTicks);
                        } 
                        catch (Exception err)
                        {
                            _runtimeError = err;
                            _algorithmState = AlgorithmStatus.RuntimeError;
                            Log.Error("AlgorithmManager.Run(): RuntimeError: New Style Mode: " + err.Message + " STACK >>> " + err.StackTrace);
                            return;
                        }
                    }

                    //If its the historical/paper trading models, wait until market orders have been "filled"
                    // Manually trigger the event handler to prevent thread switch.
                    transactions.ProcessSynchronousEvents();

                    // Process any required events of the results handler such as sampling assets, equity, or stock prices.
                    results.ProcessSynchronousEvents();

                    //Save the previous time for the sample calculations
                    _previousTime = time;

                } // End of Time Loop

            } // End of ForEach DataStream

            //Stream over:: Send the final packet and fire final events:
            Log.Trace("AlgorithmManager.Run(): Firing On End Of Algorithm...");
            try
            {
                algorithm.OnEndOfAlgorithm();
            }
            catch (Exception err)
            {
                _runtimeError = new Exception("Error running OnEndOfAlgorithm(): " + err.Message, err.InnerException);
                _algorithmState = AlgorithmStatus.RuntimeError;
                return;
            }

            // Process any required events of the results handler such as sampling assets, equity, or stock prices.
            results.ProcessSynchronousEvents();

            //Liquidate Holdings for Calculations:
            if (_algorithmState == AlgorithmStatus.Liquidated || !Engine.LiveMode)
            {
                Log.Trace("AlgorithmManager.Run(): Liquidating algorithm holdings...");
                algorithm.Liquidate();
                results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Liquidated);
            }

            //Manually stopped the algorithm
            if (_algorithmState == AlgorithmStatus.Stopped)
            {
                Log.Trace("AlgorithmManager.Run(): Stopping algorithm...");
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
            results.SampleEquity(_frontier, Math.Round(algorithm.Portfolio.TotalPortfolioValue, 4));
            results.SamplePerformance(_frontier, Math.Round((algorithm.Portfolio.TotalPortfolioValue - startingPerformance) * 100 / startingPerformance, 10));
        } // End of Run();


        /// <summary>
        /// Process the user defined messaging by retrieving all the data inside the algorithm and sending to result handler.
        /// </summary>
        /// <param name="results">IResultHandler object to send the results</param>
        /// <param name="algorithm">Algorithm to extract messages from</param>
        public static void ProcessMessages(IResultHandler results, IAlgorithm algorithm)
        {
            //Send out the debug messages:
            foreach (var message in algorithm.DebugMessages)
            {
                results.DebugMessage(message);
            }
            algorithm.DebugMessages.Clear();

            //Send out the error messages:
            foreach (var message in algorithm.ErrorMessages)
            {
                results.ErrorMessage(message);
            }
            algorithm.ErrorMessages.Clear();

            //Send out the log messages:
            foreach (var message in algorithm.LogMessages)
            {
                results.LogMessage(message);
            }
            algorithm.LogMessages.Clear();

            //Set the running statistics:
            foreach (var pair in algorithm.RuntimeStatistics)
            {
                results.RuntimeStatistic(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Reset all variables required before next loops
        /// </summary>
        public static void ResetManager() 
        {
            //Reset before the next loop/
            _frontier = new DateTime();
            _runtimeError = null;
            _algorithmId = "";
            _algorithmState = AlgorithmStatus.Running;
        }


        /// <summary>
        /// Set the quit state.
        /// </summary>
        public static void SetStatus(AlgorithmStatus state)
        {
            lock (_lock)
            {
                _algorithmState = state;
            }
        }

    } // End of AlgorithmManager

} // End of Namespace.
