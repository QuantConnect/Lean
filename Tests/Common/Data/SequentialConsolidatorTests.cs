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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class SequentialConsolidatorTests
    {
        [Test]
        public void SequentialConsolidatorsFiresAllEvents()
        {
            var first = new IdentityDataConsolidator<IBaseData>();
            var second = new IdentityDataConsolidator<IBaseData>();
            var sequential = new SequentialConsolidator(first, second);

            bool firstFired = false;
            bool secondFired = false;
            bool sequentialFired = false;

            first.DataConsolidated += (sender, consolidated) =>
            {
                firstFired = true;
            };

            second.DataConsolidated += (sender, consolidated) =>
            {
                secondFired = true;
            };

            sequential.DataConsolidated += (sender, consolidated) =>
            {
                sequentialFired = true;
            };

            sequential.Update(new TradeBar());

            Assert.IsTrue(firstFired);
            Assert.IsTrue(secondFired);
            Assert.IsTrue(sequentialFired);
        }

        [Test]
        public void SequentialConsolidatorAcceptsSubTypesForSecondInputType()
        {
            var first = new IdentityDataConsolidator<TradeBar>();
            var second = new IdentityDataConsolidator<IBaseData>();
            var sequential = new SequentialConsolidator(first, second);


            bool firstFired = false;
            bool secondFired = false;
            bool sequentialFired = false;

            first.DataConsolidated += (sender, consolidated) =>
            {
                firstFired = true;
            };

            second.DataConsolidated += (sender, consolidated) =>
            {
                secondFired = true;
            };

            sequential.DataConsolidated += (sender, consolidated) =>
            {
                sequentialFired = true;
            };

            sequential.Update(new TradeBar());

            Assert.IsTrue(firstFired);
            Assert.IsTrue(secondFired);
            Assert.IsTrue(sequentialFired);
        }
    }
}
