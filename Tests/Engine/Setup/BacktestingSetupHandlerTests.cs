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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Util;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Tests.Engine.Setup
{
    [TestFixture]
    public class BacktestingSetupHandlerTests
    {
        private IAlgorithm _algorithm;
        private DataManager _dataManager;

        [OneTimeSetUp]
        public void Setup()
        {
            _algorithm = new TestAlgorithmThrowsOnInitialize();
            _dataManager = new DataManagerStub(_algorithm);
            _algorithm.SubscriptionManager.SetDataManager(_dataManager);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _dataManager.RemoveAllSubscriptions();
        }

        [Test]
        public void HandlesErrorOnInitializeCorrectly()
        {
            using var setupHandler = new BacktestingSetupHandler();

            var packet = new BacktestNodePacket();
            packet.Controls.RamAllocation = 1024 * 4;
            var realTimeHandler = new BacktestingRealTimeHandler();
            var resultHandler = new TestResultHandler();
            Assert.IsFalse(setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, _algorithm, null, packet,
                resultHandler, null, realTimeHandler, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider)));

            resultHandler.Exit();
            realTimeHandler.Exit();
            setupHandler.DisposeSafely();
            Assert.AreEqual(1, setupHandler.Errors.Count);
            Assert.IsTrue(setupHandler.Errors[0].InnerException.Message.Equals("Some failure", StringComparison.OrdinalIgnoreCase));
        }

        internal class TestAlgorithmThrowsOnInitialize : AlgorithmStub
        {
            public override void Initialize()
            {
                SetAccountCurrency("USDT");

                SetStartDate(2018, 08, 17);
                SetEndDate(2021, 11, 15);

                // this will fail later because due to default crypto market being Coinbase there is no conversion rate route
                var symbols = new[] { "ADAUSDT", "BNBUSDT", "BTCUSDT", "ETHUSDT", "LTCUSDT", "SOLUSDT" }
                    .Select(ticker => QuantConnect.Symbol.Create(ticker, SecurityType.Crypto, Market.Binance));
                SetUniverseSelection(new ManualUniverseSelectionModel(symbols));

                SetBenchmark("BTCUSDT");

                throw new Exception("Some failure");
            }
        }
    }
}
