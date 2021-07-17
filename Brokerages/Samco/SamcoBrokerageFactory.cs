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
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Samco
{
    /// <summary>
    /// Factory method to create Samco Websockets brokerage
    /// </summary>
    public class SamcoBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Factory constructor
        /// </summary>
        public SamcoBrokerageFactory() : base(typeof(SamcoBrokerage))
        {
        }

        /// <summary>
        /// Not required
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// provides brokerage connection data
        /// </summary>
        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>
        {
            { "samco-api-key", Config.Get("samco.api-key")},
            { "samco-api-secret", Config.Get("samco.api-secret")},
            { "samco-api-yob", Config.Get("samco.year-of-birth")},
            { "trading-segment" ,Config.Get("samco.trading-segment") },
            { "product-type", Config.Get("samco.product-type") }
        };

        /// <summary>
        /// The brokerage model
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new SamcoBrokerageModel();

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override IBrokerage CreateBrokerage(Packets.LiveNodePacket job, IAlgorithm algorithm)
        {
            var required = new[] {"samco-api-secret", "samco-api-key", "samco-api-yob" };

            foreach (var item in required)
            {
                if (string.IsNullOrEmpty(job.BrokerageData[item]))
                    throw new Exception($"SamcoBrokerageFactory.CreateBrokerage: Missing {item} in config.json");
            }

            var brokerage = new SamcoBrokerage(
                job.BrokerageData["trading-segment"],
                job.BrokerageData["product-type"],
                job.BrokerageData["samco-api-key"],
                job.BrokerageData["samco-api-secret"],
                job.BrokerageData["samco-api-yob"],
                algorithm,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")));
            //Add the brokerage to the composer to ensure its accessible to the live data feed.
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);
            return brokerage;
        }
    }
}
