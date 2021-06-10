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

using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.TDAmeritrade
{
    /// <summary>
    /// Provides an implementations of IBrokerageFactory that produces a TDAmeritradeBrokerage
    /// </summary>
    public class TDAmeritradeBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Gets tradier values from configuration
        /// </summary>
        public static class Configuration
        {
            /// <summary>
            /// Gets the account ID to be used when instantiating a brokerage
            /// </summary>
            public static string AccountId => Config.Get("td-account-id");

            /// <summary>
            /// Gets the access token from configuration
            /// </summary>
            public static string ClientId => Config.Get("td-client-id");

            public static string RedirectUri => Config.Get("td-redirect-uri");

            public static string AuthorizationCode => Config.Get("td-authorization-code");
        }

        /// <summary>
        /// Initializes a new instance of he TDAmeritradeBrokerageFactory class
        /// </summary>
        public TDAmeritradeBrokerageFactory()
            : base(typeof(TDAmeritradeBrokerage))
        {
        }

        /// <summary>
        /// Gets the brokerage data required to run the brokerage from configuration/disk
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        public override Dictionary<string, string> BrokerageData
        {
            get
            {
                var data = new Dictionary<string, string>
                {
                    { "td-account-id", Configuration.AccountId.ToStringInvariant() },
                    { "td-client-id", Configuration.ClientId.ToStringInvariant() },
                    { "td-redirect-uri", Configuration.RedirectUri.ToStringInvariant() },
                    { "td-authorization-code", Configuration.AuthorizationCode.ToStringInvariant() }
                };
                return data;
            }
        }

        /// <summary>
        /// Gets a new instance of the <see cref="TDAmeritradeBrokerageModel"/>
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new TDAmeritradeBrokerageModel();

        /// <summary>
        /// Creates a new IBrokerage instance
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();
            var accountId = Read<string>(job.BrokerageData, "td-account-id", errors);
            var clientId = Read<string>(job.BrokerageData, "td-client-id", errors);
            var redirectUri = Read<string>(job.BrokerageData, "td-redirect-uri", errors);
            var authorizationCode = Read<string>(job.BrokerageData, "td-authorization-code", errors);

            var brokerage = new TDAmeritradeBrokerage(
                algorithm,
                algorithm.Transactions,
                algorithm.Portfolio,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")),
                accountId,
                clientId,
                redirectUri,
                authorizationCode);

            // Add the brokerage to the composer to ensure its accessible to the live data feed.
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);
            Composer.Instance.AddPart<IHistoryProvider>(brokerage);
            Composer.Instance.AddPart<IOptionChainProvider>(brokerage);

            return brokerage;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override void Dispose()
        {
        }
    }
}
