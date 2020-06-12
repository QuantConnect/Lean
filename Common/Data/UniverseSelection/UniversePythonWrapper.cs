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
*/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Python.Runtime;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Provides an implementation of <see cref="Universe"/> that wraps a <see cref="PyObject"/> object
    /// </summary>
    public class UniversePythonWrapper : Universe
    {
        private readonly dynamic _universe;

        /// <summary>
        /// Gets the settings used for subscriptions added for this universe
        /// </summary>
        public override UniverseSettings UniverseSettings
        {
            get
            {
                using (Py.GIL())
                {
                    return _universe.UniverseSettings;
                }
            }
        }

        /// <summary>
        /// Flag indicating if disposal of this universe has been requested
        /// </summary>
        public override bool DisposeRequested
        {
            get
            {
                using (Py.GIL())
                {
                    return _universe.DisposeRequested;
                }
            }
            protected set
            {
                using (Py.GIL())
                {
                    _universe.DisposeRequested = value;
                }
            }
        }

        /// <summary>
        /// Gets the configuration used to get universe data
        /// </summary>
        public override SubscriptionDataConfig Configuration
        {
            get
            {
                using (Py.GIL())
                {
                    return _universe.Configuration;
                }
            }
        }

        /// <summary>
        /// Gets the internal security collection used to define membership in this universe
        /// </summary>
        public override ConcurrentDictionary<Symbol, Member> Securities
        {
            get
            {
                using (Py.GIL())
                {
                    return _universe.Securities;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UniversePythonWrapper"/> class
        /// </summary>
        public UniversePythonWrapper(PyObject universe) : base(null)
        {
            _universe = universe;
        }

        /// <summary>
        /// Performs universe selection using the data specified
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            using (Py.GIL())
            {
                var symbols = _universe.SelectSymbols(utcTime, data) as PyObject;
                var iterator = symbols.GetIterator();
                foreach (PyObject symbol in iterator)
                {
                    yield return symbol.GetAndDispose<Symbol>();
                }
                iterator.Dispose();
                symbols.Dispose();
            }
        }

        /// <summary>
        /// Gets the subscription requests to be added for the specified security
        /// </summary>
        /// <param name="security">The security to get subscriptions for</param>
        /// <param name="currentTimeUtc">The current time in utc. This is the frontier time of the algorithm</param>
        /// <param name="maximumEndTimeUtc">The max end time</param>
        /// <param name="subscriptionService">Instance which implements <see cref="ISubscriptionDataConfigService"/> interface</param>
        /// <returns>All subscriptions required by this security</returns>
        public override IEnumerable<SubscriptionRequest> GetSubscriptionRequests(Security security, DateTime currentTimeUtc, DateTime maximumEndTimeUtc,
            ISubscriptionDataConfigService subscriptionService)
        {
            using (Py.GIL())
            {
                var subscriptionRequests = _universe.GetSubscriptionRequests(security, currentTimeUtc, maximumEndTimeUtc, subscriptionService) as PyObject;
                var iterator = subscriptionRequests.GetIterator();
                foreach (PyObject request in iterator)
                {
                    var subscriptionRequest = request.GetAndDispose<SubscriptionRequest>();
                    yield return new SubscriptionRequest(subscriptionRequest, universe:this);
                }
                iterator.Dispose();
                subscriptionRequests.Dispose();
            }
        }
    }
}
