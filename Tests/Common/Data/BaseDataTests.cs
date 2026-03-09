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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class BaseDataTests
    {
        [Test]
        public void IsSparseData_ReturnsTrue_WhenSecurityTypeIsBase()
        {
            var baseData = new DataType {Symbol = Symbol.Create("ticker", SecurityType.Base, QuantConnect.Market.USA)};
            Assert.IsTrue(baseData.IsSparseData());
        }

        [Test]
        public void IsSparseData_ReturnsFalse_WhenSecurityTypeIsNotBase()
        {
            var securityTypes = Enum.GetValues(typeof(SecurityType))
                .Cast<SecurityType>()
                .Where(type => type != SecurityType.Base);

            foreach (var securityType in securityTypes)
            {
                var baseData = new DataType();

                try { baseData.Symbol = Symbol.Create("ticker", securityType, QuantConnect.Market.USA); }
                catch (NotImplementedException) { continue; }

                Assert.IsFalse(baseData.IsSparseData(), securityType.ToString());
            }
        }

        private class DataType : BaseData { }
    }
}
