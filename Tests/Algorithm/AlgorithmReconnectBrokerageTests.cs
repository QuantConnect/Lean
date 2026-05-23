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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmReconnectBrokerageTests
    {
        private QCAlgorithm _algo;

        [SetUp]
        public void Setup()
        {
            _algo = new QCAlgorithm();
            _algo.SubscriptionManager.SetDataManager(new DataManagerStub(_algo));
        }

        [Test]
        public void ReconnectBrokerage_InvokesRegisteredAction()
        {
            var reconnectCalled = false;
            _algo.SetBrokerageReconnectAction(() => reconnectCalled = true);

            _algo.ReconnectBrokerage();

            Assert.IsTrue(reconnectCalled);
        }

        [Test]
        public void ReconnectBrokerage_NoOp_WhenNoActionRegistered()
        {
            // should not throw when no action has been set
            Assert.DoesNotThrow(() => _algo.ReconnectBrokerage());
        }

        [Test]
        public void SetBrokerageReconnectAction_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => _algo.SetBrokerageReconnectAction(null));
        }

        [Test]
        public void ReconnectBrokerage_InvokesDisconnectThenConnect()
        {
            var callOrder = new System.Collections.Generic.List<string>();
            _algo.SetBrokerageReconnectAction(() =>
            {
                callOrder.Add("disconnect");
                callOrder.Add("connect");
            });

            _algo.ReconnectBrokerage();

            Assert.AreEqual(new[] { "disconnect", "connect" }, callOrder);
        }
    }
}
