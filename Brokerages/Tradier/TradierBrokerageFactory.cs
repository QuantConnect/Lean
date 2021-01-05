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
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
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
            /// Gets the account ID to be used when instantiating a brokerage
            /// </summary>
            public static int QuantConnectUserID
            {
                get { return Config.GetInt("qc-user-id"); }
            }

            /// <summary>
            /// Gets the account ID to be used when instantiating a brokerage
            /// </summary>
            public static string AccountID
            {
                get { return Config.Get("tradier-account-id"); }
            }

            /// <summary>
            /// Gets the access token from configuration
            /// </summary>
            public static string AccessToken
            {
                get { return Config.Get("tradier-access-token"); }
            }

            /// <summary>
            /// Gets the refresh token from configuration
            /// </summary>
            public static string RefreshToken
            {
                get { return Config.Get("tradier-refresh-token"); }
            }

            /// <summary>
            /// Gets the date time the tokens were issued at from configuration
            /// </summary>
            public static DateTime TokensIssuedAt
            {
                get { return Config.GetValue<DateTime>("tradier-issued-at"); }
            }

            /// <summary>
            /// Gets the life span of the tokens from configuration
            /// </summary>
            public static TimeSpan LifeSpan
            {
                get { return TimeSpan.FromSeconds(Config.GetInt("tradier-lifespan")); }
            }
        }

        /// <summary>
        /// File path used to store tradier token data
        /// </summary>
        public const string TokensFile = "tradier-tokens.txt";

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
                string accessToken, refreshToken, issuedAt, lifeSpan;

                // always need to grab account ID from configuration
                var accountID = Configuration.AccountID.ToStringInvariant();
                var data = new Dictionary<string, string>();
                if (File.Exists(TokensFile))
                {
                    var tokens = JsonConvert.DeserializeObject<TokenResponse>(File.ReadAllText(TokensFile));
                    accessToken = tokens.AccessToken;
                    refreshToken = tokens.RefreshToken;
                    issuedAt = tokens.IssuedAt.ToString(CultureInfo.InvariantCulture);
                    lifeSpan = "86399";
                }
                else
                {
                    accessToken = Configuration.AccessToken;
                    refreshToken = Configuration.RefreshToken;
                    issuedAt = Configuration.TokensIssuedAt.ToString(CultureInfo.InvariantCulture);
                    lifeSpan = Configuration.LifeSpan.TotalSeconds.ToString(CultureInfo.InvariantCulture);
                }
                data.Add("tradier-account-id", accountID);
                data.Add("tradier-access-token", accessToken);
                data.Add("tradier-refresh-token", refreshToken);
                data.Add("tradier-issued-at", issuedAt);
                data.Add("tradier-lifespan", lifeSpan);
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
            var accountID = Read<string>(job.BrokerageData, "tradier-account-id", errors);
            var accessToken = Read<string>(job.BrokerageData, "tradier-access-token", errors);
            var refreshToken = Read<string>(job.BrokerageData, "tradier-refresh-token", errors);
            var issuedAt = Read<DateTime>(job.BrokerageData, "tradier-issued-at", errors);
            var lifeSpan = TimeSpan.FromSeconds(Read<double>(job.BrokerageData, "tradier-lifespan", errors));

            var brokerage = new TradierBrokerage(
                algorithm.Transactions, 
                algorithm.Portfolio,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")),
                accountID);

            // if we're running live locally we'll want to save any new tokens generated so that they can easily be retrieved
            if (Config.GetBool("tradier-save-tokens"))
            {
                brokerage.SessionRefreshed += (sender, args) =>
                {
                    File.WriteAllText(TokensFile, JsonConvert.SerializeObject(args, Formatting.Indented));
                };
            }

            brokerage.SetTokens(job.UserId, accessToken, refreshToken, issuedAt, lifeSpan);

            //Add the brokerage to the composer to ensure its accessible to the live data feed.
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


        /// <summary>
        /// Reads the tradier tokens from the <see cref="TokensFile"/> or from configuration
        /// </summary>
        public static TokenResponse GetTokens()
        {
            // pick a source for our tokens
            if (File.Exists(TokensFile))
            {
                Log.Trace("Reading tradier tokens from " + TokensFile);
                return JsonConvert.DeserializeObject<TokenResponse>(File.ReadAllText(TokensFile));
            }

            return new TokenResponse
            {
                AccessToken = Config.Get("tradier-access-token"),
                RefreshToken = Config.Get("tradier-refresh-token"),
                IssuedAt = Config.GetValue<DateTime>("tradier-issued-at"),
                ExpiresIn = Config.GetInt("tradier-lifespan")
            };
        }
    }
}
