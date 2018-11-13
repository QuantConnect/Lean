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
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class DowngradeErrorCodeToWarningBrokerageMessageHandlerTests
    {
        [Test]
        [TestCase(BrokerageMessageType.Information)]
        [TestCase(BrokerageMessageType.Warning)]
        [TestCase(BrokerageMessageType.Disconnect)]
        [TestCase(BrokerageMessageType.Reconnect)]
        public void PatchesNonErrorMessagesToWrappedImplementation(BrokerageMessageType type)
        {
            var wrapped = new Mock<IBrokerageMessageHandler>();
            wrapped.Setup(bmh => bmh.Handle(It.IsAny<BrokerageMessageEvent>())).Verifiable();

            var downgrader = new DowngradeErrorCodeToWarningBrokerageMessageHandler(wrapped.Object, new[] { "code" });
            var message = new BrokerageMessageEvent(type, "code", "message");
            downgrader.Handle(message);

            wrapped.Verify(bmh => bmh.Handle(message), Times.Once);
        }

        [Test]
        public void PatchesErrorMessageNotMatchingCodeToWrappedImplementation()
        {
            var wrapped = new Mock<IBrokerageMessageHandler>();
            wrapped.Setup(bmh => bmh.Handle(It.IsAny<BrokerageMessageEvent>())).Verifiable();

            var downgrader = new DowngradeErrorCodeToWarningBrokerageMessageHandler(wrapped.Object, new[] { "code" });
            var message = new BrokerageMessageEvent(BrokerageMessageType.Error, "not-code", "message");
            downgrader.Handle(message);

            wrapped.Verify(bmh => bmh.Handle(message), Times.Once);
        }

        [Test]
        public void RewritesErrorMessageMatchingCodeAsWarning()
        {
            var wrapped = new Mock<IBrokerageMessageHandler>();
            wrapped.Setup(bmh => bmh.Handle(It.IsAny<BrokerageMessageEvent>())).Verifiable();

            var downgrader = new DowngradeErrorCodeToWarningBrokerageMessageHandler(wrapped.Object, new[] { "code" });
            var message = new BrokerageMessageEvent(BrokerageMessageType.Error, "code", "message");
            downgrader.Handle(message);

            // verify we converter the message to a warning message w/ the same message and code
            wrapped.Verify(
                bmh => bmh.Handle(
                    It.Is<BrokerageMessageEvent>(
                        e => e.Type == BrokerageMessageType.Warning
                            && e.Message == message.Message
                            && e.Code == message.Code
                    )
                ),
                Times.Once
            );
        }
    }
}
