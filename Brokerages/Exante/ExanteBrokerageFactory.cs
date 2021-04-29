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
using Exante.Net;
using Exante.Net.Enums;
using QLNet;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

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
            {"exante-client-id", Config.Get("exante-client-id")},
            {"exante-application-id", Config.Get("exante-application-id")},
            {"exante-shared-key", Config.Get("exante-shared-key")},
            {"exante-account-id", Config.Get("exante-account-id")},
            {"exante-platform-type", Config.Get("exante-platform-type")},
        };

        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider)
        {
            return new ExanteBrokerageModel();
        }

        public static ExanteClientOptions createExanteClientOptions(
            string clientId,
            string applicationId,
            string sharedKey,
            string platformTypeStr
            )
        {
            ExantePlatformType platformType;
            var platformTypeParsed = Enum.TryParse(platformTypeStr, true, out platformType);
            if (!platformTypeParsed)
            {
                throw new Exception($"ExantePlatformType parse error: {platformTypeStr}");
            }

            var exanteClientOptions =
                new ExanteClientOptions(
                    new ExanteApiCredentials(
                        clientId,
                        applicationId,
                        sharedKey
                    ),
                    platformType
                );
            return exanteClientOptions;
        }

        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();

            // read values from the brokerage data
            var clientId = Read<string>(job.BrokerageData, "exante-client-id", errors);
            var applicationId = Read<string>(job.BrokerageData, "exante-application-id", errors);
            var sharedKey = Read<string>(job.BrokerageData, "exante-shared-key", errors);
            var accountId = Read<string>(job.BrokerageData, "exante-account-id", errors);
            var platformTypeStr = Read<string>(job.BrokerageData, "exante-platform-type", errors);

            if (errors.empty())
            {
                // if we had errors then we can't create the instance
                throw new Exception(string.Join(System.Environment.NewLine, errors));
            }

            var clientOptions = createExanteClientOptions(clientId, applicationId, sharedKey, platformTypeStr);

            var brokerage = new ExanteBrokerage(
                clientOptions,
                accountId);
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
