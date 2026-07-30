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
using System.Threading;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Disables brokerage account mutation services when the algorithm data stream ends.
    /// </summary>
    internal sealed class BrokerageAccountMutationReadinessSynchronizer : ISynchronizer
    {
        private readonly ISynchronizer _synchronizer;
        private readonly Action _disableMutationServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageAccountMutationReadinessSynchronizer"/> class.
        /// </summary>
        /// <param name="synchronizer">The algorithm data-stream synchronizer.</param>
        /// <param name="disableMutationServices">
        /// The callback that makes brokerage account mutation services unavailable.
        /// </param>
        public BrokerageAccountMutationReadinessSynchronizer(
            ISynchronizer synchronizer,
            Action disableMutationServices)
        {
            ArgumentNullException.ThrowIfNull(synchronizer);
            ArgumentNullException.ThrowIfNull(disableMutationServices);

            _synchronizer = synchronizer;
            _disableMutationServices = disableMutationServices;
        }

        /// <summary>
        /// Streams data and disables brokerage account mutation services before algorithm teardown begins.
        /// </summary>
        public IEnumerable<TimeSlice> StreamData(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var timeSlice in _synchronizer.StreamData(cancellationToken))
                {
                    yield return timeSlice;
                }
            }
            finally
            {
                _disableMutationServices();
            }
        }
    }
}
