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
using System.Collections;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Auxiliary data enumerator that will, initialize and call the <see cref="ITradableDateEventProvider.GetEvents"/>
    /// implementation each time there is a new tradable day for every <see cref="ITradableDateEventProvider"/>
    /// provided.
    /// </summary>
    public class AuxiliaryDataEnumerator : IEnumerator<BaseData>
    {
        private readonly Queue<BaseData> _auxiliaryData;
        private bool _initialized;
        private DateTime _startTime;
        private IMapFileProvider _mapFileProvider;
        private IFactorFileProvider _factorFileProvider;
        private ITradableDateEventProvider[] _tradableDateEventProviders;

        /// <summary>
        /// The associated data configuration
        /// </summary>
        protected SubscriptionDataConfig Config { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFileProvider">The factor file provider to use</param>
        /// <param name="mapFileProvider">The <see cref="MapFile"/> provider to use</param>
        /// <param name="tradableDateEventProviders">The tradable dates event providers</param>
        /// <param name="tradableDayNotifier">Tradable dates provider</param>
        /// <param name="startTime">Start date for the data request</param>
        public AuxiliaryDataEnumerator(
            SubscriptionDataConfig config,
            IFactorFileProvider factorFileProvider,
            IMapFileProvider mapFileProvider,
            ITradableDateEventProvider[] tradableDateEventProviders,
            ITradableDatesNotifier tradableDayNotifier,
            DateTime startTime
        )
        {
            Config = config;
            _startTime = startTime;
            _mapFileProvider = mapFileProvider;
            _auxiliaryData = new Queue<BaseData>();
            _factorFileProvider = factorFileProvider;
            _tradableDateEventProviders = tradableDateEventProviders;

            if (tradableDayNotifier != null)
            {
                tradableDayNotifier.NewTradableDate += NewTradableDate;
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns>Always true</returns>
        public virtual bool MoveNext()
        {
            Current = _auxiliaryData.Count != 0 ? _auxiliaryData.Dequeue() : null;
            return true;
        }

        /// <summary>
        /// Handle a new tradable date, drives the <see cref="ITradableDateEventProvider"/> instances
        /// </summary>
        protected void NewTradableDate(object sender, NewTradableDateEventArgs eventArgs)
        {
            Initialize();
            for (var i = 0; i < _tradableDateEventProviders.Length; i++)
            {
                foreach (var newEvent in _tradableDateEventProviders[i].GetEvents(eventArgs))
                {
                    _auxiliaryData.Enqueue(newEvent);
                }
            }
        }

        /// <summary>
        /// Initializes the underlying tradable data event providers
        /// </summary>
        protected void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                // Late initialization so it is performed in the data feed stack
                for (var i = 0; i < _tradableDateEventProviders.Length; i++)
                {
                    _tradableDateEventProviders[i]
                        .Initialize(Config, _factorFileProvider, _mapFileProvider, _startTime);
                }
            }
        }

        /// <summary>
        /// Dispose of the Stream Reader and close out the source stream and file connections.
        /// </summary>
        public void Dispose()
        {
            for (var i = 0; i < _tradableDateEventProviders.Length; i++)
            {
                var disposable = _tradableDateEventProviders[i] as IDisposable;
                disposable?.DisposeSafely();
            }
        }

        /// <summary>
        /// Reset the IEnumeration
        /// </summary>
        /// <remarks>Not used</remarks>
        public void Reset()
        {
            throw new NotImplementedException(
                "Reset method not implemented. Assumes loop will only be used once."
            );
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Last read BaseData object from this type and source
        /// </summary>
        public BaseData Current { get; private set; }
    }
}
