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

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Provides an implementations of IBrokerageFactory that produces a TradierBrokerage
    /// </summary>
    public class TradierBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Gets tradier values from configuration
        /// </summary>
        public static class Configuration
        {
            /// <summary>
            /// Gets whether to use the developer sandbox or not
            /// </summary>
            public static bool UseSandbox => Config.GetBool("tradier-use-sandbox");

            /// <summary>
            /// Gets the account ID to be used when instantiating a brokerage
            /// </summary>
            public static string AccountId => Config.Get("tradier-account-id");

            /// <summary>
            /// Gets the access token from configuration
            /// </summary>
            public static string AccessToken => Config.Get("tradier-access-token");
        }

        /// <summary>
        /// Initializes a new instance of he TradierBrokerageFactory class
        /// </summary>
        public TradierBrokerageFactory()
            : base(typeof(TradierBrokerage))
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
                    { "tradier-use-sandbox", Configuration.UseSandbox.ToStringInvariant() },
                    { "tradier-account-id", Configuration.AccountId.ToStringInvariant() },
                    { "tradier-access-token", Configuration.AccessToken.ToStringInvariant() }
                };
                return data;
            }
        }

        /// <summary>
        /// Gets a new instance of the <see cref="TradierBrokerageModel"/>
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new TradierBrokerageModel();

        /// <summary>
        /// Creates a new IBrokerage instance
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();
            var useSandbox = Read<bool>(job.BrokerageData, "tradier-use-sandbox", errors);
            var accountId = Read<string>(job.BrokerageData, "tradier-account-id", errors);
            var accessToken = Read<string>(job.BrokerageData, "tradier-access-token", errors);

            var brokerage = new TradierBrokerage(
                algorithm,
                algorithm.Transactions,
                algorithm.Portfolio,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")),
                useSandbox,
                accountId,
                accessToken);

            // Add the brokerage to the composer to ensure its accessible to the live data feed.
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);
            Composer.Instance.AddPart<IHistoryProvider>(brokerage);

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
