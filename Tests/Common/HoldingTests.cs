﻿/*
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
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class HoldingTests
    {
        [Test]
        public void SerializesToSymbolValue()
        {
            const string symbolValue = "ABC";
            const string sidSymbol = "EURUSD";
            var holding = new Holding {Symbol = new Symbol(SecurityIdentifier.GenerateForex(sidSymbol, Market.FXCM), symbolValue)};
            var serialized = JsonConvert.SerializeObject(holding, new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All});
            Console.WriteLine(serialized);
            Assert.IsFalse(serialized.Contains(sidSymbol));
            Assert.IsTrue(serialized.Contains(symbolValue));
        }
    }
}
