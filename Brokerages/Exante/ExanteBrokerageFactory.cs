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
using QLNet;
using QuantConnect.Brokerages.Exante;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using RestSharp;

namespace QuantConnect.Brokerages.Exante
{
    /// <summary>
    /// Factory method to create Exante brokerage
    /// </summary>
    public class ExanteBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Factory constructor
        /// </summary>
        public ExanteBrokerageFactory()
            : base(typeof(ExanteBrokerage))
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
            {"exante-data-url", Config.Get("exante-data-url", "https://api-live.exante.eu/md/")},
            {"exante-trading-url", Config.Get("exante-trading-url", "https://api-live.exante.eu/trade/")},
            {"exante-access-token", Config.Get("exante-access-token")},
        };

        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider)
        {
            throw new NotImplementedException();
        }

        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();

            // read values from the brokerage data
            var dataUrl = Read<string>(job.BrokerageData, "exante-data-url", errors);
            var tradingUrl = Read<string>(job.BrokerageData, "exante-trading-url", errors);
            var accessToken = Read<string>(job.BrokerageData, "exante-access-token", errors);

            if (errors.empty())
            {
                // if we had errors then we can't create the instance
                throw new Exception(string.Join(System.Environment.NewLine, errors));
            }

            var brokerage = new ExanteBrokerage(
                dataUrl,
                tradingUrl,
                accessToken);
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
