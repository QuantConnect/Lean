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

using Moq;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Transport;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.IO;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class SubscriptionDataSourceTests
    {
        private Mock<Stream> _stream;
        private Mock<IDataCacheProvider> _dataCacheProvider;
        private Mock<IDownloadProvider> _downloadProvider;

        [SetUp]
        public void Given()
        {
            _stream = new Mock<Stream>();
            _stream.SetupGet(s => s.CanRead).Returns(true);

            _dataCacheProvider = new Mock<IDataCacheProvider>();
            _dataCacheProvider.Setup(d => d.Fetch(It.IsAny<string>()))
                                .Returns(_stream.Object);

            _downloadProvider = new Mock<IDownloadProvider>();
            _downloadProvider.Setup(d => d.Download(It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<string>(), It.IsAny<string>()))
                                .Returns("test");

            RemoteFileSubscriptionStreamReader.SetDownloadProvider(_downloadProvider.Object);
        }

        [Test]
        public void ComparesEqualWithIdenticalSourceAndTransportMediumLocalFileOldAndNewWay()
        {
            var one = new LocalFileSubscriptionDataSource("source");
            var two = new SubscriptionDataSource("source", SubscriptionTransportMedium.LocalFile);
            Assert.IsTrue(one == two);
            Assert.IsTrue(one.Equals(two));

            var oneStream = one.GetStreamReader(_dataCacheProvider.Object);
            var twoStream = two.GetStreamReader(_dataCacheProvider.Object);

            Assert.IsNotNull(oneStream);
            Assert.IsNotNull(twoStream);
            Assert.IsTrue(oneStream.GetType() == twoStream.GetType());
        }

        [Test]
        public void ComparesEqualWithIdenticalSourceAndTransportMediumRemoteFileOldAndNewWay()
        {
            var one = new RemoteFileSubscriptionDataSource("source");
            var two = new SubscriptionDataSource("source", SubscriptionTransportMedium.RemoteFile);
            Assert.IsTrue(one == two);
            Assert.IsTrue(one.Equals(two));

            var oneStream = one.GetStreamReader(_dataCacheProvider.Object);
            var twoStream = two.GetStreamReader(_dataCacheProvider.Object);

            Assert.IsNotNull(oneStream);
            Assert.IsNotNull(twoStream);
            Assert.IsTrue(oneStream.GetType() == twoStream.GetType());
        }

        [Test]
        public void ComparesEqualWithIdenticalSourceAndTransportMediumRestWebOldAndNewWay()
        {
            bool liveMode = Config.GetBool("live-mode");
            var one = new RestSubscriptionDataSource("http://localhost/source", liveMode);
            var two = new SubscriptionDataSource("http://localhost/source", SubscriptionTransportMedium.Rest);
            Assert.IsTrue(one == two);
            Assert.IsTrue(one.Equals(two));

            var oneStream = one.GetStreamReader(_dataCacheProvider.Object);
            var twoStream = two.GetStreamReader(_dataCacheProvider.Object);

            Assert.IsNotNull(oneStream);
            Assert.IsNotNull(twoStream);
            Assert.IsTrue(oneStream.GetType() == twoStream.GetType());
        }

        [Test]
        public void ComparesEqualWithIdenticalSourceAndTransportMedium()
        {
            var one = new LocalFileSubscriptionDataSource("source");
            var two = new SubscriptionDataSource("source", SubscriptionTransportMedium.LocalFile);
            Assert.IsTrue(one == two);
            Assert.IsTrue(one.Equals(two));
        }

        [Test]
        public void ComparesNotEqualWithDifferentSource()
        {
            var one = new LocalFileSubscriptionDataSource("source1");
            var two = new LocalFileSubscriptionDataSource("source2");
            Assert.IsTrue(one != two);
            Assert.IsTrue(!one.Equals(two));
        }

        [Test]
        public void ComparesNotEqualWithDifferentTransportMedium()
        {
            var one = new LocalFileSubscriptionDataSource("source");
            var two = new RemoteFileSubscriptionDataSource("source");
            Assert.IsTrue(one != two);
            Assert.IsTrue(!one.Equals(two));
        }
    }
}
