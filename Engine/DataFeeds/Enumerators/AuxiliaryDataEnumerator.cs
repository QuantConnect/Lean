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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;

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
            ITradableDateEventProvider []tradableDateEventProviders,
            ITradableDatesNotifier tradableDayNotifier,
            DateTime startTime)
        {
            _auxiliaryData = new Queue<BaseData>();

            tradableDayNotifier.NewTradableDate += (sender, eventArgs) =>
            {
                if (!_initialized)
                {
                    Initialize(config, factorFileProvider, mapFileProvider, tradableDateEventProviders, startTime);
                }

                foreach (var tradableDateEventProvider in tradableDateEventProviders)
                {
                    var newEvents = tradableDateEventProvider.GetEvents(eventArgs).ToList();
                    foreach (var newEvent in newEvents)
                    {
                        _auxiliaryData.Enqueue(newEvent);
                    }
                }
            };
        }

        /// <summary>
        /// Late initialization so it is performed in the data feed stack
        /// and not in the algorithm thread
        /// </summary>
        private void Initialize(SubscriptionDataConfig config,
            IFactorFileProvider factorFileProvider,
            IMapFileProvider mapFileProvider,
            ITradableDateEventProvider[] tradableDateEventProviders,
            DateTime startTime)
        {
            foreach (var tradableDateEventProvider in tradableDateEventProviders)
            {
                tradableDateEventProvider.Initialize(
                    config,
                    factorFileProvider,
                    mapFileProvider,
                    startTime);
            }
            _initialized = true;
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
        /// Dispose of the Stream Reader and close out the source stream and file connections.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Reset the IEnumeration
        /// </summary>
        /// <remarks>Not used</remarks>
        public void Reset()
        {
            throw new NotImplementedException("Reset method not implemented. Assumes loop will only be used once.");
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Last read BaseData object from this type and source
        /// </summary>
        public BaseData Current
        {
            get;
            private set;
        }
    }
}
