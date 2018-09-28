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
using QuantConnect.Algorithm;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    internal class DataManagerStub : DataManager
    {
        public DataManagerStub()
            : this(new QCAlgorithm())
        {

        }

        public DataManagerStub(TimeKeeper timeKeeper)
            : this(new QCAlgorithm(), timeKeeper)
        {

        }

        public DataManagerStub(IAlgorithm algorithm)
            : this(new NullDataFeed(), algorithm, new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
        {

        }

        public DataManagerStub(IAlgorithm algorithm, TimeKeeper timeKeeper)
            : this(new NullDataFeed(), algorithm, timeKeeper)
        {

        }

        public DataManagerStub(IDataFeed dataFeed, IAlgorithm algorithm, TimeKeeper timeKeeper)
            : base(dataFeed, new UniverseSelection(dataFeed, algorithm), algorithm.Settings, timeKeeper)
        {

        }
    }
}
