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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> to modify the
    /// enumerator after creation
    /// </summary>
    public class PostCreateConfigureSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly ISubscriptionEnumeratorFactory _factory;
        private readonly Func<IEnumerator<BaseData>, IEnumerator<BaseData>> _configurator;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostCreateConfigureSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="factory">The factory being wrapped</param>
        /// <param name="configurator">The configuration to be applied</param>
        public PostCreateConfigureSubscriptionEnumeratorFactory(ISubscriptionEnumeratorFactory factory, Func<IEnumerator<BaseData>, IEnumerator<BaseData>> configurator)
        {
            _factory = factory;
            _configurator = configurator;
        }

        /// <summary>
        /// Invokes the configuration following enumerator creation
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request)
        {
            return _configurator(_factory.CreateEnumerator(request));
        }
    }
}