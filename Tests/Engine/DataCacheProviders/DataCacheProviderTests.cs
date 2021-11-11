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

using NUnit.Framework;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Engine.DataCacheProviders
{
    /// <summary>
    /// Abstract class the provides common test cases for all DataCacheProviders
    /// </summary>
    public abstract class DataCacheProviderTests
    {
        protected IDataCacheProvider DataCacheProvider;

        [OneTimeSetUp]
        public void Setup()
        {
            DataCacheProvider = CreateDataCacheProvider();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            DataCacheProvider.Dispose();
        }

        public abstract IDataCacheProvider CreateDataCacheProvider();

        [Test]
        public void CanFetchDataThatExists()
        {
            var stream = DataCacheProvider.Fetch("../../../Data/equity/usa/minute/aapl/20140606_trade.zip");

            Assert.IsNotNull(stream);
            stream.Dispose();
        }

        [Test]
        public void CannotFetchDataThatDoesNotExist()
        {
            var stream = DataCacheProvider.Fetch("../../../Data/equity/usa/minute/aapl/19980606_trade.zip");

            Assert.IsNull(stream);
        }
    }
}
