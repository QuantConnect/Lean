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
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides a means of distributing output from enumerators from a dedicated separate thread
    /// </summary>
    public class BaseDataExchange
    {
        private Thread _thread;
        private uint _sleepInterval = 1;
        private Func<Exception, bool> _isFatalError;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly string _name;
        private ManualResetEventSlim _manualResetEventSlim;
        private ConcurrentDictionary<Symbol, EnumeratorHandler> _enumerators;

        /// <summary>
        /// Gets or sets how long this thread will sleep when no data is available
        /// </summary>
        public uint SleepInterval
        {
            get => _sleepInterval;
            set
            {
                if (value == 0)
                {
                    throw new ArgumentException("Sleep interval should be bigger than 0");
                }
                _sleepInterval = value;
            }
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
            _cancellationTokenSource = new CancellationTokenSource();
            _manualResetEventSlim = new ManualResetEventSlim(false);
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
            _manualResetEventSlim.Set();
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
        /// <param name="handleData">Handler for data if HandlesData=true</param>
        public void AddEnumerator(
            Symbol symbol,
            IEnumerator<BaseData> enumerator,
            Func<bool> shouldMoveNext = null,
            Action<EnumeratorHandler> enumeratorFinished = null,
            Action<BaseData> handleData = null
        )
        {
            var enumeratorHandler = new EnumeratorHandler(
                symbol,
                enumerator,
                shouldMoveNext,
                handleData
            );
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
        public void Start()
        {
            var manualEvent = new ManualResetEventSlim(false);
            _thread = new Thread(() =>
            {
                manualEvent.Set();
                Log.Trace($"BaseDataExchange({Name}) Starting...");
                ConsumeEnumerators();
            })
            {
                IsBackground = true,
                Name = Name
            };
            _thread.Start();

            manualEvent.Wait();
            manualEvent.DisposeSafely();
        }

        /// <summary>
        /// Ends consumption of the wrapped <see cref="IDataQueueHandler"/>
        /// </summary>
        public void Stop()
        {
            _thread.StopSafely(TimeSpan.FromSeconds(5), _cancellationTokenSource);
        }

        /// <summary> Entry point for queue consumption </summary>
        /// <param name="token">A cancellation token used to signal to stop</param>
        /// <remarks> This function only returns after <see cref="Stop"/> is called or the token is cancelled</remarks>
        private void ConsumeEnumerators()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // call move next each enumerator and invoke the appropriate handlers
                    _manualResetEventSlim.Reset();
                    var handled = false;
                    foreach (var kvp in _enumerators)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            Log.Trace($"BaseDataExchange({Name}).ConsumeQueue(): Exiting...");
                            return;
                        }
                        var enumeratorHandler = kvp.Value;
                        var enumerator = enumeratorHandler.Enumerator;

                        // check to see if we should advance this enumerator
                        if (!enumeratorHandler.ShouldMoveNext())
                            continue;

                        if (!enumerator.MoveNext())
                        {
                            enumeratorHandler.OnEnumeratorFinished();
                            enumeratorHandler.Enumerator.Dispose();
                            _enumerators.TryRemove(enumeratorHandler.Symbol, out enumeratorHandler);
                            continue;
                        }

                        if (enumerator.Current == null)
                            continue;

                        handled = true;
                        enumeratorHandler.HandleData(enumerator.Current);
                    }

                    if (!handled)
                    {
                        // if we didn't handle anything on this past iteration, take a nap
                        // wait until we timeout, we are cancelled or there is a new enumerator added
                        _manualResetEventSlim.Wait(
                            Time.GetSecondUnevenWait((int)_sleepInterval),
                            _cancellationTokenSource.Token
                        );
                    }
                }
                catch (OperationCanceledException)
                {
                    // thrown by the event watcher
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    if (_isFatalError(err))
                    {
                        Log.Trace(
                            $"BaseDataExchange({Name}).ConsumeQueue(): Fatal error encountered. Exiting..."
                        );
                        return;
                    }
                }
            }

            Log.Trace($"BaseDataExchange({Name}).ConsumeQueue(): Exiting...");
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
            public Symbol Symbol { get; init; }

            /// <summary>
            /// The enumerator this handler handles
            /// </summary>
            public IEnumerator<BaseData> Enumerator { get; init; }

            /// <summary>
            /// Initializes a new instance of the <see cref="EnumeratorHandler"/> class
            /// </summary>
            /// <param name="symbol">The symbol to identify this enumerator</param>
            /// <param name="enumerator">The enumeator this handler handles</param>
            /// <param name="shouldMoveNext">Predicate function used to determine if we should call move next
            /// on the symbol's enumerator</param>
            /// <param name="handleData">Handler for data if HandlesData=true</param>
            public EnumeratorHandler(
                Symbol symbol,
                IEnumerator<BaseData> enumerator,
                Func<bool> shouldMoveNext = null,
                Action<BaseData> handleData = null
            )
            {
                Symbol = symbol;
                Enumerator = enumerator;

                _handleData = handleData;
                _shouldMoveNext = shouldMoveNext ?? (() => true);
            }

            /// <summary>
            /// Event invocator for the <see cref="EnumeratorFinished"/> event
            /// </summary>
            public void OnEnumeratorFinished()
            {
                EnumeratorFinished?.Invoke(this, this);
            }

            /// <summary>
            /// Returns true if this enumerator should move next
            /// </summary>
            public bool ShouldMoveNext()
            {
                return _shouldMoveNext();
            }

            /// <summary>
            /// Handles the specified data.
            /// </summary>
            /// <param name="data">The data to be handled</param>
            public void HandleData(BaseData data)
            {
                _handleData?.Invoke(data);
            }
        }
    }
}
