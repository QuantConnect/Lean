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
using System.IO;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds.DataDownloader;

namespace QuantConnect.Tests.Engine.DataFeeds.DataDownloader
{
    [TestFixture]
    public class DataDownloaderSelectorTests
    {
        private readonly IMapFileProvider _mapFileProvider = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>("LocalDiskMapFileProvider");

        [Test]
        public void GetDataDownloaderReturnsBaseDownloaderForNonLeanType()
        {
            var baseDownloader = new TestDataDownloader();
            using var selector = new DataDownloaderSelector(baseDownloader, _mapFileProvider, new TestDataProvider());

            var actualBaseDownloader = selector.GetDataDownloader(typeof(object));

            Assert.AreSame(baseDownloader, actualBaseDownloader);

            var actualCanonicalDownloader = selector.GetDataDownloader(typeof(TradeBar));

            Assert.AreEqual(typeof(CanonicalDataDownloaderDecorator), actualCanonicalDownloader.GetType());
        }

        private class TestDataDownloader : IDataDownloader
        {
            public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
            {
                throw new NotImplementedException();
            }
        }

        private class TestDataProvider : IDataProvider
        {
            public event EventHandler<DataProviderNewDataRequestEventArgs> NewDataRequest;

            public Stream Fetch(string key)
            {
                throw new NotImplementedException();
            }
        }
    }
}
