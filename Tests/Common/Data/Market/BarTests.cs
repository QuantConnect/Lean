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
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class BarTests
    {
        [Test]
        public void UpdatesProperly()
        {
            var bar = new Bar();
            bar.Update(10);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(10, bar.High);
            Assert.AreEqual(10, bar.Low);
            Assert.AreEqual(10, bar.Close);

            bar.Update(20);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(20, bar.High);
            Assert.AreEqual(10, bar.Low);
            Assert.AreEqual(20, bar.Close);

            bar.Update(5);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(20, bar.High);
            Assert.AreEqual(5, bar.Low);
            Assert.AreEqual(5, bar.Close);

            bar.Update(11);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(20, bar.High);
            Assert.AreEqual(5, bar.Low);
            Assert.AreEqual(11, bar.Close);
        }

        [Test]
        public void DoesNotHandleAssetsWithZeroPrice()
        {
            var bar = new Bar();
            bar.Update(10);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(10, bar.High);
            Assert.AreEqual(10, bar.Low);
            Assert.AreEqual(10, bar.Close);

            // no update performed
            bar.Update(0);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(10, bar.High);
            Assert.AreEqual(10, bar.Low);
            Assert.AreEqual(10, bar.Close);

            bar.Update(-5);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(10, bar.High);
            Assert.AreEqual(-5, bar.Low);
            Assert.AreEqual(-5, bar.Close);
            
            bar.Update(5);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(10, bar.High);
            Assert.AreEqual(-5, bar.Low);
            Assert.AreEqual(5, bar.Close);
            
            bar.Update(50);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(50, bar.High);
            Assert.AreEqual(-5, bar.Low);
            Assert.AreEqual(50, bar.Close);
        }
    }
}