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
using System.Threading.Tasks;
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
        private Func<Exception, bool> _isFatalError;

        private readonly string _name;
        private readonly Thread _thread;
        private readonly ConcurrentDictionary<Symbol, Handler> _handlers;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // using concurrent dictionary for fast/easy contains/remove, the int value is nothingness
        private readonly ConcurrentDictionary<IEnumerator<BaseData>, int> _enumerators;

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
        public BaseDataExchange(IEnumerable<IEnumerator<BaseData>> enumerators)
            : this(string.Empty, enumerators.ToArray())
        {
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
        public BaseDataExchange(string name, IEnumerable<IEnumerator<BaseData>> enumerators)
            : this(name, enumerators.ToArray())
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
            _thread = new Thread(ConsumeEnumerators);
            _enumerators = new ConcurrentDictionary<IEnumerator<BaseData>, int>();
            foreach (var enumerator in enumerators)
            {
                // enumerators added via ctor are always called
                _enumerators.AddOrUpdate(enumerator, 0);
            }
            _isFatalError = x => false;
            _cancellationTokenSource = new CancellationTokenSource();
            _handlers = new ConcurrentDictionary<Symbol, Handler>();
        }

        /// <summary>
        /// Adds the enumerator to this exchange. If it has already been added
        /// then it will remain registered in the exchange only once
        /// </summary>
        /// <param name="enumerator">The enumerator to be added</param>
        public void AddEnumerator(IEnumerator<BaseData> enumerator)
        {
            _enumerators.TryAdd(enumerator, 0);
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
        /// Sets the specified hander function to handle data for the symbol
        /// </summary>
        /// <param name="symbol">The symbol to remove handlers for</param>
        /// <param name="handler">The handler to use when this symbol's data is encountered</param>
        /// <returns>An identifier that can be used to remove this handler</returns>
        public void SetHandler(Symbol symbol, Action<BaseData> handler)
        {
            _handlers[symbol] = new Handler(symbol, handler);
        }

        /// <summary>
        /// Removes the handler with the specified identifier
        /// </summary>
        /// <param name="symbol">The symbol to remove handlers for</param>
        public bool RemoveHandler(Symbol symbol)
        {
            Handler handler;
            return _handlers.TryRemove(symbol, out handler);
        }

        /// <summary>
        /// Begins consumption of the wrapped <see cref="IDataQueueHandler"/> on
        /// a separate thread
        /// </summary>
        public void Start()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                throw new Exception("This exchange has been requested to stop already.");
            }

            _thread.Start();
        }

        /// <summary>
        /// Ends consumption of the wrapped <see cref="IDataQueueHandler"/>
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource.Cancel();

            // check back in 25 seconds, if thread is still running, abort
            Task.Delay(TimeSpan.FromSeconds(25)).ContinueWith(t =>
            {
                if (_thread.IsAlive)
                {
                    Log.Trace(string.Format("BaseDataExchange.Stop({0}): Force abort thread.", _name));
                    _thread.Abort();
                }
            });
        }

        /// <summary> Entry point for queue consumption </summary>
        /// <remarks> This function only return after <see cref="Stop"/> is called </remarks>
        private void ConsumeEnumerators()
        {
            while (true)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    Log.Trace("DataQueueHandlerExchange.ConsumeQueue(): Exiting...");
                    return;
                }

                try
                {
                    // call move next each enumerator and invoke the appropriate handlers

                    var handled = false;
                    foreach (var kvpe in _enumerators)
                    {
                        var enumerator = kvpe.Key;

                        if (!enumerator.MoveNext())
                        {
                            // remove dead enumerators
                            int state;
                            _enumerators.TryRemove(enumerator, out state);
                            continue;
                        }
                        
                        if (enumerator.Current == null) continue;

                        // invoke the correct handler
                        Handler handler;
                        if (_handlers.TryGetValue(enumerator.Current.Symbol, out handler))
                        {
                            handled = true;
                            handler.Handle(enumerator.Current);
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
                        Log.Trace("DataQueueHandlerExchange.ConsumeQueue(): Fatal error encountered. Exiting...");
                        return;
                    }
                }
            }
        }

        private class Handler
        {
            public readonly Symbol Symbol;
            public readonly Action<BaseData> Handle;

            public Handler(Symbol symbol, Action<BaseData> handle)
            {
                Symbol = symbol;
                Handle = handle;
            }
        }
    }
}