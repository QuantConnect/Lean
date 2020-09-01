/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Util;
using System;
using System.Linq;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void ComposeDataQueueHandlerInstances()
        {
            var type = typeof(IDataQueueHandler);

            var types = AppDomain.CurrentDomain.Load("QuantConnect.ToolBox")
                .GetTypes()
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract)
                .ToList();

            Assert.NotZero(types.Count);

            types.ForEach(t =>
            {
                Assert.NotNull(t.GetConstructor(Type.EmptyTypes));
            });            
        }
    }
}
