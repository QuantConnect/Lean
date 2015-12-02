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
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides a means of distributing output from enumerators from a dedicated separate thread
    /// </summary>
    public class BaseDataExchange
    {
        private int _sleepInterval = 1;
        private volatile bool _isStopping = false;
        private Func<Exception, bool> _isFatalError;

        private readonly string _name;
        private readonly ConcurrentDictionary<Symbol, DataHandler> _dataHandlers;

        // using concurrent dictionary for fast/easy contains/remove, the int value is nothingness
        private readonly ConcurrentDictionary<IEnumerator<BaseData>, EnumeratorHandler> _enumeratorHandlers;

        /// <summary>
        /// Gets or sets how long this thread will sleep when no data is available
        /// </summary>
        public int SleepInterval
        {
            get { return _sleepInterval; }
            set { if (value > -1) _sleepInterval = value; }
        }

        /// <summary>
        /// Gets a name for this exchange
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataExchange"/>
        /// </summary>
        /// <param name="enumerators">The enumerators to fanout</param>
        public BaseDataExchange(params IEnumerator<BaseData>[] enumerators)
            : this(string.Empty, enumerators)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataExchange"/>
        /// </summary>
        /// <param name="name">A name for this exchange</param>
        /// <param name="enumerators">The enumerators to fanout</param>
        public BaseDataExchange(string name, params IEnumerator<BaseData>[] enumerators)
        {
            _name = name;
            _enumeratorHandlers = new ConcurrentDictionary<IEnumerator<BaseData>, EnumeratorHandler>();
            foreach (var enumerator in enumerators)
            {
                // enumerators added via ctor are always called
                _enumeratorHandlers.AddOrUpdate(enumerator, new EnumeratorHandler(enumerator, () => true));
            }
            _isFatalError = x => false;
            _dataHandlers = new ConcurrentDictionary<Symbol, DataHandler>();
        }

        /// <summary>
        /// Adds the enumerator to this exchange. If it has already been added
        /// then it will remain registered in the exchange only once
        /// </summary>
        /// <param name="enumerator">The enumerator to be added</param>
        /// <param name="handler">The handler to use when this symbol's data is encountered</param>
        public void AddEnumerator(IEnumerator<BaseData> enumerator, EnumeratorHandler handler)
        {
            _enumeratorHandlers.TryAdd(enumerator, handler);
        }

        /// <summary>
        /// Adds the enumerator to this exchange. If it has already been added
        /// then it will remain registered in the exchange only once
        /// </summary>
        /// <param name="enumerator">The enumerator to be added</param>
        /// <param name="shouldMoveNext">Function used to determine if move next should be called on this
        /// enumerator, defaults to always returning true</param>
        /// <param name="enumeratorFinished">Delegate called when the enumerator move next returns false</param>
        public void AddEnumerator(IEnumerator<BaseData> enumerator, Func<bool> shouldMoveNext = null, Action<EnumeratorHandler> enumeratorFinished = null)
        {
            var enumeratorHandler = new EnumeratorHandler(enumerator, shouldMoveNext);
            if (enumeratorFinished != null)
            {
                enumeratorHandler.EnumeratorFinished += (sender, args) => enumeratorFinished(args);
            }
            _enumeratorHandlers.TryAdd(enumerator, enumeratorHandler);
        }

        /// <summary>
        /// Sets the specified function as the error handler. This function
        /// returns true if it is a fatal error and queue consumption should
        /// cease.
        /// </summary>
        /// <param name="isFatalError">The error handling function to use when an
        /// error is encountered during queue consumption. Returns true if queue
        /// consumption should be stopped, returns false if queue consumption should
        /// continue</param>
        public void SetErrorHandler(Func<Exception, bool> isFatalError)
        {
            // default to false;
            _isFatalError = isFatalError ?? (x => false);
        }

        /// <summary>
        /// Sets the specified hander function to handle data for the handler's symbol
        /// </summary>
        /// <param name="handler">The handler to use when this symbol's data is encountered</param>
        /// <returns>An identifier that can be used to remove this handler</returns>
        public void SetDataHandler(DataHandler handler)
        {
            _dataHandlers[handler.Symbol] = handler;
        }

        /// <summary>
        /// Sets the specified hander function to handle data for the handler's symbol
        /// </summary>
        /// <param name="symbol">The symbol whose data is to be handled</param>
        /// <param name="handler">The handler to use when this symbol's data is encountered</param>
        /// <returns>An identifier that can be used to remove this handler</returns>
        public void SetDataHandler(Symbol symbol, Action<BaseData> handler)
        {
            var dataHandler = new DataHandler(symbol);
            dataHandler.DataEmitted += (sender, args) => handler(args);
            _dataHandlers[symbol] = dataHandler;
        }

        /// <summary>
        /// Removes the handler with the specified identifier
        /// </summary>
        /// <param name="symbol">The symbol to remove handlers for</param>
        public bool RemoveDataHandler(Symbol symbol)
        {
            DataHandler handler;
            return _dataHandlers.TryRemove(symbol, out handler);
        }

        /// <summary>
        /// Begins consumption of the wrapped <see cref="IDataQueueHandler"/> on
        /// a separate thread
        /// </summary>
        /// <param name="token">A cancellation token used to signal to stop</param>
        public void Start(CancellationToken? token = null)
        {
            _isStopping = false;
            ConsumeEnumerators(token ?? CancellationToken.None);
        }

        /// <summary>
        /// Ends consumption of the wrapped <see cref="IDataQueueHandler"/>
        /// </summary>
        public void Stop()
        {
            _isStopping = true;
        }

        /// <summary> Entry point for queue consumption </summary>
        /// <param name="token">A cancellation token used to signal to stop</param>
        /// <remarks> This function only returns after <see cref="Stop"/> is called or the token is cancelled</remarks>
        private void ConsumeEnumerators(CancellationToken token)
        {
            while (true)
            {
                if (_isStopping || token.IsCancellationRequested)
                {
                    _isStopping = true;
                    Log.Trace("BaseDataExchange.ConsumeQueue(): Exiting...");
                    return;
                }

                try
                {
                    // call move next each enumerator and invoke the appropriate handlers

                    var handled = false;
                    foreach (var kvpe in _enumeratorHandlers)
                    {
                        var enumerator = kvpe.Key;
                        var enumeratorHandler = kvpe.Value;

                        // check to see if we should advance this enumerator
                        if (!enumeratorHandler.ShouldMoveNext()) continue;

                        if (!enumerator.MoveNext())
                        {
                            enumeratorHandler.OnEnumeratorFinished();

                            // remove dead enumerators
                            _enumeratorHandlers.TryRemove(enumerator, out enumeratorHandler);
                            continue;
                        }
                        
                        if (enumerator.Current == null) continue;

                        // invoke the correct handler
                        DataHandler dataHandler;
                        if (_dataHandlers.TryGetValue(enumerator.Current.Symbol, out dataHandler))
                        {
                            handled = true;
                            dataHandler.OnDataEmitted(enumerator.Current);
                        }
                    }

                    // if we didn't handle anything on this past iteration, take a nap
                    if (!handled && _sleepInterval != 0)
                    {
                        Thread.Sleep(_sleepInterval);
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    if (_isFatalError(err))
                    {
                        Log.Trace("BaseDataExchange.ConsumeQueue(): Fatal error encountered. Exiting...");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Handler used to handle data emitted from enumerators
        /// </summary>
        public class DataHandler
        {
            /// <summary>
            /// Event fired when MoveNext returns true and Current is non-null
            /// </summary>
            public event EventHandler<BaseData> DataEmitted;

            /// <summary>
            /// The symbol this handler handles
            /// </summary>
            public readonly Symbol Symbol;

            /// <summary>
            /// Initializes a new instance of the <see cref="DataHandler"/> class
            /// </summary>
            /// <param name="symbol">The symbol whose data is to be handled</param>
            public DataHandler(Symbol symbol)
            {
                Symbol = symbol;
            }

            /// <summary>
            /// Event invocator for the <see cref="DataEmitted"/> event
            /// </summary>
            /// <param name="data">The data being emitted</param>
            public void OnDataEmitted(BaseData data)
            {
                var handler = DataEmitted;
                if (handler != null) handler(this, data);
            }
        }

        /// <summary>
        /// Handler used to manage a single enumerator's move next/end of stream behavior
        /// </summary>
        public class EnumeratorHandler
        {
            /// <summary>
            /// Event fired when MoveNext returns false
            /// </summary>
            public event EventHandler<EnumeratorHandler> EnumeratorFinished;

            /// <summary>
            /// The enumerator this handler handles
            /// </summary>
            public readonly IEnumerator<BaseData> Enumerator;

            /// <summary>
            /// Predicate function used to determine if we should move next on the handled enumerator
            /// </summary>
            public readonly Func<bool> ShouldMoveNext;

            /// <summary>
            /// Initializes a new instance of the <see cref="EnumeratorHandler"/> class
            /// </summary>
            /// <param name="enumerator">The enumeator this handler handles</param>
            /// <param name="shouldMoveNext">Predicate function used to determine if we should call move next
            /// on the symbol's enumerator</param>
            public EnumeratorHandler(IEnumerator<BaseData> enumerator, Func<bool> shouldMoveNext = null)
            {
                Enumerator = enumerator;
                ShouldMoveNext = shouldMoveNext ?? (() => true);
            }

            /// <summary>
            /// Event invocator for the <see cref="EnumeratorFinished"/> event
            /// </summary>
            public void OnEnumeratorFinished()
            {
                var handler = EnumeratorFinished;
                if (handler != null) handler(this, this);
            }
        }
    }
}