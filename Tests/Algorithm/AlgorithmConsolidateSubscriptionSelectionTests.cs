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

using System.Linq;
using System;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmConsolidateSubscriptionSelectionTests
    {
        [Test]
        public void ConsolidateAttachesToHighestResolutionSubscription()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var symbol = algorithm.AddEquity("SPY", Resolution.Daily).Symbol;
            algorithm.AddEquity("SPY", Resolution.Minute);

            var consolidator = algorithm.Consolidate(symbol, Resolution.Hour, (TradeBar _) => { });

            var configs = algorithm.SubscriptionManager.SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(symbol, includeInternalConfigs: true);

            var minuteTradeConfig = configs.Single(config =>
                config.Resolution == Resolution.Minute &&
                config.TickType == TickType.Trade &&
                config.Type == typeof(TradeBar));

            var dailyTradeConfig = configs.Single(config =>
                config.Resolution == Resolution.Daily &&
                config.TickType == TickType.Trade &&
                config.Type == typeof(TradeBar));

            Assert.IsTrue(minuteTradeConfig.Consolidators.Contains(consolidator));
            Assert.IsFalse(dailyTradeConfig.Consolidators.Contains(consolidator));
        }

        [Test]
        public void ConsolidateUsesMinuteWhenHourIsTooCoarse()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var symbol = algorithm.AddEquity("SPY", Resolution.Daily).Symbol;
            algorithm.AddEquity("SPY", Resolution.Minute);
            algorithm.AddEquity("SPY", Resolution.Hour);

            var consolidator = algorithm.Consolidate(symbol, TimeSpan.FromMinutes(30), (TradeBar _) => { });

            var configs = algorithm.SubscriptionManager.SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(symbol, includeInternalConfigs: true);

            var minuteTradeConfig = configs.Single(config =>
                config.Resolution == Resolution.Minute &&
                config.TickType == TickType.Trade &&
                config.Type == typeof(TradeBar));

            var hourlyTradeConfig = configs.Single(config =>
                config.Resolution == Resolution.Hour &&
                config.TickType == TickType.Trade &&
                config.Type == typeof(TradeBar));

            Assert.IsTrue(minuteTradeConfig.Consolidators.Contains(consolidator));
            Assert.IsFalse(hourlyTradeConfig.Consolidators.Contains(consolidator));
        }

        [Test]
        public void ConsolidateAttachesToLowestResolutionThatStillWorks()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var symbol = algorithm.AddEquity("SPY", Resolution.Daily).Symbol;
            algorithm.AddEquity("SPY", Resolution.Minute);
            algorithm.AddEquity("SPY", Resolution.Hour);

            var consolidator = algorithm.Consolidate(symbol, Resolution.Hour, (TradeBar _) => { });

            var configs = algorithm.SubscriptionManager.SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(symbol, includeInternalConfigs: true);

            var minuteTradeConfig = configs.Single(config =>
                config.Resolution == Resolution.Minute &&
                config.TickType == TickType.Trade &&
                config.Type == typeof(TradeBar));

            var hourlyTradeConfig = configs.Single(config =>
                config.Resolution == Resolution.Hour &&
                config.TickType == TickType.Trade &&
                config.Type == typeof(TradeBar));

            Assert.IsFalse(minuteTradeConfig.Consolidators.Contains(consolidator));
            Assert.IsTrue(hourlyTradeConfig.Consolidators.Contains(consolidator));
        }
    }
}
