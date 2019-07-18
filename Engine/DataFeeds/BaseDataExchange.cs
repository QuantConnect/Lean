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
using System.Linq;
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
        private readonly object _enumeratorsWriteLock = new object();
        private readonly ConcurrentDictionary<Symbol, DataHandler> _dataHandlers;
        private ConcurrentDictionary<Symbol, EnumeratorHandler> _enumerators;

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
        /// <param name="name">A name for this exchange</param>
        public BaseDataExchange(string name)
        {
            _name = name;
            _isFatalError = x => false;
            _dataHandlers = new ConcurrentDictionary<Symbol, DataHandler>();
            _enumerators = new ConcurrentDictionary<Symbol, EnumeratorHandler>();
        }

        /// <summary>
        /// Adds the enumerator to this exchange. If it has already been added
        /// then it will remain registered in the exchange only once
        /// </summary>
        /// <param name="handler">The handler to use when this symbol's data is encountered</param>
        public void AddEnumerator(EnumeratorHandler handler)
        {
            _enumerators[handler.Symbol] = handler;
        }

        /// <summary>
        /// Adds the enumerator to this exchange. If it has already been added
        /// then it will remain registered in the exchange only once
        /// </summary>
        /// <param name="symbol">A unique symbol used to identify this enumerator</param>
        /// <param name="enumerator">The enumerator to be added</param>
        /// <param name="shouldMoveNext">Function used to determine if move next should be called on this
        /// enumerator, defaults to always returning true</param>
        /// <param name="enumeratorFinished">Delegate called when the enumerator move next returns false</param>
        public void AddEnumerator(Symbol symbol, IEnumerator<BaseData> enumerator, Func<bool> shouldMoveNext = null, Action<EnumeratorHandler> enumeratorFinished = null)
        {
            var enumeratorHandler = new EnumeratorHandler(symbol, enumerator, shouldMoveNext);
            if (enumeratorFinished != null)
            {
                enumeratorHandler.EnumeratorFinished += (sender, args) => enumeratorFinished(args);
            }
            AddEnumerator(enumeratorHandler);
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
            SetDataHandler(dataHandler);
        }

        /// <summary>
        /// Adds the specified hander function to handle data for the handler's symbol
        /// </summary>
        /// <param name="symbol">The symbol whose data is to be handled</param>
        /// <param name="handler">The handler to use when this symbol's data is encountered</param>
        /// <returns>An identifier that can be used to remove this handler</returns>
        public void AddDataHandler(Symbol symbol, Action<BaseData> handler)
        {
            _dataHandlers.AddOrUpdate(symbol,
                x =>
                {
                    var dataHandler = new DataHandler(symbol);
                    dataHandler.DataEmitted += (sender, args) => handler(args);
                    return dataHandler;
                },
                (x, existingHandler) =>
                {
                    existingHandler.DataEmitted += (sender, args) => handler(args);
                    return existingHandler;
                });
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
        /// Removes and returns enumerator handler with the specified symbol.
        /// The removed handler is returned, null if not found
        /// </summary>
        public EnumeratorHandler RemoveEnumerator(Symbol symbol)
        {
            EnumeratorHandler handler;
            if (_enumerators.TryRemove(symbol, out handler))
            {
                handler.OnEnumeratorFinished();
                handler.Enumerator.Dispose();
            }
            return handler;
        }

        /// <summary>
        /// Begins consumption of the wrapped <see cref="IDataQueueHandler"/> on
        /// a separate thread
        /// </summary>
        /// <param name="token">A cancellation token used to signal to stop</param>
        public void Start(CancellationToken? token = null)
        {
            Log.Trace("BaseDataExchange({0}) Starting...", Name);
            _isStopping = false;
            ConsumeEnumerators(token ?? CancellationToken.None);
        }

        /// <summary>
        /// Ends consumption of the wrapped <see cref="IDataQueueHandler"/>
        /// </summary>
        public void Stop()
        {
            Log.Trace("BaseDataExchange({0}) Stopping...", Name);
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
                    var request = token.IsCancellationRequested ? "Cancellation requested" : "Stop requested";
                    Log.Trace("BaseDataExchange({0}).ConsumeQueue(): {1}.  Exiting...", Name, request);
                    return;
                }

                try
                {
                    // call move next each enumerator and invoke the appropriate handlers

                    var handled = false;
                    foreach (var kvp in _enumerators)
                    {
                        if (_isStopping)
                        {
                            break;
                        }
                        var enumeratorHandler = kvp.Value;
                        var enumerator = enumeratorHandler.Enumerator;

                        // check to see if we should advance this enumerator
                        if (!enumeratorHandler.ShouldMoveNext()) continue;

                        if (!enumerator.MoveNext())
                        {
                            enumeratorHandler.OnEnumeratorFinished();
                            enumeratorHandler.Enumerator.Dispose();
                            _enumerators.TryRemove(enumeratorHandler.Symbol, out enumeratorHandler);
                            continue;
                        }

                        if (enumerator.Current == null) continue;

                        // if the enumerator is configured to handle it, then do it, don't pass to data handlers
                        if (enumeratorHandler.HandlesData)
                        {
                            handled = true;
                            enumeratorHandler.HandleData(enumerator.Current);
                            continue;
                        }

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
                        Log.Trace("BaseDataExchange({0}).ConsumeQueue(): Fatal error encountered. Exiting...", Name);
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
            private readonly Func<bool> _shouldMoveNext;
            private readonly Action<BaseData> _handleData;

            /// <summary>
            /// Event fired when MoveNext returns false
            /// </summary>
            public event EventHandler<EnumeratorHandler> EnumeratorFinished;

            /// <summary>
            /// A unique symbol used to identify this enumerator
            /// </summary>
            public readonly Symbol Symbol;

            /// <summary>
            /// The enumerator this handler handles
            /// </summary>
            public readonly IEnumerator<BaseData> Enumerator;

            /// <summary>
            /// Determines whether or not this handler is to be used for handling the
            /// data emitted. This is useful when enumerators are not for a single symbol,
            /// such is the case with universe subscriptions
            /// </summary>
            public readonly bool HandlesData;

            /// <summary>
            /// Initializes a new instance of the <see cref="EnumeratorHandler"/> class
            /// </summary>
            /// <param name="symbol">The symbol to identify this enumerator</param>
            /// <param name="enumerator">The enumeator this handler handles</param>
            /// <param name="shouldMoveNext">Predicate function used to determine if we should call move next
            /// on the symbol's enumerator</param>
            /// <param name="handleData">Handler for data if HandlesData=true</param>
            public EnumeratorHandler(Symbol symbol, IEnumerator<BaseData> enumerator, Func<bool> shouldMoveNext = null, Action<BaseData> handleData = null)
            {
                Symbol = symbol;
                Enumerator = enumerator;
                HandlesData = handleData != null;

                _handleData = handleData ?? (data => { });
                _shouldMoveNext = shouldMoveNext ?? (() => true);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="EnumeratorHandler"/> class
            /// </summary>
            /// <param name="symbol">The symbol to identify this enumerator</param>
            /// <param name="enumerator">The enumeator this handler handles</param>
            /// <param name="handlesData">True if this handler will handle the data, false otherwise</param>
            protected EnumeratorHandler(Symbol symbol, IEnumerator<BaseData> enumerator, bool handlesData)
            {
                Symbol = symbol;
                HandlesData = handlesData;
                Enumerator = enumerator;

                _handleData = data => { };
                _shouldMoveNext = () => true;
            }

            /// <summary>
            /// Event invocator for the <see cref="EnumeratorFinished"/> event
            /// </summary>
            public virtual void OnEnumeratorFinished()
            {
                var handler = EnumeratorFinished;
                if (handler != null) handler(this, this);
            }

            /// <summary>
            /// Returns true if this enumerator should move next
            /// </summary>
            public virtual bool ShouldMoveNext()
            {
                return _shouldMoveNext();
            }

            /// <summary>
            /// Handles the specified data.
            /// </summary>
            /// <param name="data">The data to be handled</param>
            public virtual void HandleData(BaseData data)
            {
                _handleData(data);
            }
        }
    }
}