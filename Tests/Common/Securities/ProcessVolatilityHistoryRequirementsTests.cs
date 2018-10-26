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
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine;
using QuantConnect.Securities;
using QuantConnect.Securities.Volatility;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    class ProcessVolatilityHistoryRequirementsTests
    {
        [Test]
        public void Works()
        {
            var algorithm = new AlgorithmStub();
            algorithm.HistoryProvider = new TestHistoryProvider();
            var security = algorithm.AddEquity(Symbols.SPY);
            var model = new TestVolatilityModel();
            security.VolatilityModel = model;

            AlgorithmManager.ProcessVolatilityHistoryRequirements(algorithm);

            Assert.AreEqual(1, model.dataUpdate.Count);
            Assert.AreEqual(Symbols.SPY, model.dataUpdate.First().Symbol);
            Assert.AreEqual(4, model.dataUpdate.First().Price);
        }
    }

    internal class TestVolatilityModel : BaseVolatilityModel
    {
        public List<BaseData> dataUpdate = new List<BaseData>();
        public override decimal Volatility { get; }
        public override void Update(Security security, BaseData data)
        {
            dataUpdate.Add(data);
        }

        public override IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime)
        {
            var configuration = SubscriptionDataConfigProvider.GetSubscriptionDataConfigs(security.Symbol).First();

            return new[]
            {
                new HistoryRequest(
                    utcTime,
                    utcTime,
                    typeof(TradeBar),
                    configuration.Symbol,
                    configuration.Resolution,
                    security.Exchange.Hours,
                    configuration.DataTimeZone,
                    configuration.Resolution,
                    configuration.ExtendedMarketHours,
                    configuration.IsCustomData,
                    configuration.DataNormalizationMode,
                    LeanData.GetCommonTickTypeForCommonDataTypes(typeof(TradeBar), security.Type)
                )
            };
        }
    }

    internal class TestHistoryProvider : HistoryProviderBase
    {
        public override int DataPointCount { get; }
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            var request = requests.First();
            return new List<Slice>{ new Slice(DateTime.UtcNow,
                new List<BaseData> {new TradeBar(DateTime.MinValue, request.Symbol, 1, 2, 3, 4, 5) })};
        }
    }
}
