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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class SubscriptionDataConfigExtensionsTests
    {
        private SubscriptionDataConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
        }

        [TestCase(DataNormalizationMode.Raw, 0.5, 100, 100)]
        [TestCase(DataNormalizationMode.Adjusted, 0.5, 100, 50)]
        [TestCase(DataNormalizationMode.SplitAdjusted, 0.5, 100, 50)]
        [TestCase(DataNormalizationMode.TotalReturn, 0.5, 100, 150)]
        public void NormalizePrice(DataNormalizationMode mode, decimal factor, decimal dividents, decimal expected)
        {
            _config.DataNormalizationMode = mode;
            _config.PriceScaleFactor = factor;
            _config.SumOfDividends = dividents;

            Assert.AreEqual(expected, _config.GetNormalizedPrice(100));
        }

    }
}
